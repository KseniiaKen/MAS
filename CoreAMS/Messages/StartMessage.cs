using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS.Messages
{
    [Serializable]
    public class StartMessage : Message
    {
        public int totalAgents;
        public int totalContainers;
        public int totalWorkers;

        public StartMessage(Guid senderId, int totalAgents, int totalContainers, int totalWorkers) : base(senderId, MessageType.Start)
        {
            this.totalAgents = totalAgents;
            this.totalContainers = totalContainers;
            this.totalWorkers = totalWorkers;
        }
    }
}
