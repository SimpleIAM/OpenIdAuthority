// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.PasswordlessLogin.Services.Password;
using SimpleIAM.OpenIdAuthority.UI.Account;
using SimpleIAM.OpenIdAuthority.UI.Shared;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin;
using System.Net;

namespace SimpleIAM.OpenIdAuthority.UI.Authenticate
{
    [Route("account")]
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly IPasswordService _passwordService;
        private readonly IdProviderConfig _config;

        public AccountController(
            IPasswordService passwordService,
            IdProviderConfig config
            )
        {
            _passwordService = passwordService;
            _config = config;
        }

        [HttpGet("")]
        public IActionResult MyAccount()
        {
            // account details screen
            // require a recent authentication in order to edit info
            return View();
        }

        [HttpGet("setpassword")]
        public IActionResult SetPassword(string nextUrl)
        {
            var viewModel = GetSetPasswordViewModel(null, nextUrl);
            return View(viewModel);
        }

        [HttpPost("setpassword")]
        public async Task<IActionResult> SetPassword(SetPasswordModel model, string skip)
        {
            if(skip != null && model.NextUrl != null && (Url.IsLocalUrl(model.NextUrl) || true /*todo: validate that is is a url for a registered client? maybe use IRedirectUriValidator.IsRedirectUriValidAsync*/))
            {
                return Redirect(model.NextUrl);
            }
            var sub = User.GetSubjectId();

            var status = await _passwordService.SetPasswordAsync(sub, model.NewPassword);
            if(status.IsOk)
            {
                AddPostRedirectMessage("Password successfully set");

                if (model.NextUrl != null && (Url.IsLocalUrl(model.NextUrl) || true /*todo: validate that is is a url for a registered client?*/))
                {
                    return Redirect(model.NextUrl);
                }
                return RedirectToAction(nameof(MyAccount));

            }
            if (status.PasswordDoesNotMeetStrengthRequirements) {
                ModelState.AddModelError("NewPassword", "Password does not meet minimum password strength requirements (try something longer).");
            }
            else
            {
                ModelState.AddModelError("NewPassword", "Something went wrong.");
            }

            var viewModel = GetSetPasswordViewModel(model);
            return View(viewModel);
        }

        private SetPasswordModel GetSetPasswordViewModel(SetPasswordModel inputModel = null, string nextUrl = null)
        {
            var viewModel = new SetPasswordModel() {
                MinimumPasswordStrengthInBits = _config.MinimumPasswordStrengthInBits,
                OneTimeCode = inputModel?.OneTimeCode,
                NextUrl = inputModel?.NextUrl ?? nextUrl
            };
            return viewModel;
        }
    }
}