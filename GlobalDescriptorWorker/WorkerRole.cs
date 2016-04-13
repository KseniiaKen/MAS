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
using Microsoft.ServiceBus.Messaging;
using CoreAMS.Messages;
using Microsoft.ServiceBus;
using Agent.Containers;
using CoreAMS.Global;
using CoreAMS.AgentCore;
using Agent.Agents;
using CoreAMS;

namespace GlobalDescriptorWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private string connectionString = @"Endpoint=sb://My_computer/ServiceBusDefaultNamespace;StsEndpoint=https://My_computer:9355/ServiceBusDefaultNamespace;RuntimePort=9354;ManagementPort=9355";
        private string queueName = "GlobalDescriptor";
        private const int registerTimeout = 60000;
        private Guid guid = Guid.NewGuid();
        private AutoResetEvent stopEvent = new AutoResetEvent(false);
        private Random random = new Random();
        private bool isStarted = false;
        System.Threading.Timer tickTimer;

        private QueueClient client;
        private Dictionary<Guid, QueueClient> workers = new Dictionary<Guid, QueueClient>(); // список id worker-ов + клиенты. чтобы знать куда отправлять сообщения
        private Dictionary<Guid, AutoResetEvent> waiters = new Dictionary<Guid, AutoResetEvent>(); // список id worker-ов + клиенты. чтобы знать куда отправлять сообщения
        private Dictionary<int, Guid> agents = new Dictionary<int, Guid>(); // в каком worker-е находится агент с id равным ключу

        private static void fillContainers()
        {
            Home home = new Home(0, 50, 12);
            Containers.Instance.Add(home); //Containers.Instance — глобальная коллекция, содержащая контейнеры.

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

        private void fillAgents()
        {
            List<IAgent> p = new List<IAgent>(); // создаем пустой список агентов
            p.AddRange(Adolescent.AdolescentList(Enums.HealthState.Infectious, 6, "LocationProbabilities"));
            p.AddRange(Adolescent.AdolescentList(Enums.HealthState.Susceptible, 750, "LocationProbabilities"));
            p.AddRange(Adult.AdultList(Enums.HealthState.Infectious, 6, "LocationProbabilities"));
            p.AddRange(Adult.AdultList(Enums.HealthState.Susceptible, 2450, "LocationProbabilities"));
            p.AddRange(Child.ChildList(Enums.HealthState.Infectious, 6, "LocationProbabilities"));
            p.AddRange(Child.ChildList(Enums.HealthState.Susceptible, 250, "LocationProbabilities"));
            p.AddRange(Elder.ElderList(Enums.HealthState.Infectious, 6, "LocationProbabilities"));
            p.AddRange(Elder.ElderList(Enums.HealthState.Susceptible, 900, "LocationProbabilities"));
            p.AddRange(Youngster.YoungsterList(Enums.HealthState.Infectious, 6, "LocationProbabilities"));
            p.AddRange(Youngster.YoungsterList(Enums.HealthState.Susceptible, 650, "LocationProbabilities"));

            for (int i = 0; i < p.Count; i++)
            {
                Person agent = (Person)p[i];
                var workserData = this.workers.ElementAt(i % this.workers.Count);
                this.agents.Add(agent.GetId(), workserData.Key);
                var msg = new BrokeredMessage(new AddAgentMessage(this.guid, agent.GetType().Name, agent.GetId(), agent.GetHealthState(), 1));
                msg.ContentType = typeof(AddAgentMessage).Name;
                workserData.Value.Send(msg);
            }

            //GlobalAgentDescriptorTable.AddAgents(p); // добавляем созданные агенты в класс, в котором хранятся все агенты


        }

        private void startTick()
        {
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                while(true)
                {
                    WaitHandle.WaitAll(this.waiters.Values.ToArray());

                    foreach(QueueClient c in this.workers.Values)
                    {
                        var msg = new BrokeredMessage(new Message(this.guid, MessageType.Tick));
                        msg.ContentType = typeof(Message).Name;
                        c.Send(msg);
                    }
                }
            });

            foreach(AutoResetEvent e in this.waiters.Values)
            {
                e.Set();
            }
        }

        public void Run2()
        {
            fillContainers();
            fillAgents();

            foreach (QueueClient c in this.workers.Values)
            {
                var msg = new BrokeredMessage(new Message(this.guid, MessageType.Start));
                msg.ContentType = typeof(Message).Name;
                c.Send(msg);
            }

            startTick();

            isStarted = true;
        }

        private void infectAgent(int sourceAgentId)
        {
            if (this.agents.Count < 2)
            {
                Trace.TraceInformation("Warning: too litle agents");
                return;
            }

            int idx = random.Next(0, this.agents.Count - 1);
            var kvp = this.agents.ElementAt(idx);
            QueueClient iClient = this.workers[kvp.Value];
            var msg0 = new Message(this.guid, MessageType.Infect);
            msg0.data = kvp.Key.ToString();
            Trace.TraceInformation("Infecting: {0} -> {1}", sourceAgentId, kvp.Key);
            var msg = new BrokeredMessage(msg0);
            msg.ContentType = typeof(Message).Name;
            iClient.Send(msg);
        }

        public override void Run()
        {
            OnMessageOptions options = new OnMessageOptions();
            options.AutoComplete = true; // Indicates if the message-pump should call complete on messages after the callback has completed processing.
            options.MaxConcurrentCalls = 1; // Indicates the maximum number of concurrent calls to the callback the pump should initiate 
            options.ExceptionReceived += (sender, e) =>
            {
                Trace.TraceError("Error: {0}", e.Exception);
            };

            Trace.WriteLine("Starting processing of messages");
            // Start receiveing messages
            this.client.OnMessage((receivedMessage) => // Initiates the message pump and callback is invoked for each message that is recieved, calling close on the client will stop the pump.
            {
                Message message = null;
                try
                {
                    message = receivedMessage.GetBody<Message>();
                }
                catch (Exception e)
                {
                    Trace.TraceError("Error: {0}", e);
                }

                if (message != null)
                {
                    Trace.TraceInformation("Received message of type: {0}", message.type);
                    switch(message.type)
                    {
                        case MessageType.Registration:
                            QueueClient senderClient = QueueClient.CreateFromConnectionString(this.connectionString, message.senderId.ToString(), ReceiveMode.ReceiveAndDelete);
                            this.workers.Add(message.senderId, senderClient);
                            this.waiters.Add(message.senderId, new AutoResetEvent(false));
                            break;
                        case MessageType.Infect:
                            if (isStarted)
                                this.infectAgent(Int32.Parse(message.data));
                            break;
                        case MessageType.TickEnd:
                            if (isStarted)
                                this.waiters[message.senderId].Set();
                            break;
                    }
                }

            });

            ThreadPool.QueueUserWorkItem((obj) =>
            {
                Thread.Sleep(registerTimeout);

                Trace.TraceInformation("Workers:");
                foreach (Guid g in this.workers.Keys)
                {
                    Trace.TraceInformation("\t{0}", g);
                }
                this.Run2();
            });

            this.stopEvent.WaitOne();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            var namespaceManager = NamespaceManager.CreateFromConnectionString(this.connectionString);
            if (!namespaceManager.QueueExists(this.queueName))
            {
                namespaceManager.CreateQueue(this.queueName);
            }
            client = QueueClient.CreateFromConnectionString(this.connectionString, this.queueName, ReceiveMode.ReceiveAndDelete);

            bool result = base.OnStart();

            Trace.TraceInformation("GlobalDescriptorWorker has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("GlobalDescriptorWorker is stopping");

            this.cancellationTokenSource.Cancel();
            this.stopEvent.Set();
            this.runCompleteEvent.WaitOne();

            while (client.Peek() != null)
            {
                Trace.TraceInformation("Cleaning message");
                var brokeredMessage = client.Receive();
                brokeredMessage.Complete();
            }
            this.client.Close();

            foreach(QueueClient c in this.workers.Values)
            {
                c.Close();
            }

            //var namespaceManager = NamespaceManager.CreateFromConnectionString(this.connectionString);
            //namespaceManager.DeleteQueue(this.queueName);

            base.OnStop();

            Trace.TraceInformation("GlobalDescriptorWorker has stopped");
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
