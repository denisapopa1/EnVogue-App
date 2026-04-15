namespace EnVogue.Models
{
    public class Subcategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        // Aceasta este legătura cu tabelul de Categorii (Foreign Key)
        public int CategoryId { get; set; }
    }
}
