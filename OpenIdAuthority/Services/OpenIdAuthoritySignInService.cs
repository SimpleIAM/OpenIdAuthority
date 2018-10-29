// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using SimpleIAM.PasswordlessLogin;
using SimpleIAM.PasswordlessLogin.Services;

namespace SimpleIAM.OpenIdAuthority.Services
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

        public async Task SignOutAsync()
        {
            await _httpContext.SignOutAsync();
            await _events.RaiseAsync(new UserLogoutSuccessEvent(
                _httpContext.User.GetSubjectId(), 
                _httpContext.User.GetDisplayName()
                ));

            // We're signed out now, so the UI for this request should show an anonymous user
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}
