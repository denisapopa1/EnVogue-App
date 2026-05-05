namespace EnVogue.Services
{
    public static class ApiEndpoints
    {
        public static Uri BaseAddress { get; set; } = new("http://localhost:5207/");

        public static string ImageUrl(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            return new Uri(BaseAddress, $"uploads/images/{fileName}").ToString();
        }
    }
}
