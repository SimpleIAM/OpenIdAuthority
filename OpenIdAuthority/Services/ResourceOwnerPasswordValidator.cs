using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Services.Password;
using SimpleIAM.PasswordlessLogin.Stores;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services
{
    public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly ILogger _logger;
        public readonly IUserStore _userStore;
        public readonly IReadOnlyPasswordService _passwordService;
        
        public ResourceOwnerPasswordValidator(
            ILogger<ResourceOwnerPasswordValidator> logger,
            IUserStore userStore, 
            IReadOnlyPasswordService passwordService
            )
        {
            _logger = logger;
            _userStore = userStore;
            _passwordService = passwordService;
        }

        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            _logger.LogDebug("Validating resource owner password for: {0}", context.UserName);
            var validated = false;

            var userResponse = await _userStore.GetUserByUsernameAsync(context.UserName);
            if (userResponse.IsOk)
            {
                var user = userResponse.Result;
                var response = await _passwordService.CheckPasswordAsync(user.SubjectId, context.Password);
                if (response.IsOk)
                {
                    _logger.LogDebug("Resource owner password for {0} succeeded", context.UserName);
                    validated = true;
                    context.Result = new GrantValidationResult(user.SubjectId, OidcConstants.AuthenticationMethods.Password);
                }
            }
            if (!validated)
            {
                _logger.LogDebug("Resource owner password for {0} failed", context.UserName);
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "The email address or password wasn't right", null);
            }
        }
    }
}
