namespace EnVogue.Models
{
    public class DatasetCatalogManifest
    {
        public string Name { get; set; } = "";
        public string GeneratedOn { get; set; } = "";
        public int ItemCount { get; set; }
        public List<DatasetCatalogItem> Items { get; set; } = new();
    }

    public class DatasetCatalogItem
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Subcategory { get; set; } = "";
        public string Season { get; set; } = "";
        public string Style { get; set; } = "";
        public string Color { get; set; } = "";
        public string Brand { get; set; } = "";
        public string RelativeImagePath { get; set; } = "";
    }

    public class DatasetImportProgress
    {
        public int Total { get; set; }
        public int Processed { get; set; }
        public int Imported { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public string CurrentItemName { get; set; } = "";
    }

    public class DatasetImportResult
    {
        public bool Success { get; set; }
        public int Total { get; set; }
        public int Imported { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public string Message { get; set; } = "";
    }
}
