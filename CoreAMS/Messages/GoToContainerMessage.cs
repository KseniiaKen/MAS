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
        public AddAgentMessage[] agents;              // | <- this agents are going 
        public Enums.ContainerType[] containerTypes;  // | <-   to this containers 

        public int[] infectionSourceAgentIds;         // Agents trying to infect someone

        public AddContainerMessage containerToMove = null;

        public int agentCount = 0;

        public GoToContainerMessage(Guid senderId, AddAgentMessage[] agents, Enums.ContainerType[] containerIds, int[] infectionSourceAgentIds, int agentCount): base(senderId, MessageType.GoTo)
        {
            this.agents = agents;
            this.containerTypes = containerIds;
            this.infectionSourceAgentIds = infectionSourceAgentIds;
            this.agentCount = agentCount;
        }
    }
}
