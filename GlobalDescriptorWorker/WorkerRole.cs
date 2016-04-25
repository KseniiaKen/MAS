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

        private const int registerTimeout = 60000;
        private const int waitTimeout = 30000;
        private const int numberOfIterations = 80;
        private AutoResetEvent stopEvent = new AutoResetEvent(false);
        private Random random = new Random();
        private bool isStarted = false;
        private Thread tickThread;
        private int iterationNum = 0;

        private List<Guid> workers = new List<Guid>();
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
                Guid workerId = this.workers.ElementAt(i % this.workers.Count);
                this.agents.Add(agent.GetId(), workerId);
                this.agentLocations.Add(agent.GetId(), null);

                MessageTransportSystem.Instance.SendMessage(new AddAgentMessage(MessageTransportSystem.Instance.Id, agent.GetType().Name, agent.GetId(), agent.GetHealthState(), 1), workerId);
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

                    MessageTransportSystem.Instance.SendEveryone(new Message(MessageTransportSystem.Instance.Id, MessageType.Tick));
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

            MessageTransportSystem.Instance.SendEveryone(new Message(MessageTransportSystem.Instance.Id, MessageType.Start));

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
                foreach (Guid g in this.workers)
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

            MessageTransportSystem.Instance.SendEveryone(new Message(MessageTransportSystem.Instance.Id, MessageType.Clear));

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

            Guid clientId = this.agents[destAgentId];
            var msg0 = new Message(MessageTransportSystem.Instance.Id, MessageType.Infect);
            msg0.data = destAgentId.ToString();
            //Trace.TraceInformation("Infecting: {0} -> {1}", sourceAgentId, destAgentId);
            MessageTransportSystem.Instance.SendMessage(msg0, clientId);
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
            MessageTransportSystem.Instance.OnRegistrationMessage += onRegistrationMessage;
            MessageTransportSystem.Instance.OnInfectMessage += onInfectMessage;
            MessageTransportSystem.Instance.OnTickEndMessage += onTickEndMessage;
            MessageTransportSystem.Instance.OnResultsMessage += onResultsMessage;
            MessageTransportSystem.Instance.OnGotoMessage += onGotoMessage;

            MessageTransportSystem.Instance.StartListening();

            this.start();

            this.stopEvent.WaitOne();
        }

        private void onGotoMessage(Message message)
        {
            if (isStarted && this.workers.Contains(message.senderId))
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
        }

        private void onResultsMessage(Message message)
        {
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
        }

        private void onInfectMessage(Message message)
        {
            if (isStarted)
                this.infectOtherAgent(Int32.Parse(message.data));
        }

        private void onTickEndMessage(Message message)
        {
            if (isStarted)
                this.waiters[message.senderId].Set();
        }

        private void onRegistrationMessage(Message message)
        {
            this.workers.Add(message.senderId);
            this.waiters.Add(message.senderId, new AutoResetEvent(false));
            this.results.Add(message.senderId, null);
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections            
            MessageTransportSystem.Instance.Init();

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

            MessageTransportSystem.Instance.DeInit();

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
