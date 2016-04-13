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
        public int agentId;
        public int containerId;

        public GoToContainerMessage(Guid senderId, MessageType type, int agentId, int containerId): base(senderId, MessageType.GoTo)
        {
            this.agentId = agentId;
            this.containerId = containerId;
        }
    }
}
