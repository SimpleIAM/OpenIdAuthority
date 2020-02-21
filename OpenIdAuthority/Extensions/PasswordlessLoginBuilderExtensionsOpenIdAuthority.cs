// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SimpleIAM.OpenIdAuthority;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Services;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Services;
using SimpleIAM.PasswordlessLogin.Stores;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PasswordlessLoginBuilderExtensionsOpenIdAuthority
    {
        public static PasswordlessLoginBuilder AddOpenIdAuthority(this PasswordlessLoginBuilder builder, IConfiguration configuration)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var hostingConfig = new HostingConfig();
            configuration.Bind(OpenIdAuthorityConstants.ConfigurationSections.Hosting, hostingConfig);
            builder.Services.AddSingleton(hostingConfig);

            var appConfigs = configuration.GetAppConfigs();
            var appService = new DefaultApplicationService(appConfigs);
            builder.Services.TryAddSingleton<IApplicationService>(appService);

            var clients = AppConfigHelper.GetClientsFromAppConfig(appConfigs);
            var apps = AppConfigHelper.GetAppsFromClients(clients);
            var appStore = new InMemoryAppStore(apps);
            builder.Services.TryAddSingleton<IAppStore>(appStore);

            var idScopeConfig = configuration.GetSection(OpenIdAuthorityConstants.ConfigurationSections.IdScopes).Get<List<IdScopeConfig>>() ?? new List<IdScopeConfig>();
            var idScopes = idScopeConfig.Select(x => new IdentityResource(x.Name, x.IncludeClaimTypes) { Required = x.Required }).ToList();
            idScopes.AddRange(new List<IdentityResource>() {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Phone(),
                new IdentityResources.Address(),
            });

            var apiConfigs = configuration.GetSection(OpenIdAuthorityConstants.ConfigurationSections.Apis).Get<List<ApiConfig>>() ?? new List<ApiConfig>();
            var apiResources = apiConfigs.Select(x => new ApiResource(x.Url, x.IncludeClaimTypes)
            {
                ApiSecrets = x.Secrets?.ToList()?.Select(y=> new Secret(y.Sha256())).ToList(),
                Scopes = x.Scopes?.ToList()?.Select(y => new Scope(y)).ToList()
            }).ToList();

            builder.Services.AddTransient<ISignInService, OpenIdAuthoritySignInService>(); // NOTE: This must replace the service registered by PasswordlessLogin
            builder.Services.TryAddTransient<SimpleIAM.PasswordlessLogin.Services.Password.IReadOnlyPasswordService, SimpleIAM.PasswordlessLogin.Services.Password.DefaultPasswordService>();
            builder.Services.TryAddTransient<IResourceOwnerPasswordValidator, ResourceOwnerPasswordValidator>();

            builder.Services.AddIdentityServer(options =>
            {
                options.UserInteraction.LoginUrl = builder.Options.Urls.SignIn;
                options.UserInteraction.LogoutUrl = builder.Options.Urls.SignOut;
                options.UserInteraction.LogoutIdParameter = OpenIdAuthorityConstants.Configuration.LogoutIdParameter;
                options.UserInteraction.ErrorUrl = builder.Options.Urls.Error;
                options.Authentication.CookieLifetime = TimeSpan.FromMinutes(builder.Options.DefaultSessionLengthMinutes);
            })
                .AddDeveloperSigningCredential() //todo: replace
                .AddInMemoryApiResources(apiResources)
                .AddInMemoryClients(clients)
                .AddProfileService<ProfileService>()                
                .AddInMemoryIdentityResources(idScopes);

            builder.Services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, ReconfigureCookieOptions>();

            return builder;
        }
    }
}
