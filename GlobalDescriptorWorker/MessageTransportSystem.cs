using CoreAMS.Messages;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GlobalDescriptorWorker
{
    public class MessageTransportSystem : CoreAMS.MessageTransportSystem.IMessageTransportSystem
    {
        private const string NODE_ID = "DB3C13B1-0D35-4885-9F4A-393ED089CBEB";
        private static Guid guid = new Guid(NODE_ID);
        private const string CONNECTION_STRING = @"Endpoint=sb://My_computer/ServiceBusDefaultNamespace;StsEndpoint=https://My_computer:9355/ServiceBusDefaultNamespace;RuntimePort=9354;ManagementPort=9355";

        public delegate void MessageEventHandler(Message message);
        public event MessageEventHandler OnRegistrationMessage;
        public event MessageEventHandler OnInfectMessage;
        public event MessageEventHandler OnGotoMessage;
        public event MessageEventHandler OnResultsMessage;
        public event MessageEventHandler OnTickEndMessage;

        private static MessageTransportSystem instance = new MessageTransportSystem();

        private SubscriptionClient receiveClient;
        private TopicClient sendClient;
        private List<Guid> workers = new List<Guid>();

        public static MessageTransportSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MessageTransportSystem();
                }

                return instance;
            }
        }

        public Guid Id
        {
            get
            {
                return guid;
            }
        }

        public void Init()
        {
            ServicePointManager.DefaultConnectionLimit = 12;

            var namespaceManager = NamespaceManager.CreateFromConnectionString(CONNECTION_STRING);
            if (!namespaceManager.TopicExists(NODE_ID))
            {
                namespaceManager.CreateTopic(NODE_ID);
            }
            this.sendClient = TopicClient.CreateFromConnectionString(CONNECTION_STRING, NODE_ID);
            if (!namespaceManager.SubscriptionExists(NODE_ID, "MyMessages"))
            {
                Filter filter = new SqlFilter(String.Format("receiverId = '{0}'", this.Id.ToString()));
                namespaceManager.CreateSubscription(NODE_ID, "MyMessages", filter);
            }
            this.receiveClient = SubscriptionClient.CreateFromConnectionString(CONNECTION_STRING, NODE_ID, "MyMessages", ReceiveMode.ReceiveAndDelete);
        }

        public void DeInit()
        {
            if (receiveClient != null)
            {
                while (receiveClient.Peek() != null)
                {
                    Trace.TraceInformation("Cleaning message");
                    var brokeredMessage = receiveClient.Receive();
                    brokeredMessage.Complete();
                }
                this.receiveClient.Close();

                this.sendClient.Close();
            }
        }

        public int WorkersCount
        {
            get
            {
                return this.workers.Count;
            }
        }

        public void StartListening()
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
            this.receiveClient.OnMessage((receivedMessage) => // Initiates the message pump and callback is invoked for each message that is recieved, calling close on the client will stop the pump.
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
                            Trace.TraceWarning("Warning: Received unexpected message. Type: {0}", receivedMessage.ContentType);
                            break;
                        case "ResultsMessage":
                            message = receivedMessage.GetBody<ResultsMessage>();
                            break;
                        case "GoToContainerMessage":
                            message = receivedMessage.GetBody<GoToContainerMessage>();
                            break;
                        case "AddContainerMessage":
                        case "TickMessage":
                        case "StartMessage":
                            Trace.TraceWarning("Warning: Received unexpected message. Type: {0}", receivedMessage.ContentType);
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
                    switch (message.type)
                    {
                        case MessageType.AddAgent:
                        case MessageType.Clear:
                            Trace.TraceWarning("Warning: Received unexpected message. Type: {0}; Sender: {1}", message.type, message.senderId);
                            break;
                        case MessageType.GoTo:
                            if (this.OnGotoMessage != null)
                                this.OnGotoMessage(message);
                            break;
                        case MessageType.Infect:
                            if (this.OnInfectMessage != null)
                                this.OnInfectMessage(message);
                            break;
                        case MessageType.Registration:
                            this.addClient(message.senderId);
                            if (this.OnRegistrationMessage != null)
                                this.OnRegistrationMessage(message);
                            break;
                        case MessageType.Results:
                            if (this.OnResultsMessage != null)
                                this.OnResultsMessage(message);
                            break;
                        case MessageType.Start:
                        case MessageType.Tick:
                            Trace.TraceWarning("Warning: Received unexpected message. Type: {0}; Sender: {1}", message.type, message.senderId);
                            break;
                        case MessageType.TickEnd:
                            if (this.OnTickEndMessage != null)
                                this.OnTickEndMessage(message);
                            break;
                        case MessageType.AddContainer:
                            Trace.TraceWarning("Warning: Received unexpected message. Type: {0}; Sender: {1}", message.type, message.senderId);
                            break;
                    }
                }
            });
        }

        private void addClient(Guid clientId)
        {
            this.workers.Add(clientId);
        }

        public void SendMessage(Message message)
        {
            throw new NotImplementedException("MessageTransportSystem.SendMessage is not implemented");
        }

        public void SendMessage(Message message, Guid workerId)
        {
                var msg = new BrokeredMessage(message);
                msg.ContentType = message.GetType().Name;
                msg.Properties["receiverId"] = workerId.ToString();
                this.sendClient.Send(msg);
        }

        public void SendEveryone(Message message)
        {
            foreach (Guid c in this.workers)
            {
                var msg = new BrokeredMessage(message);
                msg.ContentType = message.GetType().Name;
                msg.Properties["receiverId"] = c.ToString();
                sendClient.Send(msg);
            }
        }

        public Dictionary<Message, Guid> SendSpread(List<Message> messages)
        {
            if (this.workers.Count == 0)
            {
                return new Dictionary<Message, Guid>();
            }

            var result = new Dictionary<Message, Guid>();

            for (int i = 0; i< messages.Count; i++)
            {
                int workerIdx = i % this.workers.Count;
                Guid workerId = workers[workerIdx];
                Message message = messages[i];

                var msg = new BrokeredMessage(message);
                msg.ContentType = message.GetType().Name;
                msg.Properties["receiverId"] = workerId.ToString();
                sendClient.Send(msg);

                result[message] = workerId;
            }

            return result;
        }

    }
}
