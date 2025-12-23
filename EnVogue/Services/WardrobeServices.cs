using EnVogue.Models;

namespace EnVogue.Services
{
    public class WardrobeServices
    {
        private readonly List<ClothingItem> _clothingItems = new();
        private readonly List<Outfit> _outfits = new();

        public event Action? OnChange;

        public List<ClothingItem> GetAllClothes() => _clothingItems;

        public List<ClothingItem> GetClothesByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return _clothingItems;

            return _clothingItems.Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public void AddClothingItem(ClothingItem item)
        {
            _clothingItems.Add(item);
            OnChange?.Invoke();
        }

        public void RemoveClothingItem(string id)
        {
            var item = _clothingItems.FirstOrDefault(c => c.Id == id);
            if (item != null)
            {
                _clothingItems.Remove(item);
                OnChange?.Invoke();
            }
        }

        public List<Outfit> GetAllOutfits() => _outfits;

        public List<Outfit> GetFavoriteOutfits() => _outfits.Where(o => o.IsFavorite).ToList();

        public void AddOutfit(Outfit outfit)
        {
            _outfits.Add(outfit);
            OnChange?.Invoke();
        }

        public void ToggleFavorite(string outfitId)
        {
            var outfit = _outfits.FirstOrDefault(o => o.Id == outfitId);
            if (outfit != null)
            {
                outfit.IsFavorite = !outfit.IsFavorite;
                OnChange?.Invoke();
            }
        }

        public Outfit GenerateOutfitWithRules(string? occasion = null, string? preferredColors = null)
        {
            var outfit = new Outfit
            {
                Name = $"Outfit {_outfits.Count + 1}",
                Occasion = occasion ?? "Casual"
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
                    outfit.Items.Add(matchingShoes.Any()
                        ? matchingShoes[Random.Shared.Next(matchingShoes.Count)]
                        : shoes[Random.Shared.Next(shoes.Count)]);
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
                    selectedBottom = matchingBottoms.Any()
                        ? matchingBottoms[Random.Shared.Next(matchingBottoms.Count)]
                        : bottoms[Random.Shared.Next(bottoms.Count)];

                    outfit.Items.Add(selectedTop);
                    outfit.Items.Add(selectedBottom);
                }
                else if (tops.Any())
                {
                    selectedTop = tops[Random.Shared.Next(tops.Count)];
                    outfit.Items.Add(selectedTop);
                }
                else if (bottoms.Any())
                {
                    selectedBottom = bottoms[Random.Shared.Next(bottoms.Count)];
                    outfit.Items.Add(selectedBottom);
                }

                if (shoes.Any() && selectedBottom != null)
                {
                    var matchingShoes = shoes.Where(s => ColorsMatch(selectedBottom.Color, s.Color)).ToList();
                    outfit.Items.Add(matchingShoes.Any()
                        ? matchingShoes[Random.Shared.Next(matchingShoes.Count)]
                        : shoes[Random.Shared.Next(shoes.Count)]);
                }
                else if (shoes.Any() && selectedTop != null)
                {
                    var matchingShoes = shoes.Where(s => ColorsMatch(selectedTop.Color, s.Color)).ToList();
                    outfit.Items.Add(matchingShoes.Any()
                        ? matchingShoes[Random.Shared.Next(matchingShoes.Count)]
                        : shoes[Random.Shared.Next(shoes.Count)]);
                }
            }

            if (outerwear.Any() && (occasion?.ToLowerInvariant().Contains("office") == true ||
                occasion?.ToLowerInvariant().Contains("formal") == true))
            {
                outfit.Items.Add(outerwear[Random.Shared.Next(outerwear.Count)]);
            }

            if (accessories.Any() && Random.Shared.Next(3) == 0)
            {
                outfit.Items.Add(accessories[Random.Shared.Next(accessories.Count)]);
            }

            var hasDress = outfit.Items.Any(i => i.Category.Equals("dresses", StringComparison.OrdinalIgnoreCase));
            var hasTop = outfit.Items.Any(i => i.Category.Equals("tops", StringComparison.OrdinalIgnoreCase));
            var hasBottom = outfit.Items.Any(i => i.Category.Equals("bottoms", StringComparison.OrdinalIgnoreCase));
            var hasShoes = outfit.Items.Any(i => i.Category.Equals("shoes", StringComparison.OrdinalIgnoreCase));

            var hasValidOutfit = (hasDress && hasShoes) || (hasTop && hasBottom && hasShoes);

            if (!hasValidOutfit)
            {
                outfit.Items.Clear();
            }

            return outfit;
        }

        private static readonly HashSet<string> NeutralColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "black", "white", "gray", "grey", "beige", "brown", "navy", "cream", "tan"
    };

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

            if (NeutralColors.Contains(c1) || NeutralColors.Contains(c2))
                return true;

            if (c1 == c2)
                return true;

            foreach (var kvp in ColorMatching)
            {
                if (c1.Contains(kvp.Key) && kvp.Value.Any(v => c2.Contains(v)))
                    return true;
                if (c2.Contains(kvp.Key) && kvp.Value.Any(v => c1.Contains(v)))
                    return true;
            }

            return false;
        }
        public void UpdateClothingItem(ClothingItem updatedItem)
        {
            var index = _clothingItems.FindIndex(i => i.Id == updatedItem.Id);
            if (index != -1)
            {
                _clothingItems[index] = updatedItem;
                OnChange?.Invoke();
            }
        }
    }

}