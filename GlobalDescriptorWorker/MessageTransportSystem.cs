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
    public class MessageTransportSystem
    {
        private const string NODE_ID = "GlobalDescriptor";
        private const string CONNECTION_STRING = @"Endpoint=sb://My_computer/ServiceBusDefaultNamespace;StsEndpoint=https://My_computer:9355/ServiceBusDefaultNamespace;RuntimePort=9354;ManagementPort=9355";

        public delegate void MessageEventHandler(Message message);
        public event MessageEventHandler OnRegistrationMessage;
        public event MessageEventHandler OnInfectMessage;
        public event MessageEventHandler OnGotoMessage;
        public event MessageEventHandler OnResultsMessage;
        public event MessageEventHandler OnTickEndMessage;

        private static MessageTransportSystem instance = new MessageTransportSystem();

        private QueueClient client;
        private Dictionary<Guid, QueueClient> workers = new Dictionary<Guid, QueueClient>(); // список id worker-ов + клиенты. чтобы знать куда отправлять сообщения

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

        public void Init()
        {
            ServicePointManager.DefaultConnectionLimit = 12;

            var namespaceManager = NamespaceManager.CreateFromConnectionString(CONNECTION_STRING);
            if (!namespaceManager.QueueExists(NODE_ID))
            {
                namespaceManager.CreateQueue(NODE_ID);
            }
            this.client = QueueClient.CreateFromConnectionString(CONNECTION_STRING, NODE_ID, ReceiveMode.ReceiveAndDelete);
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
            }

            foreach (QueueClient c in this.workers.Values)
            {
                c.Close();
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
                            Trace.TraceWarning("Warning: Received unexpected message. Type: {0}", receivedMessage.ContentType);
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
                    }
                }
            });
        }

        private void addClient(Guid clientId)
        {
            QueueClient clientClient = QueueClient.CreateFromConnectionString(CONNECTION_STRING, clientId.ToString(), ReceiveMode.ReceiveAndDelete);
            this.workers.Add(clientId, clientClient);
        }

        public void SendMessage(Message message, Guid workerId)
        {
            QueueClient workerData = this.workers[workerId];

            if (workerData != null)
            {
                var msg = new BrokeredMessage(message);
                msg.ContentType = message.GetType().Name;
                workerData.Send(msg);
            }
        }

        public void SendEveryone(Message message)
        {
            foreach (QueueClient c in this.workers.Values)
            {
                var msg = new BrokeredMessage(message);
                msg.ContentType = message.GetType().Name;
                c.Send(msg);
            }
        }

    }
}
