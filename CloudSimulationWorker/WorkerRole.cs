using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Agent.Containers;
using CoreAMS.Global;
using CoreAMS.AgentCore;
using Agent.Agents;
using CoreAMS;
using CoreAMS.AgentManagementSystem;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;
using CoreAMS.Messages;

namespace CloudSimulationWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private bool isFinished = false;
        private AutoResetEvent stopEvent = new AutoResetEvent(false);

        private void register()
        {
            MessageTransportSystem.Instance.SendMessage(new Message(MessageTransportSystem.Instance.Id, MessageType.Registration));
        }

        public override void Run()
        {
            if (cancellationTokenSource.IsCancellationRequested || isFinished)
                return;

            MessageTransportSystem.Instance.OnAddAgentMessage += (msg) => this.onAddAgentMessage(msg);
            MessageTransportSystem.Instance.OnStartMessage += onStartMessage;
            MessageTransportSystem.Instance.OnInfectMessage += onInfectMessage;
            MessageTransportSystem.Instance.OnTickMessage += onTickMessage;
            MessageTransportSystem.Instance.OnClearMessage += onClearMessage;
            MessageTransportSystem.Instance.OnAddContainerMessage += onAddContainerMessage;

            MessageTransportSystem.Instance.StartListening();

            this.register();

            this.stopEvent.WaitOne();
        }

        private void onAddContainerMessage(Message message)
        {
            AddContainerMessage acm = (AddContainerMessage)message;

            Trace.TraceInformation("Added containter. Type: {0}; Id: {1}.", acm.containerType, acm.containerId);

            ContainersCore container = null;
            switch(acm.containerType)
            {
                case Enums.ContainerType.Home:
                    container = new Home(acm.containerId, acm.area, acm.dencity);
                    break;
                case Enums.ContainerType.Hospital:
                    container = new Hospital(acm.containerId, acm.area, acm.dencity);
                    break;
                case Enums.ContainerType.Mall:
                    container = new Mall(acm.containerId, acm.area, acm.dencity);
                    break;
                case Enums.ContainerType.Nursery:
                    container = new Nursery(acm.containerId, acm.area, acm.dencity);
                    break;
                case Enums.ContainerType.Office:
                    container = new Office(acm.containerId, acm.area, acm.dencity);
                    break;
                case Enums.ContainerType.School:
                    container = new School(acm.containerId, acm.area, acm.dencity);
                    break;
                case Enums.ContainerType.University:
                    container = new University(acm.containerId, acm.area, acm.dencity);
                    break;
            }

            if (container != null)
            {
                if (!Containers.Instance.ContainsKey(container.Id))
                {
                    Containers.Instance.Add(container.Id, container);
                }

                foreach (AddAgentMessage aam in acm.agentData)
                {
                    this.onAddAgentMessage(aam, container);
                }
            }
        }

        private void onClearMessage(Message message)
        {
            AgentManagementSystem.finishFlag = true;
            AgentManagementSystem.NextTimeEvent.Set();
            GlobalAgentDescriptorTable.DeleteAllAgents();
            GlobalTime.Time = 0; ;
        }

        private void onTickMessage(Message message)
        {
            TickMessage tmessage = (TickMessage)message;

            foreach (AddAgentMessage aam in tmessage.agents)
            {
                this.onAddAgentMessage(aam);
            }

            AgentManagementSystem.NextTimeEvent.Set();
        }

        private void onInfectMessage(Message message)
        {
            int agentId = Int32.Parse(message.data);
            var agent = GlobalAgentDescriptorTable.GetAgentById(agentId);
            agent.EventMessage(new CoreAMS.MessageTransportSystem.AgentMessage(Enums.MessageType.Infected.ToString(), -1, -1));
        }

        private void onStartMessage(Message message)
        {
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                StartMessage smessage = (StartMessage)message;
                AgentManagementSystem.finishFlag = false;
                Trace.TraceInformation("Started. System Info: Agents: {0}; Containers: {1}; Workers: {2}", smessage.totalAgents, smessage.totalContainers, smessage.totalWorkers);
                AgentManagementSystem.totalAgents = smessage.totalAgents;
                AgentManagementSystem.totalContainers = smessage.totalContainers;
                AgentManagementSystem.totalWorkers = smessage.totalWorkers;
                AgentManagementSystem.RunAgents();
                if (!AgentManagementSystem.finishFlag) // Process was stopped manually. No need for result sending
                {                    
                    Trace.TraceInformation("Results:\nSusceptible: {0}\nRecovered: {3}\nInfectious: {5}\nFuneral: {1}\nDead: {2}\nTime: {4}", AgentManagementSystem.susceptibleAgentsCount, AgentManagementSystem.funeralAgentsCount,
                            AgentManagementSystem.deadAgentsCount, AgentManagementSystem.recoveredAgentsCount, GlobalTime.Time, AgentManagementSystem.infectiousAgentsCount);

                    ResultsMessage msg = new ResultsMessage(
                        MessageTransportSystem.Instance.Id,
                        AgentManagementSystem.susceptibleAgentsCount,
                        AgentManagementSystem.recoveredAgentsCount,
                        AgentManagementSystem.infectiousAgentsCount,
                        AgentManagementSystem.funeralAgentsCount,
                        AgentManagementSystem.deadAgentsCount,
                        AgentManagementSystem.exposedAgentsCount,
                        GlobalTime.Time
                    );
                    MessageTransportSystem.Instance.SendMessage(msg);
                }
                else
                {
                    Trace.TraceInformation("Process was stopped manually. No need for result sending");
                }

                AgentManagementSystem.finishFlag = false;
                isFinished = true;
            });
        }

        private void onAddAgentMessage(Message message, ContainersCore container = null)
        {
            AddAgentMessage aaMessage = (AddAgentMessage)message;
            List<IAgent> ags = new List<IAgent>();
            GlobalAgentDescriptorTable.GetNewId = aaMessage.agentId;
            switch (aaMessage.agentType)
            {
                case "Adolescent":
                    ags.AddRange(Adolescent.AdolescentList(aaMessage.state, aaMessage.count, "LocationProbabilities"));
                    break;
                case "Adult":
                    ags.AddRange(Adult.AdultList(aaMessage.state, aaMessage.count, "LocationProbabilities"));
                    break;
                case "Child":
                    ags.AddRange(Child.ChildList(aaMessage.state, aaMessage.count, "LocationProbabilities"));
                    break;
                case "Elder":
                    ags.AddRange(Elder.ElderList(aaMessage.state, aaMessage.count, "LocationProbabilities"));
                    break;
                case "Youngster":
                    ags.AddRange(Youngster.YoungsterList(aaMessage.state, aaMessage.count, "LocationProbabilities"));
                    break;
            }
            foreach(IAgent a in ags)
            {
                if (container == null)
                {
                    int containerId = aaMessage.containerId;
                    if (containerId > 0)
                    {
                        if (Containers.Instance.ContainsKey(containerId))
                        {
                            var foundContainer = Containers.Instance[containerId];

                            AbstractPerson ap = (AbstractPerson)a;
                            ap.currentContainerId = containerId;
                            ap.currentContainerType = foundContainer.ContainerType;
                        }
                        else
                        {
                            Trace.TraceWarning("Agent was added to moved to another node container. Moving him again");
                            CoreAMS.MessageTransportSystem.MessageTransfer.Instance.AddToGoto((AbstractPerson)a, Enums.ContainerType.Home);
                        }
                    }
                }
                else
                {
                    AbstractPerson ap = (AbstractPerson)a;
                    ap.currentContainerId = container.Id;
                    ap.currentContainerType = container.ContainerType;
                }
            }
            GlobalAgentDescriptorTable.AddAgents(ags);
            //foreach (var ag in ags) {
            //    Trace.TraceInformation("Added agent with id {0} (check: {1})", ag.GetId(), aaMessage.agentId);
            //}
        }

        public override bool OnStart()
        {
            if (isFinished)
                return false;

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            MessageTransportSystem.Instance.Init();
            CoreAMS.MessageTransportSystem.MessageTransfer.Instance.Init(MessageTransportSystem.Instance);
            Trace.TraceInformation("My Guid: {0}", MessageTransportSystem.Instance.Id);            

            bool result = base.OnStart();

            //Trace.TraceInformation("CloudSimulationWorker has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("CloudSimulationWorker is stopping");

            this.cancellationTokenSource.Cancel();
            this.stopEvent.Set();
            this.runCompleteEvent.WaitOne();

            MessageTransportSystem.Instance.DeInit();

            base.OnStop();

            //Trace.TraceInformation("CloudSimulationWorker has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
