using System;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using SimpleIAM.PasswordlessLogin.Services;

namespace SimpleIAM.PasswordlessLogin.Services
{
    public class OpenIdAuthoritySignInService : ISignInService
    {
        private readonly HttpContext _httpContext;
        private readonly IEventService _events;

        public OpenIdAuthoritySignInService(IHttpContextAccessor httpContextAccessor, IEventService events)
        {
            _httpContext = httpContextAccessor.HttpContext;
            _events = events;
        }

        public async Task SignInAsync(string subjectId, string username, AuthenticationProperties authProps)
        {
            await _events.RaiseAsync(new UserLoginSuccessEvent(username, subjectId, username));

            await _httpContext.SignInAsync(subjectId, username, authProps);
        }

        public async Task SignOutAsync(string subjectId, string username)
        {
            await _httpContext.SignOutAsync();
            await _events.RaiseAsync(new UserLogoutSuccessEvent(subjectId, username));
        }
    }
}
