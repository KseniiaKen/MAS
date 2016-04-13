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
        private const string connectionString = @"Endpoint=sb://My_computer/ServiceBusDefaultNamespace;StsEndpoint=https://My_computer:9355/ServiceBusDefaultNamespace;RuntimePort=9354;ManagementPort=9355";
        private const string globalDescriptorQueueName = "GlobalDescriptor";
        public static Guid guid;

        // Отправка сообщения случайному агенту
        public static void MessageAgentToRandomAgent(AgentMessage message)
        {
            //IAgent agent = GlobalAgentDescriptorTable.SameLocationAgent(message.senderAgentId);
            ////IAgent agent = GlobalAgentDescriptorTable.GetRandomAgentExceptSenderAgentId(message.senderAgentId);
            //if (agent != null)
            //{
            //    agent.EventMessage(new AgentMessage(Enums.MessageType.Infected.ToString(), -1, message.senderAgentId));
            //}

            QueueClient client = QueueClient.CreateFromConnectionString(connectionString, globalDescriptorQueueName, ReceiveMode.ReceiveAndDelete);
            var msg0 = new Message(guid, MessageType.Infect);
            msg0.data = message.senderAgentId.ToString();
            var msg = new BrokeredMessage(msg0);
            msg.ContentType = typeof(Message).Name;
            client.Send(msg);
            client.Close();
        }

        //Нам нужен метод, отправляющий сообщения о заражении не случайным агентам, а находящимся в одной локации.

        public static void MessageAgentInThisLocation(AgentMessage message) { 
                    
        }
        
    }
}
