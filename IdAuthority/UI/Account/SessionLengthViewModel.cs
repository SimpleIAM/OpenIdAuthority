﻿// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.IdAuthority.UI.Account
{
    public class SessionLengthViewModel
    {
        public SessionLengthViewModel(int? minutes = null)
        {
            SessionLengthMinutes = minutes;
        }

        public int? SessionLengthMinutes { get; set; }
    }
}