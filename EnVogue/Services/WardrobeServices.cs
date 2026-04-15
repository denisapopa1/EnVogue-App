using EnVogue.Models;
using Blazored.LocalStorage;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace EnVogue.Services
{
    public class WardrobeServices
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;
        private readonly UserState _userState; // <--- Adaugă asta

        // Cache-ul local pentru UI
        private List<ClothingItem> _clothingItems = new();
        private List<Outfit> _outfits = new();

        // Variabilă pentru email-ul utilizatorului
        private string? _loggedInUserEmail;

        public event Action? OnChange;

        public WardrobeServices(HttpClient http, ILocalStorageService localStorage, UserState userState)
        {
            _http = http;
            _localStorage = localStorage;
            _userState = userState;
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

        public async Task InitializeAsync()
        {
            try
            {
                // 1. Încercăm să luăm datele salvate din browser
                var token = await _localStorage.GetItemAsync<string>("authToken");
                var savedEmail = await _localStorage.GetItemAsync<string>("userEmail");

                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(savedEmail))
                {
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

                        var outfits = await _http.GetFromJsonAsync<List<Outfit>>("api/outfits");
                        if (outfits != null) _outfits = outfits;
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
        // --- OUTFITS (API) ---

        public List<Outfit> GetAllOutfits() => _outfits;

        public async Task AddOutfit(Outfit outfit)
        {
            var response = await _http.PostAsJsonAsync("api/outfits", outfit);
            if (response.IsSuccessStatusCode)
            {
                var newOutfit = await response.Content.ReadFromJsonAsync<Outfit>();
                if (newOutfit != null) _outfits.Add(newOutfit);
                NotifyStateChanged();
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
        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            // ȘTERGE ȘI EMAIL-UL:
            await _localStorage.RemoveItemAsync("userEmail");

            _http.DefaultRequestHeaders.Authorization = null;
            _clothingItems = new();
            _outfits = new();
            _loggedInUserEmail = null;

            NotifyStateChanged();
        }
        public async Task ToggleFavorite(string outfitId)
        {
            var outfit = _outfits.FirstOrDefault(o => o.Id == outfitId);
            if (outfit != null)
            {
                outfit.IsFavorite = !outfit.IsFavorite;

                // Trimitem modificarea la API (OutfitController)
                var response = await _http.PutAsJsonAsync($"api/outfits/{outfitId}", outfit);

                if (response.IsSuccessStatusCode)
                {
                    NotifyStateChanged();
                }
            }
        }
        // --- LOGICA DE GENERATOR ---

        public Outfit GenerateOutfitWithRules(string? occasion = null, string? preferredColors = null)
        {
            var outfit = new Outfit
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Outfit {_outfits.Count + 1}",
                Occasion = occasion ?? "Casual",
                Items = new List<ClothingItem>()
            };

            // Luăm categoriile din cache-ul local (care a fost populat din API)
            var allTops = GetClothesByCategory("tops");
            var allBottoms = GetClothesByCategory("bottoms");
            var allDresses = GetClothesByCategory("dress"); // Atenție: Verifică dacă e "dress" sau "dresses" în baza de date
            var allOuterwear = GetClothesByCategory("outerwear");
            var allShoes = GetClothesByCategory("shoes");
            var allAccessories = GetClothesByCategory("accessories");

            // Procesăm culorile preferate
            var colorList = preferredColors?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().ToLowerInvariant()).ToList() ?? new List<string>();

            var tops = allTops;
            var bottoms = allBottoms;
            var dresses = allDresses;
            var shoes = allShoes;

            // --- LOGICA DE FILTRARE PE CULORI ---
            if (colorList.Any())
            {
                // ATENȚIE: Aici folosim proprietatea .Color din modelul tău
                tops = allTops.Where(t => colorList.Any(c => t.Color != null && t.Color.ToLower().Contains(c))).ToList();
                bottoms = allBottoms.Where(b => colorList.Any(c => b.Color != null && b.Color.ToLower().Contains(c))).ToList();
                dresses = allDresses.Where(d => colorList.Any(c => d.Color != null && d.Color.ToLower().Contains(c))).ToList();

                // Dacă nu găsește nimic pe culorile cerute, revenim la lista completă ca să nu returnăm un outfit gol
                if (!tops.Any()) tops = allTops;
                if (!bottoms.Any()) bottoms = allBottoms;
                if (!dresses.Any()) dresses = allDresses;
            }

            // --- LOGICA DE SELECȚIE (Reguli de modă) ---

            // Decidem: Rochie sau (Top + Bottom)?
            bool chooseDress = dresses.Any() && Random.Shared.Next(2) == 0;

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

            // Adăugăm accesorii (șansă de 30%)
            if (allAccessories.Any() && Random.Shared.Next(3) == 0)
                outfit.Items.Add(allAccessories[Random.Shared.Next(allAccessories.Count)]);

            return outfit.Items.Count > 0 ? outfit : new Outfit { Name = "Empty", Occasion = outfit.Occasion };
        }

        // --- MODELE AUXILIARE ---
        public class LoginResponse
        {
            // Folosim acest atribut pentru a fi siguri că prinde "token" din JSON
            [System.Text.Json.Serialization.JsonPropertyName("token")]
            public string Token { get; set; } = "";
        }
    }
}