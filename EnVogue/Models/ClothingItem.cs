using System.Text.Json.Serialization;

namespace EnVogue.Models
{
    public class ClothingItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        // Schimbă în "CategoryName" (cu C mare) pentru a se potrivi cu DTO-ul din API
        public string? CategoryName { get; set; }
        public int? CategoryId { get; set; }
        public string? SubcategoryName { get; set; }
        // ACEASTA ESTE LINIA CARE LIPSEȘTE ȘI CAUZEAZĂ EROAREA:
        public int? SubcategoryId { get; set; }
        public string? Brand { get; set; }
        public string? Season { get; set; }
        public string? Style { get; set; }
        public string? Color { get; set; }
        public string? ImagePath { get; set; }
        public bool IsFavorite { get; set; }
        public string UserId { get; set; } = "";
    } // <-- Aici se închide ClothingItem

    public class Outfit
    {
        public int Id { get; set; } // Backend-ul folosește int
        public string Name { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public bool IsFavorite { get; set; }
        public string? Occasion { get; set; }
        public string? CreationSource { get; set; }
        public List<ClothingItem> ClothingItems { get; set; } = new();

        [JsonIgnore]
        public List<ClothingItem> Items
        {
            get => ClothingItems;
            set => ClothingItems = value ?? new();
        }

        [JsonIgnore]
        public bool IsAiGenerated =>
            string.Equals(CreationSource, "AI", StringComparison.OrdinalIgnoreCase);
    }

} // <-- Aici se închide Namespace-ul (Dacă lipsește asta, dă erori peste tot)
