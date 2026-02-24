using System;

namespace EnVogue.Services
{
    public class NotificationService
    {
        // Evenimentul la care se abonează ToastContainer.razor
        public event Action<string, string>? OnShow;

        // Aceasta este metoda pe care o cauți
        public void Show(string message, string type = "success")
        {
            // Debugging: apare în consola browserului (F12)
            Console.WriteLine($"Notification triggered: {message} ({type})");

            // Declanșează evenimentul pentru componenta vizuală
            OnShow?.Invoke(message, type);
        }
    }
}