using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS.Messages
{
    [Serializable]
    public class AddContainerMessage : Message
    {
        public int containerId;
        public Enums.ContainerType containerType;
        public double area;
        public double dencity;
        public List<AddAgentMessage> agentData = new List<AddAgentMessage>();

        public AddContainerMessage(Guid senderId, Enums.ContainerType containerType, int id, double area, double dencity) : base(senderId, MessageType.AddContainer)
        {
            this.containerId = id;
            this.containerType = containerType;
            this.area = area;
            this.dencity = dencity;
        }
    }
}
