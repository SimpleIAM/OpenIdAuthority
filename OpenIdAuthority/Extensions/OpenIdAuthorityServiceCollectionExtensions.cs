// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SimpleIAM.OpenIdAuthority;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Services;
using SimpleIAM.PasswordlessLogin;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Services;
using SimpleIAM.PasswordlessLogin.Stores;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenIdAuthorityServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenIdAuthority(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            var idProviderConfig = new IdProviderConfig();
            configuration.Bind(PasswordlessLoginConstants.ConfigurationSections.IdProvider, idProviderConfig);
            services.AddSingleton(idProviderConfig);

            var hostingConfig = new HostingConfig();
            configuration.Bind(OpenIdAuthorityConstants.ConfigurationSections.Hosting, hostingConfig);
            services.AddSingleton(hostingConfig);

            var appConfigs = configuration.GetAppConfigs();
            var appService = new DefaultApplicationService(appConfigs);
            services.TryAddSingleton<IApplicationService>(appService);

            var clients = AppConfigHelper.GetClientsFromAppConfig(appConfigs);
            var apps = AppConfigHelper.GetAppsFromClients(clients);
            var appStore = new InMemoryAppStore(apps);
            services.TryAddSingleton<IAppStore>(appStore);

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

            services.TryAddTransient<ISignInService, OpenIdAuthoritySignInService>();
            services.TryAddTransient<SimpleIAM.PasswordlessLogin.Services.Password.IReadOnlyPasswordService, SimpleIAM.PasswordlessLogin.Services.Password.DefaultPasswordService>();
            services.TryAddTransient<IResourceOwnerPasswordValidator, ResourceOwnerPasswordValidator>();

            services.AddPasswordlessLogin(configuration, env);

            services.AddIdentityServer(options =>
            {
                options.UserInteraction.LoginUrl = idProviderConfig.Urls.SignIn;
                options.UserInteraction.LogoutUrl = idProviderConfig.Urls.SignOut;
                options.UserInteraction.LogoutIdParameter = OpenIdAuthorityConstants.Configuration.LogoutIdParameter;
                options.UserInteraction.ErrorUrl = idProviderConfig.Urls.Error;
                options.Authentication.CookieLifetime = TimeSpan.FromMinutes(idProviderConfig.DefaultSessionLengthMinutes);
            })
                .AddDeveloperSigningCredential() //todo: replace
                .AddInMemoryApiResources(apiResources)
                .AddInMemoryClients(clients)
                .AddProfileService<ProfileService>()                
                .AddInMemoryIdentityResources(idScopes);

            services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, ReconfigureCookieOptions>();

            return services;
        }
    }
}
