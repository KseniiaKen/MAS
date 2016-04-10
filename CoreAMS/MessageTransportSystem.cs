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
            IAgent agent = GlobalAgentDescriptorTable.SameLocationAgent(message.senderAgentId);
            //IAgent agent = GlobalAgentDescriptorTable.GetRandomAgentExceptSenderAgentId(message.senderAgentId);
            if (agent != null)
            {
                agent.EventMessage(new AgentMessage(Enums.MessageType.Infected.ToString(), -1, message.senderAgentId));
            }

        }

        //Нам нужен метод, отправляющий сообщения о заражении не случайным агентам, а находящимся в одной локации.

        public static void MessageAgentInThisLocation(AgentMessage message) { 
                    
        }
        
    }
}
