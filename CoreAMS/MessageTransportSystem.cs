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

    // обмен сообщениями между агентами
    public static class MessageTransfer
    {
        private const string connectionString = @"Endpoint=sb://My_computer/ServiceBusDefaultNamespace;StsEndpoint=https://My_computer:9355/ServiceBusDefaultNamespace;RuntimePort=9354;ManagementPort=9355";
        private const string globalDescriptorQueueName = "GlobalDescriptor";
        public static Guid guid;

        private static QueueClient client = QueueClient.CreateFromConnectionString(connectionString, globalDescriptorQueueName, ReceiveMode.ReceiveAndDelete);

        private static List<int> infectedAgents = new List<int>();
        // Отправка сообщения случайному агенту
        public static void AddInfect(AgentMessage message)
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

        public static void SendTickEnd()
        {            
            var msg = new BrokeredMessage(new Message(guid, MessageType.TickEnd));
            msg.ContentType = typeof(Message).Name;
            client.Send(msg);
        }

        private static List<int> gotoAgents = new List<int>();
        private static List<int> gotoContainers = new List<int>();

        public static void AddToGoto(int agentId, int containerId)
        {
            lock (gotoAgents)
            {
                gotoAgents.Add(agentId);
                gotoContainers.Add(containerId);
            }
        }

        public static void SendGoto()
        {
            lock (gotoAgents)
            {
                if (gotoAgents.Count != gotoContainers.Count)
                    Trace.TraceWarning("!!! Agent and container count doesn't match");
                //return;

                //if (gotoAgents.Count == 0)
                //    return;

                var msg = new BrokeredMessage(new GoToContainerMessage(guid, MessageType.TickEnd, gotoAgents.ToArray(), gotoContainers.ToArray(), infectedAgents.ToArray()));

                msg.ContentType = typeof(GoToContainerMessage).Name;
                client.Send(msg);
                gotoAgents.Clear();
                gotoContainers.Clear();
                infectedAgents.Clear();
            }
        }

        public static void Dispose()
        {
            client.Close();
        }
        
    }
}
