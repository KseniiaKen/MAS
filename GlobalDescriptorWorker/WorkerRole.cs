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
        private const int waitTimeout = 30000;
        private const int numberOfIterations = 80;
        private Guid guid = Guid.NewGuid();
        private AutoResetEvent stopEvent = new AutoResetEvent(false);
        private Random random = new Random();
        private bool isStarted = false;
        private Thread tickThread;
        private int iterationNum = 0;

        private QueueClient client;
        private Dictionary<Guid, QueueClient> workers = new Dictionary<Guid, QueueClient>(); // список id worker-ов + клиенты. чтобы знать куда отправлять сообщения
        private Dictionary<Guid, AutoResetEvent> waiters = new Dictionary<Guid, AutoResetEvent>();
        private Dictionary<Guid, ResultsMessage> results = new Dictionary<Guid, ResultsMessage>(); // список id worker-ов + их результаты
        private Dictionary<int, Guid> agents = new Dictionary<int, Guid>(); // в каком worker-е находится агент с id равным ключу
        private Dictionary<int, ContainersCore> agentLocations = new Dictionary<int, ContainersCore>();
        private List<int[]> totalResult = new List<int[]>();

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
                this.agentLocations.Add(agent.GetId(), null);
                var msg = new BrokeredMessage(new AddAgentMessage(this.guid, agent.GetType().Name, agent.GetId(), agent.GetHealthState(), 1));
                msg.ContentType = typeof(AddAgentMessage).Name;
                workserData.Value.Send(msg);
            }

            //GlobalAgentDescriptorTable.AddAgents(p); // добавляем созданные агенты в класс, в котором хранятся все агенты


        }

        private void calculateResult()
        {
            int suspectableCount = 0;
            int recoveredCount = 0;
            int infectiousCount = 0;
            int funeralCount = 0;
            int deadCount = 0;
            int time = 0;

            foreach(ResultsMessage res in this.results.Values)
            {
                suspectableCount += res.suspectableCount;
                recoveredCount += res.recoveredCount;
                infectiousCount += res.infectiousCount;
                funeralCount += res.funeralCount;
                deadCount += res.deadCount;
                time = (time >= res.time) ? time : res.time;
            }

            this.totalResult.Add(new int[6] {
                suspectableCount,
                recoveredCount,
                infectiousCount,
                funeralCount,
                deadCount,
                time
            });

            Trace.TraceInformation("Results ({6}):\nSuspectable: {0}\nRecovered: {1}\nInfectious: {2}\nFuneral: {3}\nDead: {4}\nTime: {5}", suspectableCount, recoveredCount, infectiousCount, funeralCount, deadCount, time, this.iterationNum);
        }

        private void calculateTotalResult()
        {
            double suspectableCount = this.totalResult.Select((d) => d[0]).Average();
            double recoveredCount = this.totalResult.Select((d) => d[1]).Average();
            double infectiousCount = this.totalResult.Select((d) => d[2]).Average();
            double funeralCount = this.totalResult.Select((d) => d[3]).Average();
            double deadCount = this.totalResult.Select((d) => d[4]).Average();
            double time = this.totalResult.Select((d) => d[5]).Average();

            Trace.TraceInformation("Complete results:\nSuspectable: {0}\nRecovered: {1}\nInfectious: {2}\nFuneral: {3}\nDead: {4}\nTime: {5}", suspectableCount, recoveredCount, infectiousCount, funeralCount, deadCount, time);
        }

        private void startTick()
        {
            this.tickThread = new Thread((obj) =>
            {
                while(true)
                {
                    bool waitRes = WaitHandle.WaitAll(this.waiters.Values.ToArray(), waitTimeout);
                    if (!waitRes)
                    {
                        Trace.TraceWarning("!!! Wait timeout !!!");
                    }

                    GlobalTime.Time += 1;

                    if (this.results.Values.All((res) => res != null)) {
                        break;
                    }

                    foreach(QueueClient c in this.workers.Values)
                    {
                        var msg = new BrokeredMessage(new Message(this.guid, MessageType.Tick));
                        msg.ContentType = typeof(Message).Name;
                        c.Send(msg);
                    }
                }
            });
            tickThread.Start();

            foreach(AutoResetEvent e in this.waiters.Values)
            {
                e.Set();
            }
        }

        public void Run2()
        {
            this.iterationNum++;

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
            Trace.TraceInformation("Started: {0}", this.iterationNum);
        }

        private void start()
        {
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
        }

        private void restart()
        {
            foreach (AutoResetEvent e in this.waiters.Values)
                e.Set();

            foreach (QueueClient c in this.workers.Values)
            {
                var msg = new BrokeredMessage(new Message(this.guid, MessageType.Clear));
                msg.ContentType = typeof(Message).Name;
                c.Send(msg);
            }

            Containers.Instance.Clear();
            this.agents.Clear();
            this.agentLocations.Clear();

            for (int i = 0; i < this.results.Count; i++)
            {
                var key = this.results.ElementAt(i).Key;
                this.results[key] = null;
            }

            if (this.iterationNum >= numberOfIterations)
            {
                this.calculateTotalResult();
                return;
            }

            Trace.TraceInformation("Restarting...");

            foreach (var w in this.waiters.Values)
                w.Reset();

            this.start();
        }

        private void infectOtherAgent(int sourceAgentId)
        {
            if (this.agents.Count < 2)
            {
                Trace.TraceInformation("Warning: too litle agents");
                return;
            }
            // Random:
            // int idx = random.Next(0, this.agents.Count - 1);

            ContainersCore currentContainer = this.agentLocations[sourceAgentId];
            if (currentContainer == null)
            {
                Trace.TraceInformation("Warning: no current container for {0}", sourceAgentId);
                return;
            }

            if (currentContainer is Home)
            {
                // Trace.TraceInformation("Current container for {0} is Home", sourceAgentId);
                return;
            }

            if (currentContainer.AgentCount < 2)
            {
                Trace.TraceInformation("Warning: noone in container for {0}", sourceAgentId);
                return;
            }

            int destAgentId = sourceAgentId;
            while (destAgentId == sourceAgentId)
            {
                destAgentId = currentContainer.GetRandomAgent();
            }

            QueueClient iClient = this.workers[this.agents[destAgentId]];
            var msg0 = new Message(this.guid, MessageType.Infect);
            msg0.data = destAgentId.ToString();
            //Trace.TraceInformation("Infecting: {0} -> {1}", sourceAgentId, destAgentId);
            var msg = new BrokeredMessage(msg0);
            msg.ContentType = typeof(Message).Name;
            iClient.Send(msg);
        }

        private void gotoContainer(int agentId, int containerId)
        {
            //if (agentId == 2)
            //    Trace.TraceInformation("Go: {0} to {1} ; Time: {2}", agentId, containerId, GlobalTime.realTime);

            int idx = Containers.Instance.FindIndex((c) => c.Id == containerId);
            if (idx < 0)
            {
                Trace.TraceWarning("Warning: Container with id {0} not found", containerId);
                return;
            }

            ContainersCore container = Containers.Instance[idx];
            ContainersCore oldContainer = this.agentLocations[agentId];
            container.AddPersonInContainer(agentId);
            if (oldContainer != null)
                oldContainer.DeletePersonFromContainer(agentId);
            agentLocations[agentId] = container;
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
                    switch (receivedMessage.ContentType)
                    {
                        case "Message":
                            message = receivedMessage.GetBody<Message>();
                            break;
                        case "AddAgentMessage":
                            Trace.TraceWarning("Unexpected message");
                            break;
                        case "ResultsMessage":
                            message = receivedMessage.GetBody<ResultsMessage>();
                            break;
                        case "GoToContainerMessage":
                            message = receivedMessage.GetBody<GoToContainerMessage>();
                            break;
                    }
                    
                }
                catch (Exception e)
                {
                    Trace.TraceError("Error: {0}", e);
                }

                if (message != null)
                {
                    // Trace.TraceInformation("Received message of type: {0}", message.type);
                    switch(message.type)
                    {
                        case MessageType.Registration:
                            QueueClient senderClient = QueueClient.CreateFromConnectionString(this.connectionString, message.senderId.ToString(), ReceiveMode.ReceiveAndDelete);
                            this.workers.Add(message.senderId, senderClient);
                            this.waiters.Add(message.senderId, new AutoResetEvent(false));
                            this.results.Add(message.senderId, null);
                            break;
                        case MessageType.Infect:
                            if (isStarted)
                                this.infectOtherAgent(Int32.Parse(message.data));
                            break;
                        case MessageType.TickEnd:
                            if (isStarted)
                                this.waiters[message.senderId].Set();
                            break;
                        case MessageType.Results:
                            if (isStarted)
                            {
                                this.results[message.senderId] = (ResultsMessage)message;
                                if (this.results.Values.All((res) => res != null))
                                {
                                    this.isStarted = false;

                                    this.calculateResult();

                                    this.restart();
                                }                                
                            }
                            break;
                        case MessageType.GoTo:
                            if (isStarted)
                            {
                                GoToContainerMessage gtMessage = (GoToContainerMessage)message;

                                foreach (int agentId in gtMessage.infectionSourceAgentIds)
                                {
                                    this.infectOtherAgent(agentId);
                                }

                                for (int i = 0; i < gtMessage.agentIds.Length; i++)
                                {
                                    this.gotoContainer(gtMessage.agentIds[i], gtMessage.containerIds[i]);
                                }

                                this.waiters[message.senderId].Set();
                            }
                            break;
                    }
                }

            });

            this.start();

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
