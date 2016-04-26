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

        public GoToContainerMessage(Guid senderId, MessageType type, AddAgentMessage[] agents, Enums.ContainerType[] containerIds, int[] infectionSourceAgentIds): base(senderId, MessageType.GoTo)
        {
            this.agents = agents;
            this.containerTypes = containerIds;
            this.infectionSourceAgentIds = infectionSourceAgentIds;
        }
    }
}
