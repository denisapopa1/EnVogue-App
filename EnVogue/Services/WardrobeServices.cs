using EnVogue.Models;
using Blazored.LocalStorage;
using System.Text.Json;

namespace EnVogue.Services
{
    public class WardrobeServices
    {
        private List<ClothingItem> _clothingItems = new();
        private List<Outfit> _outfits = new();
        private readonly ILocalStorageService _localStorage;
        private const string StorageKey = "my_wardrobe_data";
        private const string OutfitsKey = "my_outfits_data";
        // Variabilă care reține mailul utilizatorului logat
        private string _loggedInUserEmail;
        public event Action? OnChange;
        // Metoda care primește mail-ul de la Login
        public void SetLoggedInUser(string email)
        {
            _loggedInUserEmail = email;
        }

        // Metoda pe care o folosește pagina Home pentru a extrage numele
        public string GetLoggedInUserEmail()
        {
            return _loggedInUserEmail;
        }

        public WardrobeServices(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        // --- LOGICĂ DE PERSISTENȚĂ ---

        // Trebuie apelată în MainLayout sau la pornirea aplicației
        public async Task InitializeAsync()
        {
            try
            {
                var savedItems = await _localStorage.GetItemAsync<List<ClothingItem>>(StorageKey);
                if (savedItems != null) _clothingItems = savedItems;

                var savedOutfits = await _localStorage.GetItemAsync<List<Outfit>>(OutfitsKey);
                if (savedOutfits != null) _outfits = savedOutfits;

                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                // Previne crash-ul dacă browserul nu e gata încă
                Console.WriteLine($"Storage not ready: {ex.Message}");
            }
        }
        private async Task SaveChangesAsync()
        {
            await _localStorage.SetItemAsync(StorageKey, _clothingItems);
            await _localStorage.SetItemAsync(OutfitsKey, _outfits);
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        // --- HAINE ---

        public List<ClothingItem> GetAllClothes() => _clothingItems;

        public List<ClothingItem> GetClothesByCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) return _clothingItems;
            return _clothingItems.Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task AddClothingItem(ClothingItem item)
        {
            if (string.IsNullOrEmpty(item.Id)) item.Id = Guid.NewGuid().ToString();
            _clothingItems.Add(item);
            await SaveChangesAsync();
        }

        public async Task UpdateClothingItem(ClothingItem updatedItem)
        {
            var index = _clothingItems.FindIndex(i => i.Id == updatedItem.Id);
            if (index != -1)
            {
                _clothingItems[index] = updatedItem;
                await SaveChangesAsync();
            }
        }

        public async Task RemoveClothingItem(string id)
        {
            var item = _clothingItems.FirstOrDefault(c => c.Id == id);
            if (item != null)
            {
                _clothingItems.Remove(item);
                await SaveChangesAsync();
            }
        }

        // --- OUTFITS ---

        public List<Outfit> GetAllOutfits() => _outfits;
        public List<Outfit> GetFavoriteOutfits() => _outfits.Where(o => o.IsFavorite).ToList();

        public async Task AddOutfit(Outfit outfit)
        {
            if (string.IsNullOrEmpty(outfit.Id)) outfit.Id = Guid.NewGuid().ToString();
            _outfits.Add(outfit);
            await SaveChangesAsync();
        }

        public async Task ToggleFavorite(string outfitId)
        {
            var outfit = _outfits.FirstOrDefault(o => o.Id == outfitId);
            if (outfit != null)
            {
                outfit.IsFavorite = !outfit.IsFavorite;
                await SaveChangesAsync();
            }
        }

        // --- GENERATOR LOGIC (Rămâne neschimbat, dar apelează metodele interne) ---

        public Outfit GenerateOutfitWithRules(string? occasion = null, string? preferredColors = null)
        {
            var outfit = new Outfit
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Outfit {_outfits.Count + 1}",
                Occasion = occasion ?? "Casual",
                Items = new List<ClothingItem>()
            };

            var allTops = GetClothesByCategory("tops");
            var allBottoms = GetClothesByCategory("bottoms");
            var allDresses = GetClothesByCategory("dresses");
            var allOuterwear = GetClothesByCategory("outerwear");
            var allShoes = GetClothesByCategory("shoes");
            var allAccessories = GetClothesByCategory("accessories");

            var colorList = preferredColors?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().ToLowerInvariant()).ToList() ?? new List<string>();

            var tops = allTops;
            var bottoms = allBottoms;
            var dresses = allDresses;
            var outerwear = allOuterwear;
            var shoes = allShoes;
            var accessories = allAccessories;

