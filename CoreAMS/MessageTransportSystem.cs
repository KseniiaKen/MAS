using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS.AgentManagementSystem;
using CoreAMS.AgentCore;
using CoreAMS;

namespace CoreAMS.MessageTransportSystem
{
    // Класс сообщения.
    public class AgentMessage{
        public int receiverAgentId; // ID агента получателя
        public int senderAgentId;   // ID агента отправителя
        public string message;      // передаваемое сообщение

        public AgentMessage(string message = "", int receiverAgentId = -1, int senderAgentId = -1)
        {
            this.message = message;
            this.receiverAgentId = receiverAgentId;
            this.senderAgentId = senderAgentId;
        }
    }

    // обмен сообщениями между агентами
    public static class MessageTransfer
    {
        // Отправка сообщения случайному агенту
        public static void MessageAgentToRandomAgent(AgentMessage message)
        {
            GlobalAgentDescriptorTable.
                GetRandomAgentExceptSenderAgentId(message.senderAgentId).
                EventMessage(new AgentMessage(
                    Enums.MessageType.Infected.ToString(),
                    -1,
                    message.senderAgentId
                )
            );
        }
    }
}
