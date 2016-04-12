﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS.Messages
{
    [Serializable]
    public class AddAgentMessage : Message
    {
        public Enums.HealthState state;
        public string agentType;
        public int count;

        public AddAgentMessage(Guid senderId, string aType, Enums.HealthState healthState, int count) : base(senderId, MessageType.AddAgent)
        {
            this.agentType = aType;
            this.state = healthState;
            this.count = count;
        }

    }
}