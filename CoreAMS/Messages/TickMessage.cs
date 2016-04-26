using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS.Messages
{
    [Serializable]
    public class TickMessage: Message
    {
        public AddAgentMessage[] agents;

        public TickMessage(Guid senderId, AddAgentMessage[] agents) : base(senderId, MessageType.Tick)
        {
            this.agents = agents;
        }
    }
}
