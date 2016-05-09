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

namespace CloudSimulationWorker
{
    public class MessageTransportSystem : CoreAMS.MessageTransportSystem.IMessageTransportSystem
    {
        private static Guid NODE_ID = Guid.NewGuid();
        private const string GLOBAL_DESCRIPTOR_QUEUE_NAME = "FF76E790-80DE-4DC6-8712-6D661C9807C5";
        private const string CONNECTION_STRING = @"Endpoint=sb://My_computer/ServiceBusDefaultNamespace;StsEndpoint=https://My_computer:9355/ServiceBusDefaultNamespace;RuntimePort=9354;ManagementPort=9355";

        public delegate void MessageEventHandler(Message message);
        public event MessageEventHandler OnAddAgentMessage;
        public event MessageEventHandler OnStartMessage;
        public event MessageEventHandler OnInfectMessage;
        public event MessageEventHandler OnTickMessage;
        public event MessageEventHandler OnClearMessage;
        public event MessageEventHandler OnAddContainerMessage;

        private static MessageTransportSystem instance = new MessageTransportSystem();

        private SubscriptionClient client;
        private TopicClient globalDescriptorClient;

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
                return NODE_ID;
            }
        }

        public void Init()
        {
            ServicePointManager.DefaultConnectionLimit = 12;

            var namespaceManager = NamespaceManager.CreateFromConnectionString(CONNECTION_STRING);
            if (!namespaceManager.TopicExists(GLOBAL_DESCRIPTOR_QUEUE_NAME))
            {
                namespaceManager.CreateTopic(GLOBAL_DESCRIPTOR_QUEUE_NAME);
            }
            if (!namespaceManager.SubscriptionExists(GLOBAL_DESCRIPTOR_QUEUE_NAME, this.Id.ToString()))
            {
                Filter filter = new SqlFilter(String.Format("receiverId = '{0}'", this.Id.ToString()));
                namespaceManager.CreateSubscription(GLOBAL_DESCRIPTOR_QUEUE_NAME, this.Id.ToString(), filter);
            }
            this.client = SubscriptionClient.CreateFromConnectionString(CONNECTION_STRING, GLOBAL_DESCRIPTOR_QUEUE_NAME, this.Id.ToString(), ReceiveMode.ReceiveAndDelete);
            this.globalDescriptorClient = TopicClient.CreateFromConnectionString(CONNECTION_STRING, GLOBAL_DESCRIPTOR_QUEUE_NAME);
        }

        public void DeInit()
        {
            if (client != null)
            {
                while (client.Peek() != null)
                {
                    Trace.TraceInformation("Cleaning message");
                    var brokeredMessage = client.Receive();
                    brokeredMessage.Complete();
                }
                this.client.Close();

                this.globalDescriptorClient.Close();
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
                            message = receivedMessage.GetBody<AddAgentMessage>();
                            break;
                        case "ResultsMessage":
                        case "GoToContainerMessage":
                            Trace.TraceWarning("Warning: Received unexpected message. Type: {0}", receivedMessage.ContentType);
                            break;
                        case "AddContainerMessage":
                            message = receivedMessage.GetBody<AddContainerMessage>();
                            break;
                        case "TickMessage":
                            message = receivedMessage.GetBody<TickMessage>();
                            break;
                        case "StartMessage":
                            message = receivedMessage.GetBody<StartMessage>();
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
                            if (this.OnAddAgentMessage != null)
                                this.OnAddAgentMessage(message);
                            break;
                        case MessageType.Clear: 
                            if (this.OnClearMessage != null)
                                this.OnClearMessage(message);
                            break;
                        case MessageType.GoTo:
                            Trace.TraceWarning("Warning: Received unexpected message. Type: {0}; Sender: {1}", message.type, message.senderId);
                            break;
                        case MessageType.Infect:
                            if (this.OnInfectMessage != null)
                                this.OnInfectMessage(message);
                            break;
                        case MessageType.Registration:
                        case MessageType.Results:
                            Trace.TraceWarning("Warning: Received unexpected message. Type: {0}; Sender: {1}", message.type, message.senderId);
                            break;
                        case MessageType.Start:
                            if (this.OnStartMessage != null)
                                this.OnStartMessage(message);
                            break;
                        case MessageType.Tick:
                            if (this.OnTickMessage != null)
                                this.OnTickMessage(message);
                            break;
                        case MessageType.TickEnd:
                            Trace.TraceWarning("Warning: Received unexpected message. Type: {0}; Sender: {1}", message.type, message.senderId);
                            break;
                        case MessageType.AddContainer:
                            if (this.OnAddContainerMessage != null)
                                this.OnAddContainerMessage(message);
                            break;

                    }
                }
            });
        }

        public void SendMessage(Message message)
        {
            var msg = new BrokeredMessage(message);
            msg.ContentType = message.GetType().Name;
            msg.Properties["receiverId"] = new Guid(GLOBAL_DESCRIPTOR_QUEUE_NAME).ToString();
            this.globalDescriptorClient.Send(msg);
        }

        public void SendMessage(Message message, Guid clientId)
        {
            throw new NotImplementedException("MessageTransportSystem.SendMessage is not implemented");
        }
    }
}
