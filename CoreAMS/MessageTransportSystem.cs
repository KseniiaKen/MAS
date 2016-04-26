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
using CoreAMS.Global;

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
        private List<AbstractPerson> gotoAgents = new List<AbstractPerson>();
        private List<Enums.ContainerType> gotoContainers = new List<Enums.ContainerType>();
        private AddContainerMessage moveContainerMessage = null;

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

        public void AddToGoto(AbstractPerson agent, Enums.ContainerType containerType)
        {
            lock (gotoAgents)
            {
                gotoAgents.Add(agent);
                gotoContainers.Add(containerType);
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

                List<AddAgentMessage> ams = new List<AddAgentMessage>();
                foreach(AbstractPerson a in this.gotoAgents)
                {
                    ams.Add(new AddAgentMessage(
                        this.transportSystem.Id,
                        a.GetType().Name,
                        a.GetId(),
                        a.GetHealthState(),
                        1
                        ));
                }

                GoToContainerMessage msg = new GoToContainerMessage(
                    this.transportSystem.Id, 
                    ams.ToArray(), 
                    this.gotoContainers.ToArray(), 
                    this.infectedAgents.ToArray());

                if (this.moveContainerMessage != null)
                {
                    msg.containerToMove = this.moveContainerMessage;
                }

                this.transportSystem.SendMessage(msg);
                foreach (AbstractPerson a in this.gotoAgents)
                {
                    GlobalAgentDescriptorTable.DeleteOneAgent(a);
                }

                if (this.moveContainerMessage != null)
                {
                    foreach (var am in this.moveContainerMessage.agentData)
                    {
                        IAgent a = GlobalAgentDescriptorTable.GetAgentById(am.agentId);
                        if (a != null)
                        {
                            GlobalAgentDescriptorTable.DeleteOneAgent(a);
                        }
                    }
                    Containers.Instance.Remove(this.moveContainerMessage.containerId);
                }

                gotoAgents.Clear();
                gotoContainers.Clear();
                infectedAgents.Clear();
                this.moveContainerMessage = null;
            }
        }

        public void MoveContainer(ContainersCore container)
        {
            Trace.TraceInformation("Going to move container {0} ({1})", container.Id, container.ContainerType);

            var agents = GlobalAgentDescriptorTable.PersonsInContainer(container.Id);
            Trace.TraceInformation("Going to move container agents: {0}", String.Join(", ", agents.Select(a => a.GetId())));

            AddContainerMessage msg = new AddContainerMessage(
                this.transportSystem.Id,
                container.ContainerType,
                container.Id,
                container.Area,
                container.Dencity
            );
            msg.agentData.AddRange(agents.Select(a => new AddAgentMessage(
                this.transportSystem.Id,
                a.GetType().Name,
                a.GetId(),
                a.GetHealthState(),
                1
                )));

            this.moveContainerMessage = msg;
        }
        
    }
}
