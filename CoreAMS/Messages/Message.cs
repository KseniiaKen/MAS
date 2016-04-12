using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS.Messages
{
    [Serializable]
    public class Message
    {
        public Guid senderId;
        public MessageType type;
        public string data = "";

        public Message(Guid senderId, MessageType type)
        {
            this.senderId = senderId;
            this.type = type;
        }
    }
}
