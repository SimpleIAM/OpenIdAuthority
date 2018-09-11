// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.OpenIdAuthority
{
    public static class OpenIdAuthorityConstants
    {
        public static class Configuration
        {
            public const string LogoutIdParameter = "id";
            public const string CheckSessionIFrame = "/connect/checksession";
        }

        public static class ConfigurationSections
        {
            public const string Apis = "Apis";
            public const string Apps = "Apps";
            public const string Hosting = "Hosting";
            public const string IdScopes = "IdScopes";
        }

        public static class StandardClaims
        {
            public const string Name = "name";
            public const string Email = "email";
            public const string EmailVerified = "email_verified";
        }
    }
}
