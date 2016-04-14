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
        private Guid guid = Guid.NewGuid();
        private string connectionString = @"Endpoint=sb://My_computer/ServiceBusDefaultNamespace;StsEndpoint=https://My_computer:9355/ServiceBusDefaultNamespace;RuntimePort=9354;ManagementPort=9355";
        private string globalDescriptorQueueName = "GlobalDescriptor";
        private QueueClient client;
        private QueueClient clientForGlobalDescriptor;
        private AutoResetEvent stopEvent = new AutoResetEvent(false);

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

        private void register()
        {
            //var namespaceManager = NamespaceManager.CreateFromConnectionString(this.connectionString);
            //if (!namespaceManager.QueueExists(this.globalDescriptorQueueName))
            //{
            //    namespaceManager.CreateQueue(this.globalDescriptorQueueName);
            //}
            //Trace.TraceInformation("Sending registration...");
            var msg = new BrokeredMessage(new Message(this.guid, MessageType.Registration));
            msg.ContentType = typeof(Message).Name;
            clientForGlobalDescriptor.Send(msg);
        }

        public override void Run()
        {
            if (cancellationTokenSource.IsCancellationRequested || isFinished)
                return;

            OnMessageOptions options = new OnMessageOptions();
            options.AutoComplete = true; // Indicates if the message-pump should call complete on messages after the callback has completed processing.
            options.MaxConcurrentCalls = 1; // Indicates the maximum number of concurrent calls to the callback the pump should initiate 
            options.ExceptionReceived += (sender, e) =>
            {
                Trace.TraceError("Error: {0}", e.Exception);
            };

            //Trace.WriteLine("Starting processing of messages");
            // Start receiveing messages
            this.client.OnMessage((receivedMessage) => // Initiates the message pump and callback is invoked for each message that is recieved, calling close on the client will stop the pump.
            {
                Message message = null;
                try
                {
                    var ct = receivedMessage.ContentType;
                    //Trace.TraceInformation("ContentType: {0}", ct);
                    switch (ct)
                    {
                        case "AddAgentMessage":
                            message = receivedMessage.GetBody<AddAgentMessage>();
                            break;
                        default:
                            message = receivedMessage.GetBody<Message>();
                            //Trace.TraceInformation("Subtype: {0}", message.type);
                            break;
                    }                    
                }
                catch (Exception e)
                {
                    Trace.TraceError("Error: {0}", e);
                }

                if (message != null)
                {
                    switch (message.type)
                    {
                        case MessageType.AddAgent:
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
                            break;
                        case MessageType.Start:
                            ThreadPool.QueueUserWorkItem((obj) =>
                            {
                                AgentManagementSystem.RunAgents();
                                Trace.TraceInformation("Results:\nSusceptible: {0}\nRecovered: {3}\nInfectious: {5}\nFuneral: {1}\nDead: {2}\nTime: {4}", AgentManagementSystem.susceptibleAgentsCount, AgentManagementSystem.funeralAgentsCount,
                                        AgentManagementSystem.deadAgentsCount, AgentManagementSystem.recoveredAgentsCount, GlobalTime.Time, AgentManagementSystem.infectiousAgentsCount);

                                var msg = new BrokeredMessage(new ResultsMessage(
                                    this.guid,
                                    AgentManagementSystem.susceptibleAgentsCount,
                                    AgentManagementSystem.recoveredAgentsCount,
                                    AgentManagementSystem.infectiousAgentsCount,
                                    AgentManagementSystem.funeralAgentsCount,
                                    AgentManagementSystem.deadAgentsCount,
                                    GlobalTime.Time
                                ));
                                msg.ContentType = typeof(ResultsMessage).Name;
                                this.clientForGlobalDescriptor.Send(msg);
                                
                                isFinished = true;
                            });
                            break;
                        case MessageType.Infect:
                            int agentId = Int32.Parse(message.data);
                            var agent = GlobalAgentDescriptorTable.GetAgentById(agentId);
                            agent.EventMessage(new CoreAMS.MessageTransportSystem.AgentMessage(Enums.MessageType.Infected.ToString(), -1, -1));
                            break;
                        case MessageType.Tick:
                            AgentManagementSystem.NextTimeEvent.Set();
                            break;
                        case MessageType.Clear:
                            GlobalAgentDescriptorTable.deleteAllAgents();
                            GlobalTime.Time = 0;
                            break;

                    }
                }

            });

            this.register();
            fillContainers();

            this.stopEvent.WaitOne();
        }

        public override bool OnStart()
        {
            if (isFinished)
                return false;

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            Trace.TraceInformation("My Guid: {0}", this.guid);
            CoreAMS.MessageTransportSystem.MessageTransfer.guid = this.guid;

            var namespaceManager = NamespaceManager.CreateFromConnectionString(this.connectionString);
            if (!namespaceManager.QueueExists(this.guid.ToString()))
            {
                namespaceManager.CreateQueue(this.guid.ToString());
            }
            this.client = QueueClient.CreateFromConnectionString(this.connectionString, this.guid.ToString(), ReceiveMode.ReceiveAndDelete);

            this.clientForGlobalDescriptor = QueueClient.CreateFromConnectionString(this.connectionString, this.globalDescriptorQueueName);

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

            while (client.Peek() != null)
            {
                var brokeredMessage = client.Receive();
                brokeredMessage.Complete();
            }

            this.client.Close();
            //var namespaceManager = NamespaceManager.CreateFromConnectionString(this.connectionString);
            //namespaceManager.DeleteQueue(this.guid.ToString());

            this.clientForGlobalDescriptor.Close();

            CoreAMS.MessageTransportSystem.MessageTransfer.Dispose();

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
