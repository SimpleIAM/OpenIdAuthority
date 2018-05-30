﻿// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleIAM.OpenIdAuthority.API;
using SimpleIAM.OpenIdAuthority.Configuration;
using SimpleIAM.OpenIdAuthority.Entities;
using SimpleIAM.OpenIdAuthority.Orchestrators;
using SimpleIAM.OpenIdAuthority.Services.Message;
using SimpleIAM.OpenIdAuthority.Services.OTC;
using SimpleIAM.OpenIdAuthority.Services.Password;
using SimpleIAM.OpenIdAuthority.Stores;
using SimpleIAM.OpenIdAuthority.UI.Shared;

namespace SimpleIAM.OpenIdAuthority.UI.Authenticate
{
    [Route("")]
    [Authorize]
    public class AuthenticateController : BaseController
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly ISubjectStore _subjectStore;
        private readonly IClientStore _clientStore;
        private readonly IPasswordService _passwordService;
        private readonly IdProviderConfig _config;
        private readonly AuthenticateOrchestrator _authenticateOrchestrator;

        public AuthenticateController(
            AuthenticateOrchestrator authenticateOrchestrator,
            IIdentityServerInteractionService interaction,
            IEventService events,
            IOneTimeCodeService oneTimeCodeService,
            IMessageService messageService,
            ISubjectStore subjectStore,
            IdProviderConfig config,
            IClientStore clientStore,
            IPasswordService passwordService)
        {
            _authenticateOrchestrator = authenticateOrchestrator;
            _interaction = interaction;
            _events = events;
            _oneTimeCodeService = oneTimeCodeService;
            _messageService = messageService;
            _subjectStore = subjectStore;
            _clientStore = clientStore;
            _passwordService = passwordService;
            _config = config;
        }

        [HttpGet("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register(string returnUrl)
        {
            return View(new RegisterInputModel());
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterInputModel model, bool consent, string leaveBlank)
        {
            if(leaveBlank != null) 
            {
                ViewBag.Message = "You appear to be a spambot";
            }
            else if (ModelState.IsValid)
            {
                if(!consent) 
                {
                    ViewBag.Message = "Please acknowledge your consent";
                }
                else 
                {
                    var result = await _authenticateOrchestrator.Register(model);
                    ViewBag.Message = result.Message;
                }
            }
            return View(model);
        }

        [HttpGet("forgotpassword")]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotPassword()
        {
            return View();
        }

        [HttpPost("forgotpassword")]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotPassword(SendPasswordResetMessageInputModel model, string leaveBlank)
        {
            if(leaveBlank != null) 
            {
                ViewBag.Message = "You appear to be a spambot";
            }
            else if (ModelState.IsValid)
            {
                var result = await _authenticateOrchestrator.SendPasswordResetMessage(model);
                ViewBag.Message = result.Message;
            }
            return View(model);
        }

        [HttpGet("signin")]
        [AllowAnonymous]
        public async Task<ActionResult> SignIn(string returnUrl)
        {
            var viewModel = await GetSignInPassViewModelAsync(returnUrl);
            return View(viewModel);
        }
        
        [HttpGet("signin/{longCode}")]
        [AllowAnonymous]
        public async Task<ActionResult> SignInLink(string longCode)
        {
            if(longCode != null && longCode.Length < 36)
            {
                var response = await _oneTimeCodeService.CheckOneTimeCodeAsync(longCode);
                switch(response.Result)
                {
                    case CheckOneTimeCodeResult.Verified:
                        var subject = await _subjectStore.GetSubjectByEmailAsync(response.SentTo); //todo: does this need to handle a phone number?
                        return await FinishSignIn(subject, null, response.RedirectUrl);
                    case CheckOneTimeCodeResult.Expired:
                        AddPostRedirectMessage("The sign in link expired.");
                        return RedirectToAction("SignIn");
                    case CheckOneTimeCodeResult.CodeIncorrect:
                    case CheckOneTimeCodeResult.NotFound:
                        AddPostRedirectMessage("The sign in link is invalid.");
                        return RedirectToAction("SignIn");
                    case CheckOneTimeCodeResult.ServiceFailure:
                    default:
                        AddPostRedirectMessage("Something went wrong.");
                        return RedirectToAction("SignIn");
                }
            }

            return NotFound();
        }

        [HttpGet("signout")]
        [AllowAnonymous]
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

            return View(viewModel);
        }

        private async Task<ActionResult> FinishSignIn(Subject subject, int? sessionLengthMinutes, string returnUrl)
        {
            await _events.RaiseAsync(new UserLoginSuccessEvent(subject.Email, subject.SubjectId, subject.Email));

            // handle custom session length
            var authProps = (AuthenticationProperties)null;
            var sessionLengthMinutesInt = sessionLengthMinutes ?? 0;
            if (sessionLengthMinutes > 0 && sessionLengthMinutes < _config.MaxSessionLengthMinutes)
            {
                authProps = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(sessionLengthMinutesInt))
                };
            };

            await HttpContext.SignInAsync(subject.SubjectId, subject.Email, authProps);

            if (_interaction.IsValidReturnUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Apps", "Home");
        }

        private async Task<SignInViewModel> GetSignInViewModelAsync(string returnUrl, SignInInputModel model = null)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            var viewModel = new SignInViewModel()
            {
                Email = model?.Email ?? context?.LoginHint,
                LeaveBlank = model?.LeaveBlank,
                ReturnUrl = returnUrl,
            };

            return viewModel;
        }

        private async Task<SignInPassViewModel> GetSignInPassViewModelAsync(string returnUrl = null, SignInPassInputModel model = null)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            var viewModel = new SignInPassViewModel()
            {
                Email = model?.Email ?? context?.LoginHint,
                LeaveBlank = model?.LeaveBlank,
                SessionLengthMinutes = model?.SessionLengthMinutes ?? _config.DefaultSessionLengthMinutes,
                ReturnUrl = returnUrl,
            };

            return viewModel;
        }
    }
}