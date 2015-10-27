using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS.MessageTransportSystem;

namespace CoreAMS.AgentCore
{
    public interface IAgent
    {
        int GetId();
        Enums.AgentState GetState();
        void SendMessage();
        void EventMessage(AgentMessage message);
        void Run();
        void Stop();
    }

    public abstract class AbstractPerson : IAgent
    {
        protected Queue<AgentMessage> messageQueue = new Queue<AgentMessage>();
        protected Enums.AgentState state;
        protected Dictionary<string, Object> variableDictionary = new Dictionary<string,object>();
        protected int Id = -1;

        public abstract int GetId();

        public abstract Enums.AgentState GetState();

        public abstract Enums.HealthState GetHealthState();

        protected abstract void SetState(Enums.AgentState state);

        public abstract void SendMessage();

        public abstract void EventMessage(AgentMessage message);

        public abstract void Run();

        public abstract void Stop();
    }
}
