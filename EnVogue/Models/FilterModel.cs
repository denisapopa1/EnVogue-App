namespace EnVogue.Models
{
    public class FilterModel
    {
        public string SearchTerm { get; set; } = "";

        // Redenumim pentru a fi clar că acestea sunt valorile selectate în UI
        public string SelectedCategory { get; set; } = "All";
        public string SelectedSeason { get; set; } = "All";

        public string Subcategory { get; set; } = "";
        public string Color { get; set; } = "";
        public string Style { get; set; } = "";

        // Aceasta este variabila care îți lipsea probabil pentru dropdown-ul de sortare
        public string SortBy { get; set; } = "Newest";
    }
}