using Blazored.LocalStorage;
using EnVogue.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace EnVogue.Services
{
    public class AccountProfileService
    {
        private static readonly JsonSerializerOptions ApiJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;
        private readonly UserState _userState;

        private bool _isLoaded;
        private string? _loadedForUser;
        private AccountProfileDetails? _profile;

        public event Action? OnChange;

        public AccountProfileService(HttpClient http, ILocalStorageService localStorage, UserState userState)
        {
            _http = http;
            _localStorage = localStorage;
            _userState = userState;
        }

        public AccountProfileDetails? CurrentProfile => _profile;

        public async Task<AccountProfileDetails?> EnsureLoadedAsync()
        {
            var currentKey = GetCurrentStorageKey();

            if (_isLoaded && string.Equals(_loadedForUser, currentKey, StringComparison.Ordinal))
            {
                return _profile;
            }

            _loadedForUser = currentKey;
            _isLoaded = true;

            if (string.IsNullOrWhiteSpace(currentKey))
            {
                _profile = null;
                OnChange?.Invoke();
                return _profile;
            }

            _profile = await TryLoadFromBackendAsync()
                ?? await _localStorage.GetItemAsync<AccountProfileDetails>(currentKey);

            _profile ??= CreateDefaultProfile();
            NormalizeProfile(_profile);

            await _localStorage.SetItemAsync(currentKey, _profile);

            OnChange?.Invoke();
            return _profile;
        }

        public async Task<AccountProfileDetails?> GetEditableProfileAsync()
        {
            var profile = await EnsureLoadedAsync();
            return profile == null ? null : Clone(profile);
        }

        public async Task<bool> SaveAsync(AccountProfileDetails profile)
        {
            await EnsureLoadedAsync();

            var currentKey = GetCurrentStorageKey();
            if (string.IsNullOrWhiteSpace(currentKey))
            {
                return false;
            }

            NormalizeProfile(profile);
            profile.Email = _userState.Email ?? profile.Email;

            var backendProfile = await TrySaveToBackendAsync(profile);
            _profile = Clone(backendProfile ?? profile);
            await _localStorage.SetItemAsync(currentKey, _profile);
            OnChange?.Invoke();
            return true;
        }

        public async Task ClearLoadedStateAsync()
        {
            _profile = null;
            _isLoaded = false;
            _loadedForUser = null;
            await Task.CompletedTask;
            OnChange?.Invoke();
        }

        public async Task DeleteStoredProfileForCurrentUserAsync()
        {
            var currentKey = GetCurrentStorageKey();
            if (!string.IsNullOrWhiteSpace(currentKey))
            {
                await _localStorage.RemoveItemAsync(currentKey);
            }

            _profile = null;
            _isLoaded = false;
            _loadedForUser = null;
            OnChange?.Invoke();
        }

        private AccountProfileDetails CreateDefaultProfile()
        {
            var email = _userState.Email ?? string.Empty;
            return new AccountProfileDetails
            {
                Email = email,
                DisplayName = BuildDefaultDisplayName(email)
            };
        }

        private static AccountProfileDetails Clone(AccountProfileDetails profile)
        {
            return new AccountProfileDetails
            {
                DisplayName = profile.DisplayName,
                Email = profile.Email,
                Bio = profile.Bio,
                Phone = profile.Phone,
                City = profile.City,
                Birthday = profile.Birthday,
                FavoriteStyle = profile.FavoriteStyle,
                FavoriteBrands = profile.FavoriteBrands,
                PreferredColors = profile.PreferredColors,
                InstagramHandle = profile.InstagramHandle,
                StylingNotes = profile.StylingNotes
            };
        }

        private void NormalizeProfile(AccountProfileDetails profile)
        {
            profile.Email = string.IsNullOrWhiteSpace(_userState.Email) ? profile.Email.Trim() : _userState.Email;
            profile.DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? BuildDefaultDisplayName(profile.Email)
                : profile.DisplayName.Trim();
            profile.Bio = profile.Bio?.Trim() ?? string.Empty;
            profile.Phone = profile.Phone?.Trim() ?? string.Empty;
            profile.City = profile.City?.Trim() ?? string.Empty;
            profile.Birthday = profile.Birthday?.Trim() ?? string.Empty;
            profile.FavoriteStyle = profile.FavoriteStyle?.Trim() ?? string.Empty;
            profile.FavoriteBrands = profile.FavoriteBrands?.Trim() ?? string.Empty;
            profile.PreferredColors = profile.PreferredColors?.Trim() ?? string.Empty;
            profile.InstagramHandle = profile.InstagramHandle?.Trim() ?? string.Empty;
            profile.StylingNotes = profile.StylingNotes?.Trim() ?? string.Empty;
        }

        private string? GetCurrentStorageKey()
        {
            var email = _userState.Email?.Trim().ToLowerInvariant();
            return string.IsNullOrWhiteSpace(email) ? null : $"accountProfile:{email}";
        }

        private static string BuildDefaultDisplayName(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "Member";
            }

            var namePart = email.Split('@')[0].Trim();
            return string.IsNullOrWhiteSpace(namePart) ? "Member" : namePart;
        }

        private async Task<AccountProfileDetails?> TryLoadFromBackendAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/account/profile");
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var raw = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<AccountProfileDetails>(raw, ApiJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private async Task<AccountProfileDetails?> TrySaveToBackendAsync(AccountProfileDetails profile)
        {
            try
            {
                var response = await _http.PutAsJsonAsync("api/account/profile", profile);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var raw = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return profile;
                }

                return JsonSerializer.Deserialize<AccountProfileDetails>(raw, ApiJsonOptions) ?? profile;
            }
            catch
            {
                return null;
            }
        }
    }
}
