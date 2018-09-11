// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleIAM.PasswordlessLogin.Configuration;

namespace SimpleIAM.OpenIdAuthority.Configuration
{
    internal class ReconfigureCookieOptions : IConfigureNamedOptions<CookieAuthenticationOptions>
    {
        private readonly IdProviderConfig _idProviderConfig;
        public ReconfigureCookieOptions(IdProviderConfig idProviderConfig)
        {
            _idProviderConfig = idProviderConfig;
        }

        public void Configure(CookieAuthenticationOptions options)
        {
        }

        public void Configure(string name, CookieAuthenticationOptions options)
        {
            options.ConfigurePasswordlessAuthenticationOptions(_idProviderConfig.Urls);
        }
    }
}
