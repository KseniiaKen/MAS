﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS.Messages
{
    [Serializable]
    public enum MessageType
    {
        Registration = 0,
        AddAgent     = 1,
        Start        = 2,
        Infect       = 3,
        Tick         = 4,
        TickEnd      = 5,
        Results      = 6,
        GoTo         = 7,
        Clear        = 8,
        AddContainer = 9
    }
}
