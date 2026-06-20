using System.Text.Json.Serialization;

namespace EnVogue.Models
{
    public class ClothingItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? CategoryName { get; set; }
        public int? CategoryId { get; set; }
        public string? SubcategoryName { get; set; }
        public int? SubcategoryId { get; set; }
        public string? Brand { get; set; }
        public string? Season { get; set; }
        public string? Style { get; set; }
        public string? Color { get; set; }
        public string? ImagePath { get; set; }
        public bool IsFavorite { get; set; }
        public string UserId { get; set; } = "";
    }

    public class Outfit
    {
        public int Id { get; set; }
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

}
