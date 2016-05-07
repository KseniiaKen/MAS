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
using System.IO;

namespace GlobalDescriptorWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private const int registerTimeout = 60000;
        private const int waitTimeout = 30000;
        private const int numberOfIterations = 3;
        private AutoResetEvent stopEvent = new AutoResetEvent(false);
        private Random random = new Random();
        private bool isStarted = false;
        private Thread tickThread;
        private int iterationNum = 0;

        private List<Guid> workers = new List<Guid>();
        private Dictionary<Guid, AutoResetEvent> waiters = new Dictionary<Guid, AutoResetEvent>();
        private Dictionary<Guid, Result> results = new Dictionary<Guid, Result>(); // список id worker-ов + их результаты
        private Dictionary<Guid, Result> intermediateResults = new Dictionary<Guid, Result>(); // список id worker-ов + их промежуточные результаты
        private Dictionary<int, Guid> containers2workers = new Dictionary<int, Guid>(); // в каком worker-е находится контейнер с id равным ключу
        private Dictionary<int, ContainersCore> agentLocations = new Dictionary<int, ContainersCore>();
        private Dictionary<Guid, List<AddAgentMessage>> addAgentMessages = new Dictionary<Guid, List<AddAgentMessage>>();
        private Dictionary<Guid, int> agentCounters = new Dictionary<Guid, int>();
        private List<Result> totalResult = new List<Result>();
        private DateTime startTime = DateTime.Now;

        private void fillContainers()
        {
            int homeCount = 1000;
            int hospitalCount = 3;
            int mallCount = 5;
            int officeCount = 10;
            int univercityCount = 1;
            int schoolCount = 3;
            int nurseryCount = 3;
            //int homeCount = 1;
            //int hospitalCount = 1;
            //int mallCount = 1;
            //int officeCount = 1;
            //int univercityCount = 1;
            //int schoolCount = 1;
            //int nurseryCount = 1;

            List<Message> messagesToSend = new List<Message>();
            int currentId = 0;

            // Messages for home containers creation
            List<AddContainerMessage> homes = new List<AddContainerMessage>();            
            for (int i = 0; i < homeCount; i++)
            {
                homes.Add(new AddContainerMessage(MessageTransportSystem.Instance.Id, Enums.ContainerType.Home, currentId, 50, 12));
                Containers.Instance.Add(currentId, new Home(currentId, 50, 12));
                currentId++;
            }
            // Everyone at home at the beginning
            List<Person> p = new List<Person>();
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
                int homeNum = i % homeCount;
                Person a = p[i];
                homes[homeNum].agentData.Add(new AddAgentMessage(MessageTransportSystem.Instance.Id, a.GetType().Name, a.GetId(), a.GetHealthState(), 1));
                this.agentLocations[a.GetId()] = Containers.Instance[homes[homeNum].containerId];
            }
            messagesToSend.AddRange(homes);

            // Messages for hospital containers creation
            List<AddContainerMessage> hospitals = new List<AddContainerMessage>();
            for (int i = 0; i < hospitalCount; i++)
            {
                hospitals.Add(new AddContainerMessage(MessageTransportSystem.Instance.Id, Enums.ContainerType.Hospital, currentId, 237, 19));
                Containers.Instance.Add(currentId, new Hospital(currentId, 237, 19));
                currentId++;
            }
            messagesToSend.AddRange(hospitals);

            // Messages for mall containers creation
            List<AddContainerMessage> malls = new List<AddContainerMessage>();
            for (int i = 0; i < mallCount; i++)
            {
                malls.Add(new AddContainerMessage(MessageTransportSystem.Instance.Id, Enums.ContainerType.Mall, currentId, 578, 90));
                Containers.Instance.Add(currentId, new Mall(currentId, 578, 90));
                currentId++;
            }
            messagesToSend.AddRange(malls);

            // Messages for office containers creation
            List<AddContainerMessage> offices = new List<AddContainerMessage>();
            for (int i = 0; i < officeCount; i++)
            {
                offices.Add(new AddContainerMessage(MessageTransportSystem.Instance.Id, Enums.ContainerType.Office, currentId, 236, 20));
                Containers.Instance.Add(currentId, new Office(currentId, 236, 20));
                currentId++;
            }
            messagesToSend.AddRange(offices);

            // Messages for univercity containers creation
            List<AddContainerMessage> univercities = new List<AddContainerMessage>();
            for (int i = 0; i < univercityCount; i++)
            {
                univercities.Add(new AddContainerMessage(MessageTransportSystem.Instance.Id, Enums.ContainerType.University, currentId, 300, 25));
                Containers.Instance.Add(currentId, new University(currentId, 300, 25));
                currentId++;
            }
            messagesToSend.AddRange(univercities);

            // Messages for school containers creation
            List<AddContainerMessage> schools = new List<AddContainerMessage>();
            for (int i = 0; i < schoolCount; i++)
            {
                schools.Add(new AddContainerMessage(MessageTransportSystem.Instance.Id, Enums.ContainerType.School, currentId, 237, 19));
                Containers.Instance.Add(currentId, new School(currentId, 237, 19));
                currentId++;
            }
            messagesToSend.AddRange(schools);

            // Messages for nursery containers creation
            List<AddContainerMessage> nurseries = new List<AddContainerMessage>();
            for (int i = 0; i < nurseryCount; i++)
            {
                nurseries.Add(new AddContainerMessage(MessageTransportSystem.Instance.Id, Enums.ContainerType.Nursery, currentId, 60, 23));
                Containers.Instance.Add(currentId, new Nursery(currentId, 60, 23));
                currentId++;
            }
            messagesToSend.AddRange(nurseries);

            // Now sending all messages equally spread to all CS workers and saving what container on what worker
            this.containers2workers.Clear();
            Dictionary<Message, Guid> res = MessageTransportSystem.Instance.SendSpread(messagesToSend);
            foreach(var r in res)
            {
                AddContainerMessage message = (AddContainerMessage)r.Key;
                Guid workerId = r.Value;
                this.containers2workers[message.containerId] = workerId;
            }
        }

        private void calculateIntermediateResults()
        {
            lock (this.intermediateResults)
            {

                int suspectableCount = 0;
                int recoveredCount = 0;
                int infectiousCount = 0;
                int funeralCount = 0;
                int deadCount = 0;
                int exposedCount = 0;
                int time = 0;
                TimeSpan realTime = DateTime.Now - this.startTime;

                foreach (Result res in this.intermediateResults.Values)
                {
                    suspectableCount += res.suspectableCount;
                    recoveredCount += res.recoveredCount;
                    infectiousCount += res.infectiousCount;
                    funeralCount += res.funeralCount;
                    deadCount += res.deadCount;
                    exposedCount += res.exposedCount;
                    time = (time >= res.executionTime) ? time : res.executionTime;
                }

                // Trace.TraceInformation("Intermediate results ({6}):\nSuspectable: {0}\nRecovered: {1}\nExposed: {7}\nInfectious: {2}\nFuneral: {3}\nDead: {4}\nTime: {5}\nReal time: {8}", suspectableCount, recoveredCount, infectiousCount, funeralCount, deadCount, time, GlobalTime.Day, exposedCount, realTime);
                Trace.TraceInformation("Intermediate results ({6}): {0} {1} {7} {2} {3} {4} {5} {8}", suspectableCount, recoveredCount, infectiousCount, funeralCount, deadCount, time, GlobalTime.Day, exposedCount, realTime);

                saveResult(new Result(
                    suspectableCount,
                    recoveredCount,
                    exposedCount,
                    infectiousCount,
                    funeralCount,
                    deadCount,
                    time,
                    (int)realTime.TotalSeconds
                    ), "intermediate");

                foreach (var key in this.results.Keys)
                {
                    this.intermediateResults[key] = null;
                }
            }
        }

        private void calculateResult()
        {
            int suspectableCount = 0;
            int recoveredCount = 0;
            int infectiousCount = 0;
            int funeralCount = 0;
            int deadCount = 0;
            int exposedCount = 0;
            int time = 0;
            TimeSpan realTime = DateTime.Now - this.startTime;

            foreach (Result res in this.results.Values)
            {
                suspectableCount += res.suspectableCount;
                recoveredCount += res.recoveredCount;
                infectiousCount += res.infectiousCount;
                funeralCount += res.funeralCount;
                deadCount += res.deadCount;
                exposedCount += res.exposedCount;
                time = (time >= res.executionTime) ? time : res.executionTime;
            }

            Result newResult = new Result(
                suspectableCount,
                recoveredCount,
                exposedCount,
                infectiousCount,
                funeralCount,
                deadCount,
                time,
                (int)realTime.TotalSeconds
            );

            this.totalResult.Add(newResult);
            saveResult(newResult, "");

            Trace.TraceInformation("Results ({6}):\nSuspectable: {0}\nRecovered: {1}\nExposed: {7}\nInfectious: {2}\nFuneral: {3}\nDead: {4}\nTime: {5}\nReal time: {8}", suspectableCount, recoveredCount, infectiousCount, funeralCount, deadCount, time, this.iterationNum, exposedCount, realTime);
        }

        private void calculateTotalResult()
        {
            double suspectableCount = this.totalResult.Select((d) => d.suspectableCount).Average();
            double recoveredCount = this.totalResult.Select((d) => d.recoveredCount).Average();
            double infectiousCount = this.totalResult.Select((d) => d.infectiousCount).Average();
            double funeralCount = this.totalResult.Select((d) => d.funeralCount).Average();
            double deadCount = this.totalResult.Select((d) => d.deadCount).Average();
            double time = this.totalResult.Select((d) => d.executionTime).Average();
            double realTime = this.totalResult.Select((d) => d.realTime).Average();

            Trace.TraceInformation("Complete results:\nSuspectable: {0}\nRecovered: {1}\nInfectious: {2}\nFuneral: {3}\nDead: {4}\nTime: {5}\nReal time: {6}", suspectableCount, recoveredCount, infectiousCount, funeralCount, deadCount, time, realTime);
        }

        private static void saveResult(Result result, string suffix)
        {
            string fileName = String.Format("D:/FileOfResults_{0}.csv", suffix);
            string lineFormat = "{0},{1},{2},{3},{4},{5},{6},{7},{8}";

            bool isNewFile = false;

            if (!File.Exists(fileName))
            {
                isNewFile = true;
                File.Create(fileName).Dispose();
            }

            using (StreamWriter resultsFile = File.AppendText(fileName))
            {
                if (isNewFile)
                {
                    resultsFile.WriteLine(lineFormat,
                        "current_time",
                        "suspectable",
                        "recovered",
                        "exposed",
                        "infectious",
                        "funeral",
                        "dead",
                        "execution time",
                        "execution real time"
                        );
                }
                resultsFile.WriteLine(lineFormat, 
                    DateTime.Now,
                    result.suspectableCount, 
                    result.recoveredCount, 
                    result.exposedCount, 
                    result.infectiousCount, 
                    result.funeralCount, 
                    result.deadCount, 
                    result.executionTime, 
                    result.realTime
                    );
            }
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

                    if (GlobalTime.Time != 0 && GlobalTime.realTime == 0 && this.intermediateResults.Values.All((res) => res != null))
                    {
                        this.calculateIntermediateResults();
                    }

                    GlobalTime.Time += 1;

                    if (this.results.Values.All((res) => res != null)) {
                        break;
                    }

                    // MessageTransportSystem.Instance.SendEveryone(new Message(MessageTransportSystem.Instance.Id, MessageType.Tick));
                    lock (this.addAgentMessages)
                    {
                        foreach (var kvp in this.addAgentMessages)
                        {

                            MessageTransportSystem.Instance.SendMessage(new TickMessage(MessageTransportSystem.Instance.Id, kvp.Value.ToArray()), kvp.Key);
                            kvp.Value.Clear();
                        }
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

            this.fillContainers();
            // fillAgents();

            MessageTransportSystem.Instance.SendEveryone(new StartMessage(MessageTransportSystem.Instance.Id, this.agentLocations.Count, this.containers2workers.Count, MessageTransportSystem.Instance.WorkersCount));
            this.startTime = DateTime.Now;

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
            this.containers2workers.Clear();
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

        //private void infectOtherAgent(int sourceAgentId)
        //{
        //    if (this.agents.Count < 2)
        //    {
        //        Trace.TraceInformation("Warning: too litle agents");
        //        return;
        //    }
        //    // Random:
        //    // int idx = random.Next(0, this.agents.Count - 1);

        //    ContainersCore currentContainer = this.agentLocations[sourceAgentId];
        //    if (currentContainer == null)
        //    {
        //        Trace.TraceInformation("Warning: no current container for {0}", sourceAgentId);
        //        return;
        //    }

        //    if (currentContainer is Home)
        //    {
        //        // Trace.TraceInformation("Current container for {0} is Home", sourceAgentId);
        //        return;
        //    }

        //    if (currentContainer.AgentCount < 2)
        //    {
        //        Trace.TraceInformation("Warning: noone in container for {0}", sourceAgentId);
        //        return;
        //    }

        //    int destAgentId = sourceAgentId;
        //    while (destAgentId == sourceAgentId)
        //    {
        //        destAgentId = currentContainer.GetRandomAgent();
        //    }

        //    Guid clientId = this.agents[destAgentId];
        //    var msg0 = new Message(MessageTransportSystem.Instance.Id, MessageType.Infect);
        //    msg0.data = destAgentId.ToString();
        //    //Trace.TraceInformation("Infecting: {0} -> {1}", sourceAgentId, destAgentId);
        //    MessageTransportSystem.Instance.SendMessage(msg0, clientId);
        //}

        private void gotoContainer(AddAgentMessage amsg, Enums.ContainerType containerType)
        {
            //if (agentId == 2)
            //    Trace.TraceInformation("Go: {0} to {1} ; Time: {2}", agentId, containerId, GlobalTime.realTime);

            List<ContainersCore> containersWithType = Containers.Instance.Values.Where((c) => c.ContainerType == containerType).ToList();
            if (containersWithType.Count == 0)
            {
                Trace.TraceWarning("Warning: Container with type {0} not found", containerType);
                return;
            }

            int idx = this.random.Next(0, containersWithType.Count - 1);
            ContainersCore container = containersWithType[idx];
            ContainersCore oldContainer = this.agentLocations[amsg.agentId];

            amsg.containerId = container.Id;

            Guid workerId = this.containers2workers[container.Id];
            // MessageTransportSystem.Instance.SendMessage(amsg, workerId);
            this.addAgentMessages[workerId].Add(amsg);

            agentLocations[amsg.agentId] = container;
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
                    //this.infectOtherAgent(agentId);
                }

                lock (this.addAgentMessages)
                {
                    for (int i = 0; i < gtMessage.agents.Length; i++)
                    {
                        this.gotoContainer(gtMessage.agents[i], gtMessage.containerTypes[i]);
                    }
                }

                if (gtMessage.containerToMove != null)
                {
                    moveContainer(gtMessage.containerToMove);
                }

                this.agentCounters[message.senderId] = gtMessage.agentCount;

                if (gtMessage.currentResult != null) {
                    lock (this.intermediateResults)
                    {
                        this.intermediateResults[message.senderId] = gtMessage.currentResult;
                    }
                }

                this.waiters[message.senderId].Set();
            }
        }

        private void moveContainer(AddContainerMessage message)
        {
            // Trace.TraceInformation("Container requested to move: {0}", message.containerId);

            // int idx = random.Next(0, this.results.Count - 1);
            // Guid workerId = this.results.ElementAt(idx).Key;

            Guid workerId =
                this.agentCounters
                .OrderBy(kvp => kvp.Value)
                .First().Key;

            // Trace.TraceInformation("Selected worker for container: {0}. Agent count: {1}", workerId, this.agentCounters[workerId]);

            MessageTransportSystem.Instance.SendMessage(message, workerId);

            this.containers2workers[message.containerId] = workerId;
        }

        private void onResultsMessage(Message message)
        {
            if (isStarted)
            {
                this.results[message.senderId] = (message as ResultsMessage).result;
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
            //if (isStarted)
            //    this.infectOtherAgent(Int32.Parse(message.data));
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
            this.intermediateResults.Add(message.senderId, null);
            this.addAgentMessages.Add(message.senderId, new List<AddAgentMessage>());
            this.agentCounters.Add(message.senderId, 0);
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
