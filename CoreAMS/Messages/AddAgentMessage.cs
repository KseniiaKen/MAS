using System;
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
        public int agentId;
        public int containerId;

        public AddAgentMessage(Guid senderId, string aType, int agentId, Enums.HealthState healthState, int count, int containerId = -1) : base(senderId, MessageType.AddAgent)
        {
            this.agentType = aType;
            this.state = healthState;
            this.count = count;
            this.agentId = agentId;
            this.containerId = containerId;
        }

    }
}
