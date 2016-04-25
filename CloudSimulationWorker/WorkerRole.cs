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

        private static void fillContainers()
        {
            Home home = new Home(0, 50, 12);
            Containers.Instance.Add(home); //Containers.Instance Ч глобальна€ коллекци€, содержаща€ контейнеры.

            Hospital hospital = new Hospital(1, 237, 19);
            Containers.Instance.Add(hospital);

            Mall mall = new Mall(2, 578, 90);
            Containers.Instance.Add(mall);

            Office office = new Office(3, 236, 20);
            Containers.Instance.Add(office);

            University university = new University(4, 300, 25);
            Containers.Instance.Add(university);

            School school = new School(5, 250, 30);
            Containers.Instance.Add(school);

            Nursery nursery = new Nursery(6, 60, 23);
            Containers.Instance.Add(nursery);

        }

        public override void Run()
        {
            if (cancellationTokenSource.IsCancellationRequested || isFinished)
                return;

            MessageTransportSystem.Instance.OnAddAgentMessage += onAddAgentMessage;
            MessageTransportSystem.Instance.OnStartMessage += onStartMessage;
            MessageTransportSystem.Instance.OnInfectMessage += onInfectMessage;
            MessageTransportSystem.Instance.OnTickMessage += onTickMessage;
            MessageTransportSystem.Instance.OnClearMessage += onClearMessage;

            MessageTransportSystem.Instance.StartListening();

            this.register();
            fillContainers();

            this.stopEvent.WaitOne();
        }

        private void onClearMessage(Message message)
        {
            GlobalAgentDescriptorTable.deleteAllAgents();
            GlobalTime.Time = 0; ;
        }

        private void onTickMessage(Message message)
        {
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
                AgentManagementSystem.RunAgents();
                Trace.TraceInformation("Results:\nSusceptible: {0}\nRecovered: {3}\nInfectious: {5}\nFuneral: {1}\nDead: {2}\nTime: {4}", AgentManagementSystem.susceptibleAgentsCount, AgentManagementSystem.funeralAgentsCount,
                        AgentManagementSystem.deadAgentsCount, AgentManagementSystem.recoveredAgentsCount, GlobalTime.Time, AgentManagementSystem.infectiousAgentsCount);

                ResultsMessage msg = new ResultsMessage(
                    MessageTransportSystem.Instance.Id,
                    AgentManagementSystem.susceptibleAgentsCount,
                    AgentManagementSystem.recoveredAgentsCount,
                    AgentManagementSystem.infectiousAgentsCount,
                    AgentManagementSystem.funeralAgentsCount,
                    AgentManagementSystem.deadAgentsCount,
                    GlobalTime.Time
                );
                MessageTransportSystem.Instance.SendMessage(msg);

                isFinished = true;
            });
        }

        private void onAddAgentMessage(Message message)
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
