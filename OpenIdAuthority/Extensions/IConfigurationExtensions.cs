using SimpleIAM.OpenIdAuthority;
using SimpleIAM.OpenIdAuthority.Configuration;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    public static class IConfigurationExtensions
    {
        public static List<AppConfig> GetAppConfigs(this IConfiguration configuration)
        {
            return configuration.GetSection(OpenIdAuthorityConstants.ConfigurationSections.Apps).Get<List<AppConfig>>() ?? new List<AppConfig>();
        }        
    }
}
