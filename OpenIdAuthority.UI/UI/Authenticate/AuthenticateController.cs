// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.PasswordlessLogin.Orchestrators;
using SimpleIAM.PasswordlessLogin.Services.OTC;
using SimpleIAM.OpenIdAuthority.UI.Shared;

namespace SimpleIAM.OpenIdAuthority.UI.Authenticate
{
    [Route("")]
    public class AuthenticateController : BaseController
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IClientStore _clientStore;
        private readonly AuthenticateOrchestrator _authenticateOrchestrator;

        public AuthenticateController(
            AuthenticateOrchestrator authenticateOrchestrator,
            IIdentityServerInteractionService interaction,
            IEventService events,
            IOneTimeCodeService oneTimeCodeService,
            IClientStore clientStore)
        {
            _authenticateOrchestrator = authenticateOrchestrator;
            _interaction = interaction;
            _events = events;
            _oneTimeCodeService = oneTimeCodeService;
            _clientStore = clientStore;
        }

        [HttpGet("register")]
        public ActionResult Register(string returnUrl)
        {
            return View(new RegisterInputModel());
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterInputModel model, bool consent, string leaveBlank)
        {
            if (leaveBlank != null)
            {
                ViewBag.Message = "You appear to be a spambot";
            }
            else if (ModelState.IsValid)
            {
                if (!consent)
                {
                    ViewBag.Message = "Please acknowledge your consent";
                }
                else
                {
                    var status = await _authenticateOrchestrator.RegisterAsync(model);
                    ViewBag.Message = status.Text;
                }
            }
            return View(model);
        }

        [HttpGet("forgotpassword")]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost("forgotpassword")]
        public async Task<ActionResult> ForgotPassword(SendPasswordResetMessageInputModel model, string leaveBlank)
        {
            if (leaveBlank != null)
            {
                ViewBag.Message = "You appear to be a spambot";
            }
            else if (ModelState.IsValid)
            {
                var status = await _authenticateOrchestrator.SendPasswordResetMessageAsync(model);
                ViewBag.Message = status.Text;
            }
            return View(model);
        }

        [HttpGet("signin")]
        public async Task<ActionResult> SignIn(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            var viewModel = new AuthenticatePasswordInputModel()
            {
                Username = context?.LoginHint,
                NextUrl = returnUrl,
            };

            return View(viewModel);
        }

        [HttpPost("signin")]
        public async Task<ActionResult> SignIn(AuthenticatePasswordInputModel model, string action, string leaveBlank)
        {
            if (leaveBlank != null)
            {
                ViewBag.Message = "You appear to be a spambot";
            }
            else if (model.Username != null && (action == "getcode" || (action == "submit" && model.Password == null)))
            {
                ModelState.ClearValidationState(nameof(model.Password));
                var context = await _interaction.GetAuthorizationContextAsync(model.NextUrl);
                var input = new SendCodeInputModel()
                {
                    ApplicationId = context?.ClientId,
                    Username = model.Username,
                    NextUrl = model.NextUrl
                };
                var status = await _authenticateOrchestrator.SendOneTimeCodeAsync(input);
                ViewBag.Message = status.Text;
            }
            else if (ModelState.IsValid)
            {
                var status = await _authenticateOrchestrator.AuthenticateAsync(model);
                if (status.StatusCode == HttpStatusCode.Redirect)
                {
                    return Redirect(status.RedirectUrl);
                }
                ViewBag.Message = status.Text;
            }
            return View(model);
        }

        [HttpGet("signin/{longCode}")]
        public async Task<ActionResult> SignInLink(string longCode)
        {
            var status = await _authenticateOrchestrator.AuthenticateLongCodeAsync(longCode);
            switch (status.StatusCode)
            {
                case HttpStatusCode.Redirect:
                    return Redirect(status.RedirectUrl);
                case HttpStatusCode.NotFound:
                    return NotFound();
                default:
                    AddPostRedirectMessage(status.Text);
                    return RedirectToAction(nameof(SignIn));
            }
        }

        [HttpGet("signout")]
        public async Task<ActionResult> SignOut(string id)
        {
            var context = await _interaction.GetLogoutContextAsync(id);

            if (User?.Identity.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync();
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));

                // We're signed out now, so the UI for this request should show an anonymous user
                HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            }
            var viewModel = new SignedOutViewModel()
            {
                AppName = (await _clientStore.FindEnabledClientByIdAsync(context?.ClientId))?.ClientName ?? "the website",
                PostLogoutRedirectUri = context?.PostLogoutRedirectUri,
                SignOutIFrameUrl = context?.SignOutIFrameUrl
            };

            return View("SignedOut", viewModel);
        }
    }
}