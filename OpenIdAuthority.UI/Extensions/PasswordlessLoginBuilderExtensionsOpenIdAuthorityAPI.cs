// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using SimpleIAM.OpenIdAuthority.UI;
using SimpleIAM.PasswordlessLogin.Configuration;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PasswordlessLoginBuilderExtensionsOpenIdAuthorityAPI
    {
        /// <summary>
        /// Adds ability to use views embedded in packages. Add before calling services.UseMvc() or services.UseEndpoints().
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static PasswordlessLoginBuilder AddOpenIdAuthorityUI(this PasswordlessLoginBuilder builder)
        {
            var assembly = typeof(PasswordlessLoginBuilderExtensionsOpenIdAuthorityAPI).GetTypeInfo().Assembly;

            var embeddedFileProvider = new EmbeddedFileProvider(assembly, "SimpleIAM.OpenIdAuthority.UI.UI");

            builder.Services.AddControllersWithViews()
                .AddRazorRuntimeCompilation(options => options.FileProviders.Add(embeddedFileProvider));

            builder.Services.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new EmbeddedViewLocator());
            });

            builder.Services.TryAddSingleton<ITempDataProvider, CookieTempDataProvider>();

            return builder;
        }
    }
}
