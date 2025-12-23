namespace EnVogue.Models
{
    public class FilterModel
    {
        public string SearchTerm { get; set; } = ""; // Căutare text liberă
        public string Category { get; set; } = "";   // Filtru Categorie
        public string Subcategory { get; set; } = ""; // 💡 NOU
        public string Color { get; set; } = "";      // Filtru Culoare
        public string Season { get; set; } = "";     // Filtru Sezon
        public string Style { get; set; } = ""; // 💡 NOU
    }
}
