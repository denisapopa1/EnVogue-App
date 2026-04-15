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
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";

        // Dacă ClothingItem este definit chiar deasupra, aici va funcționa!
        public List<ClothingItem> Items { get; set; } = new();

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsFavorite { get; set; } = false;
        public string Occasion { get; set; } = "";
    } // <-- Aici se închide Outfit
} // <-- Aici se închide Namespace-ul (Dacă lipsește asta, dă erori peste tot)