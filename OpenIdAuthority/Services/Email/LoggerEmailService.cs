﻿// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SimpleIAM.OpenIdAuthority.Services.Email
{
    public class LoggerEmailService : IEmailService
    {
        private readonly ILogger _logger;

        public LoggerEmailService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<SendMessageResult> SendEmailAsync(string from, string to, string subject, string body)
        {
            _logger.LogInformation("Sending email to log (sensitive info not redacted, use only in dev)");
            _logger.LogInformation("From: {0}\r\nTo: {1}\r\nSubject: {2}\r\nBody:\r\n{3}", from, to, subject, body);
            return SendMessageResult.Success();
        }
    }
}