using EnVogue.Models;
using Blazored.LocalStorage;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace EnVogue.Services
{
    public class WardrobeServices
    {
        private static readonly System.Text.Json.JsonSerializerOptions ApiJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;
        private readonly UserState _userState; // <--- Adaugă asta
        private readonly IJSRuntime _js;

        // Cache-ul local pentru UI
        private List<ClothingItem> _clothingItems = new();
        private List<Outfit> _outfits = new();

        // Variabilă pentru email-ul utilizatorului
        private string? _loggedInUserEmail;
        private bool _isInitialized;
        private string? _initializedForUser;

        public event Action? OnChange;

        public WardrobeServices(HttpClient http, ILocalStorageService localStorage, UserState userState, IJSRuntime js)
        {
            _http = http;
            _localStorage = localStorage;
            _userState = userState;
            _js = js;
        }

        // --- AUTENTIFICARE ȘI IDENTITATE ---

        public async Task<bool> LoginAsync(string email, string password)
        {
            var loginModel = new { Email = email, Password = password };
            var response = await _http.PostAsJsonAsync("api/auth/login", loginModel);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    // Salvăm în LocalStorage (Aici se face legătura cu ce am verificat în browser)
                    await _localStorage.SetItemAsync("authToken", result.Token);
                    await _localStorage.SetItemAsync("userEmail", email);

                    // Setăm header-ul pentru HttpClient
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                    _loggedInUserEmail = email;

                    // ANUNȚĂM UserState că ne-am logat (foarte important!)
                    _userState.Login(email);

                    await InitializeAsync();
                    return true;
                }
            }
            return false;
        }
        public void SetLoggedInUser(string email) => _loggedInUserEmail = email;
        public string GetLoggedInUserEmail() => _loggedInUserEmail ?? "Guest";

        // --- LOGICĂ DE PERSISTENȚĂ (API) ---

        public async Task InitializeAsync(bool forceReload = false)
        {
            try
            {
                // 1. Încercăm să luăm datele salvate din browser
                var token = await _localStorage.GetItemAsync<string>("authToken");
                var savedEmail = await _localStorage.GetItemAsync<string>("userEmail");

                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(savedEmail))
                {
                    if (!forceReload &&
                        _isInitialized &&
                        string.Equals(_initializedForUser, savedEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    // 2. Setăm header-ul pentru ca toate cererile viitoare către API să fie autorizate
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // 3. Re-populăm variabilele locale
                    _loggedInUserEmail = savedEmail;

                    _userState.Login(savedEmail);
                    // 4. (OPȚIONAL dar RECOMANDAT) Verificăm dacă token-ul este încă valid făcând o cerere mică la API
                    // Dacă API-ul returnează 401 Unauthorized, înseamnă că token-ul a expirat
                    var checkResponse = await _http.GetAsync("api/clothing/count"); // presupunând că ai un endpoint simplu

                    if (checkResponse.IsSuccessStatusCode)
                    {
                        // Totul e ok, datele se încarcă în continuare...
                        var items = await _http.GetFromJsonAsync<List<ClothingItem>>("api/clothing");
                        if (items != null) _clothingItems = items;

                        await LoadOutfitsAsync();
                        _isInitialized = true;
                        _initializedForUser = savedEmail;
                    }
                    else
                    {
                        // Token-ul a expirat sau e invalid -> curățăm tot
                        await LogoutAsync();
                    }
                }

                //NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auth initialization failed: {ex.Message}");
            }
        }

        public void NotifyStateChanged() => OnChange?.Invoke();

        // --- HAINE (API) ---

        public List<ClothingItem> GetAllClothes() => _clothingItems;

        public List<ClothingItem> GetClothesByCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) return _clothingItems;
            return _clothingItems.Where(c => c.CategoryName != null &&
                c.CategoryName.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task AddClothingItem(ClothingItem item)
        {
            var response = await _http.PostAsJsonAsync("api/clothing", item);

            if (response.IsSuccessStatusCode)
            {
                var newItem = await response.Content.ReadFromJsonAsync<ClothingItem>();
                if (newItem != null) _clothingItems.Add(newItem);
                NotifyStateChanged();
            }
            else
            {
                // ASTA te va ajuta să vezi în Consola browserului (F12) de ce NU se salvează
                var errorMsg = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"SERVER ERROR: {errorMsg}");
            }
        }
        public async Task RemoveClothingItem(int id)
        {
            var response = await _http.DeleteAsync($"api/clothing/{id}");
            if (response.IsSuccessStatusCode)
            {
                _clothingItems.RemoveAll(i => i.Id == id);
                NotifyStateChanged();
            }
        }
        public async Task<bool> RegisterAsync(string email, string password)
        {
            var model = new { Email = email, Password = password };
            var response = await _http.PostAsJsonAsync("api/auth/register", model);
            return response.IsSuccessStatusCode;
        }

        public async Task<AccountActionResult> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            var request = new ChangePasswordRequest
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            };

            try
            {
                var response = await _http.PutAsJsonAsync("api/account/password", request);
                var rawResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return AccountActionResult.Ok("Password updated successfully.");
                }

                return AccountActionResult.Fail(BuildChangePasswordErrorMessage(response.StatusCode, rawResponse));
            }
            catch (Exception ex)
            {
                await LogToBrowserConsoleAsync("error", $"Wardrobe API: password update failed. {ex.Message}");
                return AccountActionResult.Fail("Password could not be updated.");
            }
        }

        public async Task<AccountActionResult> DeleteAccountAsync(string currentPassword)
        {
            var request = new DeleteAccountRequest
            {
                CurrentPassword = currentPassword
            };

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, "api/account")
                {
                    Content = JsonContent.Create(request)
                };

                var response = await _http.SendAsync(httpRequest);
                var rawResponse = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return AccountActionResult.Ok("Account deleted successfully.");
                }

                return AccountActionResult.Fail(BuildGenericAccountErrorMessage(response.StatusCode, rawResponse, "Account could not be deleted."));
            }
            catch (Exception ex)
            {
                await LogToBrowserConsoleAsync("error", $"Wardrobe API: account delete failed. {ex.Message}");
                return AccountActionResult.Fail("Account could not be deleted.");
            }
        }
        // --- OUTFITS (API) ---

        public List<Outfit> GetAllOutfits() => _outfits;

        public Outfit? GetOutfitById(int outfitId) => _outfits.FirstOrDefault(o => o.Id == outfitId);

        public async Task LoadOutfitsAsync()
        {
            await LogToBrowserConsoleAsync("log", "Wardrobe API: loading outfits from GET api/outfits...");
            var outfits = await _http.GetFromJsonAsync<List<Outfit>>("api/outfits");
            if (outfits != null)
            {
                _outfits = outfits;
                await LogToBrowserConsoleAsync("log", $"Wardrobe API: loaded {_outfits.Count} outfit(s) from backend.");
            }
        }

        public async Task<SaveOutfitResult> AddOutfit(Outfit outfit)
        {
            var clothingItemIds = outfit.Items
                .Where(item => item.Id > 0)
                .Select(item => item.Id)
                .Distinct()
                .ToList();

            if (!clothingItemIds.Any())
            {
                const string noItemsMessage = "Outfitul nu poate fi salvat fara itemuri valide.";
                return SaveOutfitResult.Fail(noItemsMessage);
            }

            var request = new CreateOutfitRequest
            {
                Name = string.IsNullOrWhiteSpace(outfit.Name) ? "Outfit casual" : outfit.Name,
                Occasion = string.IsNullOrWhiteSpace(outfit.Occasion) ? "Daily" : outfit.Occasion,
                ClothingItemIds = clothingItemIds,
                IsFavorite = outfit.IsFavorite
            };

            var requestJson = System.Text.Json.JsonSerializer.Serialize(request);
            await LogToBrowserConsoleAsync("log", $"Wardrobe API: POST api/outfits payload = {requestJson}");

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsJsonAsync("api/outfits", request);
            }
            catch (Exception ex)
            {
                var networkMessage = $"Nu am putut contacta Wardrobe API: {ex.Message}";
                await LogToBrowserConsoleAsync("error", networkMessage);
                return SaveOutfitResult.Fail(networkMessage);
            }

            var rawResponseBody = await response.Content.ReadAsStringAsync();
            await LogToBrowserConsoleAsync("log", $"Wardrobe API: POST api/outfits responded {(int)response.StatusCode} {response.StatusCode}.");
            if (!string.IsNullOrWhiteSpace(rawResponseBody))
            {
                await LogToBrowserConsoleAsync(response.IsSuccessStatusCode ? "log" : "error", $"Wardrobe API: response body = {rawResponseBody}");
            }

            if (response.IsSuccessStatusCode)
            {
                Outfit? newOutfit = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(rawResponseBody))
                    {
                        newOutfit = System.Text.Json.JsonSerializer.Deserialize<Outfit>(rawResponseBody, ApiJsonOptions);
                    }
                }
                catch
                {
                    // Some APIs return 200/201 with an empty body. Keep the UI in sync anyway.
                }

                var savedOutfit = newOutfit ?? outfit;
                if (savedOutfit.Items == null || !savedOutfit.Items.Any())
                {
                    savedOutfit.Items = new List<ClothingItem>(outfit.Items);
                }

                if (string.IsNullOrWhiteSpace(savedOutfit.CreationSource) && !string.IsNullOrWhiteSpace(outfit.CreationSource))
                {
                    savedOutfit.CreationSource = outfit.CreationSource;
                }

                var existingIndex = _outfits.FindIndex(o => o.Id == savedOutfit.Id);
                if (existingIndex >= 0)
                {
                    _outfits[existingIndex] = savedOutfit;
                }
                else
                {
                    _outfits.Add(savedOutfit);
                }

                try
                {
                    await LoadOutfitsAsync();
                }
                catch (Exception ex)
                {
                    await LogToBrowserConsoleAsync("warn", $"Outfit saved, but refresh failed: {ex.Message}");
                }

                var persistedOutfit = _outfits.FirstOrDefault(o => o.Id == savedOutfit.Id);
                if (persistedOutfit != null && persistedOutfit.IsFavorite != outfit.IsFavorite)
                {
                    persistedOutfit.IsFavorite = outfit.IsFavorite;

                    if ((persistedOutfit.Items == null || !persistedOutfit.Items.Any()) && outfit.Items.Any())
                    {
                        persistedOutfit.Items = new List<ClothingItem>(outfit.Items);
                    }

                    if (string.IsNullOrWhiteSpace(persistedOutfit.CreationSource) && !string.IsNullOrWhiteSpace(outfit.CreationSource))
                    {
                        persistedOutfit.CreationSource = outfit.CreationSource;
                    }

                    var favoriteSynced = await UpdateOutfit(persistedOutfit);
                    if (!favoriteSynced)
                    {
                        await LogToBrowserConsoleAsync("warn",
                            $"Outfit {persistedOutfit.Id} was saved, but favorite state could not be synchronized to {outfit.IsFavorite}.");
                    }
                }

                NotifyStateChanged();
                return SaveOutfitResult.Ok("Outfitul a fost salvat in wardrobe.");
            }

            var userMessage = BuildOutfitSaveErrorMessage(response.StatusCode, rawResponseBody);
            await LogToBrowserConsoleAsync("error", $"Failed to add outfit: {userMessage} | Raw response: {rawResponseBody}");
            return SaveOutfitResult.Fail(userMessage, (int)response.StatusCode);
        }

        public async Task<Outfit?> GenerateOutfitPreviewAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<Outfit>("api/outfit-generator/generate");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate outfit preview: {ex.Message}");
                return null;
            }
        }

        public async Task UpdateClothingItem(ClothingItem updatedItem)
        {
            // Trimitem obiectul modificat către API (ClothingController din Backend)
            // Ruta va fi probabil api/clothing/{id}
            var response = await _http.PutAsJsonAsync($"api/clothing/{updatedItem.Id}", updatedItem);

            if (response.IsSuccessStatusCode)
            {
                // Actualizăm și lista noastră locală (cache-ul) pentru a vedea schimbarea imediat
                var index = _clothingItems.FindIndex(i => i.Id == updatedItem.Id);
                if (index != -1)
                {
                    _clothingItems[index] = updatedItem;
                    NotifyStateChanged();
                }
            }
            else
            {
                // Poți loga eroarea dacă API-ul refuză update-ul
                Console.WriteLine("Failed to update item in API.");
            }
        }
        public async Task<bool> UpdateOutfit(Outfit updatedOutfit)
        {
            var request = BuildUpdateOutfitRequest(updatedOutfit);
            var requestJson = System.Text.Json.JsonSerializer.Serialize(request);
            await LogToBrowserConsoleAsync("log", $"Wardrobe API: PUT api/outfits/{updatedOutfit.Id} payload = {requestJson}");

            var response = await _http.PutAsJsonAsync($"api/outfits/{updatedOutfit.Id}", request);
            var rawResponseBody = await response.Content.ReadAsStringAsync();
            await LogToBrowserConsoleAsync("log", $"Wardrobe API: PUT api/outfits/{updatedOutfit.Id} responded {(int)response.StatusCode} {response.StatusCode}.");
            if (!string.IsNullOrWhiteSpace(rawResponseBody))
            {
                await LogToBrowserConsoleAsync(response.IsSuccessStatusCode ? "log" : "error",
                    $"Wardrobe API: update response body = {rawResponseBody}");
            }

            if (response.IsSuccessStatusCode)
            {
                Outfit? serverOutfit = null;
                if (!string.IsNullOrWhiteSpace(rawResponseBody))
                {
                    try
                    {
                        serverOutfit = System.Text.Json.JsonSerializer.Deserialize<Outfit>(rawResponseBody, ApiJsonOptions);
                    }
                    catch
                    {
                        // Keep using the local payload when the API returns an unexpected shape.
                    }
                }

                if (serverOutfit != null)
                {
                    await LogToBrowserConsoleAsync("log",
                        $"Wardrobe API: deserialized server outfit {serverOutfit.Id} favorite={serverOutfit.IsFavorite}.");

                    if ((serverOutfit.Items == null || !serverOutfit.Items.Any()) && updatedOutfit.Items.Any())
                    {
                        serverOutfit.Items = new List<ClothingItem>(updatedOutfit.Items);
                    }

                    if (string.IsNullOrWhiteSpace(serverOutfit.CreationSource) && !string.IsNullOrWhiteSpace(updatedOutfit.CreationSource))
                    {
                        serverOutfit.CreationSource = updatedOutfit.CreationSource;
                    }

                    if (serverOutfit.IsFavorite != updatedOutfit.IsFavorite)
                    {
                        await LogToBrowserConsoleAsync("warn",
                            $"Wardrobe API: server returned isFavorite={serverOutfit.IsFavorite} after requesting {updatedOutfit.IsFavorite} for outfit {updatedOutfit.Id}.");
                        return false;
                    }
                }

                var index = _outfits.FindIndex(o => o.Id == updatedOutfit.Id);
                if (index != -1)
                {
                    _outfits[index] = serverOutfit ?? updatedOutfit;
                    NotifyStateChanged();
                }

                return true;
            }

            return false;
        }

        public async Task<bool> RemoveOutfit(int outfitId)
        {
            var response = await _http.DeleteAsync($"api/outfits/{outfitId}");
            if (response.IsSuccessStatusCode)
            {
                _outfits.RemoveAll(o => o.Id == outfitId);
                NotifyStateChanged();
                return true;
            }

            return false;
        }
        // --- METODĂ NOUĂ: Toggle Favorite (folosită în pagină) ---
        public async Task<bool> ToggleOutfitFavorite(int outfitId)
        {
            var outfit = _outfits.FirstOrDefault(o => o.Id == outfitId);
            if (outfit != null)
            {
                return await ToggleFavorite(outfitId);
            }

            return false;
        }
        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            // ȘTERGE ȘI EMAIL-UL:
            await _localStorage.RemoveItemAsync("userEmail");

            _http.DefaultRequestHeaders.Authorization = null;
            _clothingItems = new();
            _outfits = new();
            _loggedInUserEmail = null;
            _isInitialized = false;
            _initializedForUser = null;

            NotifyStateChanged();
        }
        public async Task<bool> ToggleFavorite(int outfitId)
        {
            var outfit = _outfits.FirstOrDefault(o => o.Id == outfitId);
            if (outfit != null)
            {
                var previousValue = outfit.IsFavorite;
                outfit.IsFavorite = !outfit.IsFavorite;

                var updatedSuccessfully = await UpdateOutfit(outfit);
                if (updatedSuccessfully)
                {
                    NotifyStateChanged();
                }
                else
                {
                    outfit.IsFavorite = previousValue;
                    await LogToBrowserConsoleAsync("warn", $"Wardrobe API: favorite state rollback applied for outfit {outfitId}.");
                }

                return updatedSuccessfully;
            }

            return false;
        }
        // --- LOGICA DE GENERATOR ---

        public Outfit GenerateOutfitWithRules(
            string? occasion = null,
            string? preferredStyle = null,
            string? preferredSeason = null,
            string? preferredWeather = null)
        {
            var trimmedOccasion = occasion?.Trim();
            var trimmedStyle = preferredStyle?.Trim();
            var trimmedSeason = preferredSeason?.Trim();
            var trimmedWeather = preferredWeather?.Trim();

            var outfit = new Outfit
            {
                Name = BuildGeneratedOutfitName(trimmedOccasion),
                Occasion = !string.IsNullOrWhiteSpace(trimmedStyle)
                    ? trimmedStyle
                    : !string.IsNullOrWhiteSpace(trimmedOccasion) ? trimmedOccasion : "Casual Style",
                Items = new List<ClothingItem>()
            };

            // Luăm categoriile din cache-ul local (care a fost populat din API)
            var allTops = GetClothesByCategory("tops");
            var allBottoms = GetClothesByCategory("bottoms");
            var allDresses = GetClothesByCategory("dress"); // Atenție: Verifică dacă e "dress" sau "dresses" în baza de date
            var allOuterwear = GetClothesByCategory("outerwear");
            var allShoes = GetClothesByCategory("shoes");
            var allAccessories = GetClothesByCategory("accessories");

            var tops = ApplyOptionalFilters(allTops, trimmedStyle, trimmedSeason);
            var bottoms = ApplyOptionalFilters(allBottoms, trimmedStyle, trimmedSeason);
            var dresses = ApplyOptionalFilters(allDresses, trimmedStyle, trimmedSeason);
            var shoes = ApplyWeatherPreferences(ApplyOptionalFilters(allShoes, trimmedStyle, trimmedSeason), trimmedWeather);
            var outerwear = ApplyOptionalFilters(allOuterwear, trimmedStyle, trimmedSeason);
            var accessories = ApplyOptionalFilters(allAccessories, trimmedStyle, trimmedSeason);

            // --- LOGICA DE SELECȚIE (Reguli de modă) ---
            var preferLayeredLook = string.Equals(trimmedWeather, "Cloudy", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(trimmedWeather, "Rainy", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(trimmedWeather, "Cold", StringComparison.OrdinalIgnoreCase);

            // Decidem: Rochie sau (Top + Bottom)?
            bool chooseDress = dresses.Any() && !(preferLayeredLook && tops.Any() && bottoms.Any()) && Random.Shared.Next(2) == 0;

            if (chooseDress)
            {
                outfit.Items.Add(dresses[Random.Shared.Next(dresses.Count)]);
            }
            else
            {
                if (tops.Any()) outfit.Items.Add(tops[Random.Shared.Next(tops.Count)]);
                if (bottoms.Any()) outfit.Items.Add(bottoms[Random.Shared.Next(bottoms.Count)]);
            }

            // Adăugăm încălțăminte
            if (shoes.Any()) outfit.Items.Add(shoes[Random.Shared.Next(shoes.Count)]);

            if (ShouldAddOuterwear(trimmedWeather) && outerwear.Any())
            {
                outfit.Items.Add(outerwear[Random.Shared.Next(outerwear.Count)]);
            }

            // Adăugăm accesorii cu șanse adaptate vremii.
            if (accessories.Any() && ShouldAddAccessory(trimmedWeather))
            {
                outfit.Items.Add(accessories[Random.Shared.Next(accessories.Count)]);
            }

            return outfit.Items.Count > 0 ? outfit : new Outfit { Name = "Empty", Occasion = outfit.Occasion };
        }

        private static List<ClothingItem> ApplyOptionalFilters(
            List<ClothingItem> items,
            string? preferredStyle,
            string? preferredSeason)
        {
            IEnumerable<ClothingItem> query = items;

            if (!string.IsNullOrWhiteSpace(preferredStyle))
            {
                query = query.Where(item => string.Equals(item.Style, preferredStyle, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(preferredSeason))
            {
                query = query.Where(item => string.Equals(item.Season, preferredSeason, StringComparison.OrdinalIgnoreCase));
            }

            var filteredItems = query.ToList();
            return filteredItems.Any() ? filteredItems : items;
        }

        private static List<ClothingItem> ApplyWeatherPreferences(List<ClothingItem> shoes, string? preferredWeather)
        {
            if (!shoes.Any() || string.IsNullOrWhiteSpace(preferredWeather))
            {
                return shoes;
            }

            IEnumerable<ClothingItem> query = preferredWeather.ToLowerInvariant() switch
            {
                "sunny" => shoes.Where(item => MatchesSubcategory(item, "Sandals") || MatchesSubcategory(item, "Sneakers")),
                "cloudy" => shoes.Where(item => MatchesSubcategory(item, "Sneakers") || MatchesSubcategory(item, "Boots")),
                "rainy" => shoes.Where(item => MatchesSubcategory(item, "Boots") || MatchesSubcategory(item, "Sneakers")),
                "cold" => shoes.Where(item => MatchesSubcategory(item, "Boots")),
                _ => shoes
            };

            var filteredShoes = query.ToList();
            return filteredShoes.Any() ? filteredShoes : shoes;
        }

        private static bool MatchesSubcategory(ClothingItem item, string subcategory)
            => string.Equals(item.SubcategoryName, subcategory, StringComparison.OrdinalIgnoreCase);

        private static bool ShouldAddOuterwear(string? preferredWeather) => preferredWeather?.ToLowerInvariant() switch
        {
            "cold" => true,
            "rainy" => true,
            "cloudy" => Random.Shared.Next(100) < 55,
            _ => false
        };

        private static bool ShouldAddAccessory(string? preferredWeather) => preferredWeather?.ToLowerInvariant() switch
        {
            "sunny" => Random.Shared.Next(100) < 55,
            "rainy" => Random.Shared.Next(100) < 15,
            _ => Random.Shared.Next(100) < 30
        };

        private string BuildGeneratedOutfitName(string? occasion)
            => !string.IsNullOrWhiteSpace(occasion)
                ? occasion
                : $"Outfit {_outfits.Count + 1}";

        // --- MODELE AUXILIARE ---
        public class LoginResponse
        {
            // Folosim acest atribut pentru a fi siguri că prinde "token" din JSON
            [System.Text.Json.Serialization.JsonPropertyName("token")]
            public string Token { get; set; } = "";
        }

        private class CreateOutfitRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("occasion")]
            public string Occasion { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("clothingItemIds")]
            public List<int> ClothingItemIds { get; set; } = new();

            [System.Text.Json.Serialization.JsonPropertyName("isFavorite")]
            public bool IsFavorite { get; set; }
        }

        private class UpdateOutfitRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("occasion")]
            public string Occasion { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("clothingItemIds")]
            public List<int> ClothingItemIds { get; set; } = new();

            [System.Text.Json.Serialization.JsonPropertyName("isFavorite")]
            public bool IsFavorite { get; set; }
        }

        private class ChangePasswordRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("currentPassword")]
            public string CurrentPassword { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("newPassword")]
            public string NewPassword { get; set; } = "";
        }

        private class DeleteAccountRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("currentPassword")]
            public string CurrentPassword { get; set; } = "";
        }

        private static UpdateOutfitRequest BuildUpdateOutfitRequest(Outfit updatedOutfit)
        {
            return new UpdateOutfitRequest
            {
                Name = string.IsNullOrWhiteSpace(updatedOutfit.Name) ? "Styled Look" : updatedOutfit.Name,
                Occasion = string.IsNullOrWhiteSpace(updatedOutfit.Occasion) ? "Daily" : updatedOutfit.Occasion,
                ClothingItemIds = updatedOutfit.Items
                    .Where(item => item.Id > 0)
                    .Select(item => item.Id)
                    .Distinct()
                    .ToList(),
                IsFavorite = updatedOutfit.IsFavorite
            };
        }

        private static string BuildOutfitSaveErrorMessage(System.Net.HttpStatusCode statusCode, string? rawResponse)
        {
            var details = ExtractApiError(rawResponse);

            return statusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "Nu esti autentificat. Te rog sa te reloghezi si sa incerci din nou.",
                System.Net.HttpStatusCode.Forbidden => "Nu ai permisiunea sa salvezi acest outfit pe acest cont.",
                System.Net.HttpStatusCode.BadRequest => string.IsNullOrWhiteSpace(details)
                    ? "Backendul a respins outfitul. Verifica numele, stilul si itemurile selectate."
                    : $"Backendul a respins outfitul: {details}",
                System.Net.HttpStatusCode.NotFound => "Endpointul de salvare outfit nu a fost gasit in Wardrobe API.",
                System.Net.HttpStatusCode.Conflict => string.IsNullOrWhiteSpace(details)
                    ? "Exista deja un outfit conflictual sau backendul a raportat un conflict."
                    : details,
                _ => string.IsNullOrWhiteSpace(details)
                    ? $"Salvarea a esuat cu codul {(int)statusCode}."
                    : $"Salvarea a esuat ({(int)statusCode}): {details}"
            };
        }

        private static string? ExtractApiError(string? rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return null;
            }

            var trimmed = rawResponse.Trim();

            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(trimmed);
                var root = document.RootElement;

                if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var messageProp) &&
                        messageProp.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        return messageProp.GetString();
                    }

                    if (root.TryGetProperty("title", out var titleProp) &&
                        titleProp.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        return titleProp.GetString();
                    }

                    if (root.TryGetProperty("error", out var errorProp) &&
                        errorProp.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        return errorProp.GetString();
                    }
                }
            }
            catch
            {
                // Fallback to raw text below.
            }

            return trimmed;
        }

        public class SaveOutfitResult
        {
            public bool Success { get; init; }
            public string Message { get; init; } = "";
            public int? StatusCode { get; init; }

            public static SaveOutfitResult Ok(string message) => new()
            {
                Success = true,
                Message = message
            };

            public static SaveOutfitResult Fail(string message, int? statusCode = null) => new()
            {
                Success = false,
                Message = message,
                StatusCode = statusCode
            };
        }

        public class AccountActionResult
        {
            public bool Success { get; init; }
            public string Message { get; init; } = "";

            public static AccountActionResult Ok(string message) => new()
            {
                Success = true,
                Message = message
            };

            public static AccountActionResult Fail(string message) => new()
            {
                Success = false,
                Message = message
            };
        }

        private async Task LogToBrowserConsoleAsync(string level, string message)
        {
            try
            {
                await _js.InvokeVoidAsync($"console.{level}", message);
            }
            catch
            {
                // Logging-ul nu trebuie să rupă fluxul principal.
            }
        }

        private static string BuildGenericAccountErrorMessage(System.Net.HttpStatusCode statusCode, string? rawResponse, string fallbackMessage)
        {
            var details = ExtractApiError(rawResponse);

            return statusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => string.IsNullOrWhiteSpace(details) ? fallbackMessage : details,
                System.Net.HttpStatusCode.Unauthorized => "Please log in again and try once more.",
                System.Net.HttpStatusCode.Forbidden => "You are not allowed to perform this action on this account.",
                System.Net.HttpStatusCode.NotFound => "This account endpoint is not available yet.",
                _ => string.IsNullOrWhiteSpace(details) ? fallbackMessage : details
            };
        }

        private static string BuildChangePasswordErrorMessage(System.Net.HttpStatusCode statusCode, string? rawResponse)
        {
            var details = ExtractApiError(rawResponse);
            var normalizedDetails = details?.ToLowerInvariant() ?? string.Empty;

            if (normalizedDetails.Contains("current password") ||
                normalizedDetails.Contains("incorrect password") ||
                normalizedDetails.Contains("invalid password") ||
                normalizedDetails.Contains("wrong password"))
            {
                return "Current password is incorrect.";
            }

            return statusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => string.IsNullOrWhiteSpace(details)
                    ? "Current password is incorrect."
                    : details,
                System.Net.HttpStatusCode.Unauthorized => "Please log in again and try once more.",
                System.Net.HttpStatusCode.Forbidden => "You are not allowed to change the password for this account.",
                System.Net.HttpStatusCode.NotFound => "This password endpoint is not available yet.",
                _ => string.IsNullOrWhiteSpace(details)
                    ? "Password could not be updated."
                    : details
            };
        }

    }
}
