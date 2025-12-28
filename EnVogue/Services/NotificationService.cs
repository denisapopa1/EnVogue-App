namespace EnVogue.Services
{
    public class NotificationService
    {
        public event Action<string, string>? OnShow;

        // Tipuri: success, error, info
        public void Show(string message, string type = "success")
        {
            OnShow?.Invoke(message, type);
        }
    }
}
