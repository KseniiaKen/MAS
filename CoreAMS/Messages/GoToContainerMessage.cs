using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS.Messages
{
    [Serializable]
    public class GoToContainerMessage : Message
    {
        public int[] agentIds;
        public int[] containerIds;
        public int[] infectionSourceAgentIds;

        public GoToContainerMessage(Guid senderId, MessageType type, int[] agentIds, int[] containerIds, int[] infectionSourceAgentIds): base(senderId, MessageType.GoTo)
        {
            this.agentIds = agentIds;
            this.containerIds = containerIds;
            this.infectionSourceAgentIds = infectionSourceAgentIds;
        }
    }
}
