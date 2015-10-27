using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;
using CoreAMS.AgentCore;
using CoreAMS.MessageTransportSystem;
using CoreAMS.AgentManagementSystem;

namespace Agent
{
    public class Person : AbstractPerson
    {
        private CoreAMS.Enums.HealthState healthState; // состояние здоровья агента
        private int changeTime;                        // время, когда агент должен перейти из одного состояния в другое
        private bool isInfected = false;               // true, если агента инфицировали

        // Время протекания каждой стадии и время заражения других людей
        private int exposedTime = 2 * Enums.HoursDay;
        private int infectiousTime = 7 * Enums.HoursDay;
        private int recoveredTime = 4 * Enums.HoursWeek;
        private int infectedOtherAgentTime = 24; // частота чаражения других людей (например, заражать кого-либо каждые 10 часов)

        // Конструктор класса Person (создается агент, которому мы задаем ID, состояние здоровья)
        public Person(int Id, CoreAMS.Enums.HealthState healthState)
        {
            this.Id = Id;
            this.healthState = healthState;
            this.state = Enums.AgentState.Stop;

            // в зависимости от начального состояния агента мы устанавливаем время, когда агент 
            // перейдет в следующее состояние
            switch (healthState)
            {
                case Enums.HealthState.Infectious:
                    changeTime = infectiousTime;
                    break;
                case Enums.HealthState.Exposed:
                    changeTime = exposedTime;
                    break;
                case Enums.HealthState.Recovered:
                    changeTime = recoveredTime;
                    break;
            }
            
        }

        public override int GetId()
        {
            return Id;
        }

        public override Enums.AgentState GetState()
        {
            return state;
        }

        public override Enums.HealthState GetHealthState()
        {
            return healthState;
        }

        protected override void SetState(Enums.AgentState state)
        {
            this.state = state;
        }

        protected void SetHealthState(CoreAMS.Enums.HealthState healthState)
        {
            this.healthState = healthState;
        }

        // Отправка сообщения другому агенту
        public override void SendMessage()
        {
            // выбираем случайного агента и отправляем ему сообщение что он инфицирован
            MessageTransfer.MessageAgentToRandomAgent(
                new AgentMessage(Enums.HealthState.Infectious.ToString(), -1, Id));
        }

        // Получение сообщения от другого агента
        public override void EventMessage(AgentMessage message)
        {
            if (message.message == Enums.MessageType.Infected.ToString() &&
                healthState == Enums.HealthState.Susceptible)
            {
                isInfected = true;
            }
        }

        // Запуск агента
        public override void Run()
        {
            // Susceptible
            if (healthState == Enums.HealthState.Susceptible)
            {
                // Агент заражается
                if (isInfected)
                {
                    SetHealthState(Enums.HealthState.Exposed);
                    changeTime = GlobalTime.Time + exposedTime;
                    isInfected = false;
                }
                return;
            }

            // Exposed
            if (healthState == Enums.HealthState.Exposed)
            {
                // Когда наступает время, агент переходит в состояние Infectious
                if (GlobalTime.Time == changeTime)
                {
                    SetHealthState(Enums.HealthState.Infectious);
                    changeTime = GlobalTime.Time + infectiousTime;
                }
                return;
            }
            
            // Infectious
            if (healthState == Enums.HealthState.Infectious)
            {
                // Заражаем кого-либо каждые ? часов
                if ((changeTime - GlobalTime.Time) % infectedOtherAgentTime == 0)
                    SendMessage();

                // Когда наступает время, агент переходит в состояние Recovered
                if (GlobalTime.Time == changeTime)
                {
                    SetHealthState(Enums.HealthState.Recovered);
                    changeTime = GlobalTime.Time + recoveredTime;
                }
                return;
            }

            // Recovered
            if (healthState == Enums.HealthState.Recovered)
            {
                // Когда наступает время, агент переходит в состояние Susceptible
                if (GlobalTime.Time == changeTime)
                {
                    SetHealthState(Enums.HealthState.Susceptible);
                }
                return;
            }
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        // Вспомогательная функция для создания агентов определенного состояния. К какому-либо агенту не имеет отношения.
        public static List<Person> PersonList(CoreAMS.Enums.HealthState healthState, int count)
        {
            List<Person> persons = new List<Person>();

            for (int i = 0; i < count; ++i)
                persons.Add(new Person(GlobalAgentDescriptorTable.GetNewId, healthState));

            return persons;
        }
    }
}
