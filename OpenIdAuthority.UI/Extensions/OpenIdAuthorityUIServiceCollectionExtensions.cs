// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using SimpleIAM.OpenIdAuthority.UI;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenIdAuthorityUIServiceCollectionExtensions
    {
        /// <summary>
        /// Adds ability to use views embedded in packages. Add before calling services.UseMvc() or services.UseEndpoints().
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddOpenIdAuthorityUI(this IServiceCollection services)
        {
            var assembly = typeof(OpenIdAuthorityUIServiceCollectionExtensions).GetTypeInfo().Assembly;

            var embeddedFileProvider = new EmbeddedFileProvider(assembly, "SimpleIAM.OpenIdAuthority.UI.UI");

            services.AddControllersWithViews()
                .AddRazorRuntimeCompilation(options => options.FileProviders.Add(embeddedFileProvider));

            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new EmbeddedViewLocator());
            });

            services.TryAddSingleton<ITempDataProvider, CookieTempDataProvider>();

            return services;
        }
    }
}
