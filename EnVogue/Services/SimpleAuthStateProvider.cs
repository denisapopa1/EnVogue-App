using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace EnVogue.Services
{
    public class SimpleAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Returnăm un utilizator gol (neautentificat) pentru a permite pornirea aplicației
            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);
            return Task.FromResult(new AuthenticationState(user));
        }
    }
}