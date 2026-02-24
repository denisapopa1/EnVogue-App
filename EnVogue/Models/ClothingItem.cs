namespace EnVogue.Models
{
    public class ClothingItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";

        // 💡 ADAUGĂ ACESTE CÂMPURI NOI
        public string Subcategory { get; set; } = "";
        public string Season { get; set; } = "";
        public string Style { get; set; } = "";
        // -----------------------------

        public string Color { get; set; } = "";
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public DateTime AddedDate { get; set; } = DateTime.Now;
        public List<string> Tags { get; set; } = new();
        public bool IsFavorite { get; set; } = false;
        public string Brand { get; set; } = ""; // 💡 Noua proprietate

    }

    public class Outfit
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public List<ClothingItem> Items { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsFavorite { get; set; } = false;
        public string Occasion { get; set; } = "";
    }

}
