namespace EnVogue.Services
{
    public class UserState
    {

        public string? Email { get; private set; }
        public bool IsLoggedIn => Email != null;

        public void Login(string email)
        {
            Email = email;
        }

        public void Logout()
        {
            Email = null;
        }
    }
}
