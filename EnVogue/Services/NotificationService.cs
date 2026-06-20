using System;

namespace EnVogue.Services
{
    public class NotificationService
    {
        public event Action<string, string>? OnShow;

        public void Show(string message, string type = "success")
        {
            OnShow?.Invoke(message, type);
        }
    }
}
