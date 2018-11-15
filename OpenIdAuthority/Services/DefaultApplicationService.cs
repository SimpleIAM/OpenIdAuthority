// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.PasswordlessLogin.Services;
using System.Collections.Generic;
using System.Linq;

namespace SimpleIAM.OpenIdAuthority.Services
{
    public class DefaultApplicationService : IApplicationService
    {
        private readonly IEnumerable<AppConfig> _appConfig;
        public DefaultApplicationService(IEnumerable<AppConfig> appConfig)
        {
            _appConfig = appConfig;
        }

        public bool ApplicationExists(string applicationId)
        {
            return _appConfig.Any(x => x.ClientId == applicationId);
        }

        public string GetApplicationName(string applicationId)
        {
            return _appConfig.FirstOrDefault(x => x.ClientId == applicationId)?.Name;
        }

        public IDictionary<string, string> GetApplicationProperties(string applicationId)
        {
            return _appConfig.FirstOrDefault(x => x.ClientId == applicationId)?.CustomProperties ??
                new Dictionary<string, string>();
        }
    }
}
