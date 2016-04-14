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

        public GoToContainerMessage(Guid senderId, MessageType type, int[] agentIds, int[] containerIds): base(senderId, MessageType.GoTo)
        {
            this.agentIds = agentIds;
            this.containerIds = containerIds;
        }
    }
}
