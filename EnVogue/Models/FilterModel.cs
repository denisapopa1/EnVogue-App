namespace EnVogue.Models
{
    public class FilterModel
    {
        public string SearchTerm { get; set; } = "";

        public string SelectedCategory { get; set; } = "All";
        public string SelectedSeason { get; set; } = "All";

        public string Subcategory { get; set; } = "";
        public string Color { get; set; } = "";
        public string Style { get; set; } = "";

        public string SortBy { get; set; } = "Newest";
    }
}
