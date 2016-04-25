using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS.AgentManagementSystem;
using CoreAMS.AgentCore;
using CoreAMS;
using Microsoft.ServiceBus.Messaging;
using CoreAMS.Messages;
using System.Diagnostics;

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

    public interface IMessageTransportSystem
    {
        Guid Id { get; }
        void SendMessage(Message message);
        void SendMessage(Message message, Guid clientId);
        void StartListening();
    }

    // обмен сообщениями между агентами
    public class MessageTransfer
    {
        private static MessageTransfer instance = new MessageTransfer();

        private IMessageTransportSystem transportSystem;
        private List<int> infectedAgents = new List<int>();
        private List<int> gotoAgents = new List<int>();
        private List<int> gotoContainers = new List<int>();

        public static MessageTransfer Instance
        {
            get
            {
                return instance; 
            }
        }

        public void Init(IMessageTransportSystem messageTransportSystem)
        {
            this.transportSystem = messageTransportSystem;
        }

        // Отправка сообщения случайному агенту
        public void AddInfect(AgentMessage message)
        {
            //var msg0 = new Message(guid, MessageType.Infect);
            //msg0.data = message.senderAgentId.ToString();
            //var msg = new BrokeredMessage(msg0);
            //msg.ContentType = typeof(Message).Name;
            //client.Send(msg);

            lock (gotoAgents)
            {
                infectedAgents.Add(message.senderAgentId);
            }
        }

        //Нам нужен метод, отправляющий сообщения о заражении не случайным агентам, а находящимся в одной локации.

        public void SendTickEnd()
        {            
            this.transportSystem.SendMessage(new Message(this.transportSystem.Id, MessageType.TickEnd));
        }

        public void AddToGoto(int agentId, int containerId)
        {
            lock (gotoAgents)
            {
                gotoAgents.Add(agentId);
                gotoContainers.Add(containerId);
            }
        }

        public void SendGoto()
        {
            lock (gotoAgents)
            {
                if (gotoAgents.Count != gotoContainers.Count)
                    Trace.TraceWarning("!!! Agent and container count doesn't match");
                //return;

                //if (gotoAgents.Count == 0)
                //    return;

                this.transportSystem.SendMessage(new GoToContainerMessage(this.transportSystem.Id, MessageType.TickEnd, gotoAgents.ToArray(), gotoContainers.ToArray(), infectedAgents.ToArray()));
                gotoAgents.Clear();
                gotoContainers.Clear();
                infectedAgents.Clear();
            }
        }
        
    }
}
