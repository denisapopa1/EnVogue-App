using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;
using System.Text.Json;

namespace EnVogue.Services
{
    public class ProfilePhotoService
    {
        private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;
        private static readonly JsonSerializerOptions ApiJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;
        private readonly UserState _userState;

        private bool _isLoaded;
        private string? _loadedForUser;
        private string? _photoDataUrl;

        public event Action? OnChange;

        public ProfilePhotoService(HttpClient http, ILocalStorageService localStorage, UserState userState)
        {
            _http = http;
            _localStorage = localStorage;
            _userState = userState;
        }

        public string? PhotoDataUrl => _photoDataUrl;
        public bool HasPhoto => !string.IsNullOrWhiteSpace(_photoDataUrl);

        public async Task EnsureLoadedAsync()
        {
            var currentKey = GetCurrentStorageKey();

            if (_isLoaded && string.Equals(_loadedForUser, currentKey, StringComparison.Ordinal))
            {
                return;
            }

            _loadedForUser = currentKey;
            _isLoaded = true;

            if (string.IsNullOrWhiteSpace(currentKey))
            {
                _photoDataUrl = null;
                OnChange?.Invoke();
                return;
            }

            var backendResult = await TryLoadFromBackendAsync();
            if (backendResult.BackendAvailable)
            {
                _photoDataUrl = backendResult.PhotoDataUrl;

                if (string.IsNullOrWhiteSpace(_photoDataUrl))
                {
                    await _localStorage.RemoveItemAsync(currentKey);
                }
                else
                {
                    await _localStorage.SetItemAsync(currentKey, _photoDataUrl);
                }
            }
            else
            {
                _photoDataUrl = await _localStorage.GetItemAsync<string>(currentKey);
            }

            if (!string.IsNullOrWhiteSpace(_photoDataUrl))
            {
                await _localStorage.SetItemAsync(currentKey, _photoDataUrl);
            }

            OnChange?.Invoke();
        }

        public async Task<bool> SavePhotoAsync(IBrowserFile file)
        {
            await EnsureLoadedAsync();

            var currentKey = GetCurrentStorageKey();
            if (string.IsNullOrWhiteSpace(currentKey))
            {
                return false;
            }

            var imageFile = file;
            if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                imageFile = await file.RequestImageFileAsync("image/jpeg", 600, 600);
            }

            await using var stream = imageFile.OpenReadStream(MaxPhotoSizeBytes);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            var contentType = string.IsNullOrWhiteSpace(imageFile.ContentType) ? "image/jpeg" : imageFile.ContentType;
            var base64 = Convert.ToBase64String(memoryStream.ToArray());
            _photoDataUrl = $"data:{contentType};base64,{base64}";

            var syncedPhoto = await TrySaveToBackendAsync(_photoDataUrl);
            if (!string.IsNullOrWhiteSpace(syncedPhoto))
            {
                _photoDataUrl = syncedPhoto;
            }

            await _localStorage.SetItemAsync(currentKey, _photoDataUrl);
            OnChange?.Invoke();
            return true;
        }

        public async Task<bool> RemovePhotoAsync()
        {
            await EnsureLoadedAsync();

            var currentKey = GetCurrentStorageKey();
            if (string.IsNullOrWhiteSpace(currentKey))
            {
                return false;
            }

            await TryDeleteFromBackendAsync();
            await _localStorage.RemoveItemAsync(currentKey);
            _photoDataUrl = null;
            OnChange?.Invoke();
            return true;
        }

        public async Task ClearLoadedStateAsync()
        {
            _photoDataUrl = null;
            _isLoaded = false;
            _loadedForUser = null;
            await Task.CompletedTask;
            OnChange?.Invoke();
        }

        public async Task DeleteStoredPhotoForCurrentUserAsync()
        {
            var currentKey = GetCurrentStorageKey();
            if (!string.IsNullOrWhiteSpace(currentKey))
            {
                await _localStorage.RemoveItemAsync(currentKey);
            }

            _photoDataUrl = null;
            _isLoaded = false;
            _loadedForUser = null;
            OnChange?.Invoke();
        }

        private string? GetCurrentStorageKey()
        {
            var email = _userState.Email?.Trim().ToLowerInvariant();
            return string.IsNullOrWhiteSpace(email) ? null : $"profilePhoto:{email}";
        }

        private async Task<BackendPhotoLoadResult> TryLoadFromBackendAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/account/profile-photo");
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return new BackendPhotoLoadResult
                    {
                        BackendAvailable = true,
                        PhotoDataUrl = null
                    };
                }

                if (!response.IsSuccessStatusCode)
                {
                    return new BackendPhotoLoadResult
                    {
                        BackendAvailable = false
                    };
                }

                var raw = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return new BackendPhotoLoadResult
                    {
                        BackendAvailable = true,
                        PhotoDataUrl = null
                    };
                }

                var result = JsonSerializer.Deserialize<ProfilePhotoResponse>(raw, ApiJsonOptions);
                return new BackendPhotoLoadResult
                {
                    BackendAvailable = true,
                    PhotoDataUrl = result?.PhotoDataUrl
                };
            }
            catch
            {
                return new BackendPhotoLoadResult
                {
                    BackendAvailable = false
                };
            }
        }

        private async Task<string?> TrySaveToBackendAsync(string photoDataUrl)
        {
            try
            {
                var response = await _http.PutAsJsonAsync("api/account/profile-photo", new ProfilePhotoRequest
                {
                    PhotoDataUrl = photoDataUrl
                });

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var raw = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return photoDataUrl;
                }

                var result = JsonSerializer.Deserialize<ProfilePhotoResponse>(raw, ApiJsonOptions);
                return string.IsNullOrWhiteSpace(result?.PhotoDataUrl) ? photoDataUrl : result.PhotoDataUrl;
            }
            catch
            {
                return null;
            }
        }

        private async Task TryDeleteFromBackendAsync()
        {
            try
            {
                await _http.DeleteAsync("api/account/profile-photo");
            }
            catch
            {
                // Keep local delete working even if backend sync is not ready yet.
            }
        }

        private class ProfilePhotoRequest
        {
            public string PhotoDataUrl { get; set; } = string.Empty;
        }

        private class ProfilePhotoResponse
        {
            public string? PhotoDataUrl { get; set; }
        }

        private class BackendPhotoLoadResult
        {
            public bool BackendAvailable { get; set; }
            public string? PhotoDataUrl { get; set; }
        }
    }
}