            if (colorList.Any())
            {
                var filteredTops = allTops.Where(t => colorList.Any(c => t.Color.ToLowerInvariant().Contains(c))).ToList();
                var filteredBottoms = allBottoms.Where(b => colorList.Any(c => b.Color.ToLowerInvariant().Contains(c))).ToList();
                var filteredDresses = allDresses.Where(d => colorList.Any(c => d.Color.ToLowerInvariant().Contains(c))).ToList();
                var filteredShoes = allShoes.Where(s => NeutralColors.Contains(s.Color.ToLowerInvariant()) ||
                    colorList.Any(c => s.Color.ToLowerInvariant().Contains(c))).ToList();

                if ((filteredTops.Any() && filteredBottoms.Any()) || filteredDresses.Any())
                {
                    tops = filteredTops.Any() ? filteredTops : allTops;
                    bottoms = filteredBottoms.Any() ? filteredBottoms : allBottoms;
                    dresses = filteredDresses.Any() ? filteredDresses : allDresses;
                    shoes = filteredShoes.Any() ? filteredShoes : allShoes;
                }
            }

            if (dresses.Any() && Random.Shared.Next(2) == 0)
            {
                var dress = dresses[Random.Shared.Next(dresses.Count)];
                outfit.Items.Add(dress);
                if (shoes.Any())
                {
                    var matchingShoes = shoes.Where(s => ColorsMatch(dress.Color, s.Color)).ToList();
                    outfit.Items.Add(matchingShoes.Any() ? matchingShoes[Random.Shared.Next(matchingShoes.Count)] : shoes[Random.Shared.Next(shoes.Count)]);
                }
            }
            else
            {
                ClothingItem? selectedTop = null;
                ClothingItem? selectedBottom = null;
                if (tops.Any() && bottoms.Any())
                {
                    selectedTop = tops[Random.Shared.Next(tops.Count)];
                    var matchingBottoms = bottoms.Where(b => ColorsMatch(selectedTop.Color, b.Color)).ToList();
                    selectedBottom = matchingBottoms.Any() ? matchingBottoms[Random.Shared.Next(matchingBottoms.Count)] : bottoms[Random.Shared.Next(bottoms.Count)];
                    outfit.Items.Add(selectedTop);
                    outfit.Items.Add(selectedBottom);
                }
                else if (tops.Any()) { selectedTop = tops[Random.Shared.Next(tops.Count)]; outfit.Items.Add(selectedTop); }
                else if (bottoms.Any()) { selectedBottom = bottoms[Random.Shared.Next(bottoms.Count)]; outfit.Items.Add(selectedBottom); }

                if (shoes.Any() && selectedBottom != null)
                {
                    var matchingShoes = shoes.Where(s => ColorsMatch(selectedBottom.Color, s.Color)).ToList();
                    outfit.Items.Add(matchingShoes.Any() ? matchingShoes[Random.Shared.Next(matchingShoes.Count)] : shoes[Random.Shared.Next(shoes.Count)]);
                }
                else if (shoes.Any() && selectedTop != null)
                {
                    var matchingShoes = shoes.Where(s => ColorsMatch(selectedTop.Color, s.Color)).ToList();
                    outfit.Items.Add(matchingShoes.Any() ? matchingShoes[Random.Shared.Next(matchingShoes.Count)] : shoes[Random.Shared.Next(shoes.Count)]);
                }
            }

            if (outerwear.Any() && (occasion?.ToLowerInvariant().Contains("office") == true || occasion?.ToLowerInvariant().Contains("formal") == true))
                outfit.Items.Add(outerwear[Random.Shared.Next(outerwear.Count)]);

            if (accessories.Any() && Random.Shared.Next(3) == 0)
                outfit.Items.Add(accessories[Random.Shared.Next(accessories.Count)]);

            return outfit.Items.Count > 1 ? outfit : new Outfit { Name = "Empty", Occasion = outfit.Occasion };
        }

        private static readonly HashSet<string> NeutralColors = new(StringComparer.OrdinalIgnoreCase)
        { "black", "white", "gray", "grey", "beige", "brown", "navy", "cream", "tan" };

        private static readonly Dictionary<string, List<string>> ColorMatching = new(StringComparer.OrdinalIgnoreCase)
        {
            ["red"] = new() { "black", "white", "navy", "beige", "gray" },
            ["blue"] = new() { "white", "black", "beige", "gray", "brown" },
            ["navy"] = new() { "white", "beige", "gray", "red" },
            ["green"] = new() { "white", "beige", "brown", "black" },
            ["yellow"] = new() { "white", "black", "navy", "gray" },
            ["pink"] = new() { "white", "black", "gray", "navy" },
            ["purple"] = new() { "white", "black", "gray" },
            ["orange"] = new() { "navy", "white", "black", "brown" }
        };

        private static bool ColorsMatch(string color1, string color2)
        {
            var c1 = color1.ToLowerInvariant();
            var c2 = color2.ToLowerInvariant();
            if (NeutralColors.Contains(c1) || NeutralColors.Contains(c2)) return true;
            if (c1 == c2) return true;
            foreach (var kvp in ColorMatching)
            {
                if (c1.Contains(kvp.Key) && kvp.Value.Any(v => c2.Contains(v))) return true;
                if (c2.Contains(kvp.Key) && kvp.Value.Any(v => c1.Contains(v))) return true;
            }
            return false;
        }
    }
}