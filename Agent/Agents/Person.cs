using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;
using CoreAMS.AgentCore;
using CoreAMS.MessageTransportSystem;
using CoreAMS.AgentManagementSystem;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace Agent.Agents
{
    public class Person : AbstractPerson
    {
        public struct LocationProbabilitiesKey // для описания ключа, в котором содержится информация о том, какой контейнер и какое время. 
        //Если бы это был класс, то не работали бы сравнения ключей в Dictionary; они бы были ссылками на разные места в куче.
        {
            public int startTime;
            public int endTime;
            public Enums.ContainerType containerType; // какой-то контейнер, для которого определяем вероятность.
        }

        public Dictionary<LocationProbabilitiesKey, double> locationProbabilities = new Dictionary<LocationProbabilitiesKey, double>();

        private enum DeathMode
        {
            Immediate,
            AlmostImmediate,
            Long
        }

        //private const DeathMode immediateDeath = DeathMode.Immediate;
        //private const double FUNERAL_PROBABILITY = 0.015; //вероятность смерти
        //private const double DEATH_PROBABILITY = 0.99; //вероятность быть погребённым
        //private const double INFECTION_PROBABILITY = 0.01; // вероятность заразиться при встрече с больным агентом

        private const DeathMode deathMode = DeathMode.AlmostImmediate;
        private const double FUNERAL_PROBABILITY = 0.008; //вероятность смерти
        private const double DEATH_PROBABILITY = 0.99; //вероятность быть погребённым
        private const double INFECTION_PROBABILITY = 0.0099; // вероятность заразиться при встрече с больным агентом

        //private const DeathMode immediateDeath = DeathMode.Long;
        //private const double FUNERAL_PROBABILITY = 0.9; //вероятность смерти
        //private const double DEATH_PROBABILITY = 0.99; //вероятность быть погребённым
        //private const double INFECTION_PROBABILITY = 0.013; // вероятность заразиться при встрече с больным агентом

        private CoreAMS.Enums.HealthState healthState; // состояние здоровья агента
        private int changeTime;                        // время, когда агент должен перейти из одного состояния в другое
        private bool isBeingInfected = false;          // true, если пошёл процесс заражения; после того, как заразился, true заменяетс на false.
      
        // Время протекания каждой стадии и время заражения других людей
        private int exposedTime = 10 * Enums.HoursDay;
        private int infectiousTime = 10 * Enums.HoursDay;
        private int recoveringTime = 10 * Enums.HoursDay;
        private int recoveredTime = 9000 * Enums.HoursWeek;
        private int funeralTime = (int)(4.5 * (float)Enums.HoursDay);

        private int infectedOtherAgentTime = 1; // частота заражения других людей (например, заражать кого-либо каждый час)

        // создаётся агент, которому задаются ID, состояние здоровья
        public Person(int Id, CoreAMS.Enums.HealthState healthState, string locationProbabilitiesFile)
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

            string[] rowValues = null;
            string[] rows = File.ReadAllLines(locationProbabilitiesFile);
            for (int i = 0; i < rows.Length; i++) 
            {
                if (!String.IsNullOrEmpty(rows[i]))
                {
                    rowValues = rows[i].Split(',');
                    LocationProbabilitiesKey key = new LocationProbabilitiesKey()
                    {
                        startTime = Int32.Parse(rowValues[0]),
                        endTime = Int32.Parse(rowValues[1]),
                        containerType = (Enums.ContainerType)Int32.Parse(rowValues[2])
                    };
                    this.locationProbabilities.Add(key, (double)Decimal.Parse(rowValues[3], CultureInfo.InvariantCulture));
                }
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
            // выбираем случайного агента и отправляем ему сообщение, что он инфицирован
            if (GlobalAgentDescriptorTable.random.NextDouble() <= INFECTION_PROBABILITY)
            {
                // MessageTransfer.Instance.AddInfect(new AgentMessage(Enums.HealthState.Infectious.ToString(), -1, Id));
                AbstractPerson personToInfect = GlobalAgentDescriptorTable.SameLocationPerson(this.Id);
                if (personToInfect != null)
                {
                    personToInfect.EventMessage(new AgentMessage(Enums.MessageType.Infected.ToString(), personToInfect.GetId(), this.Id));
                }
            }
        }

        // Получение сообщения от другого агента
        public override void EventMessage(AgentMessage message)
        {
            if (message.message == Enums.MessageType.Infected.ToString() &&
                healthState == Enums.HealthState.Susceptible)
            {
                isBeingInfected = true;
            }
        }

        // в каком контейнере должен быть агент в данный момент времени
        private Enums.ContainerType containerToGo() {
            var matchingRules = locationProbabilities.Where(p => p.Key.startTime <= GlobalTime.realTime && p.Key.endTime > GlobalTime.realTime);
            double probabilitiesSum = matchingRules.Select(p => p.Value).Sum();
            double scale = 1.0d / probabilitiesSum;
            double picked = GlobalAgentDescriptorTable.random.NextDouble();
            foreach(var kvp in matchingRules)
            {
                if (picked < kvp.Value * scale)
                {
                    return kvp.Key.containerType;
                }
                else
                {
                    picked -= kvp.Value * scale;
                }
            }

            throw new InvalidDataException("Something wrong wit probablities");
        }

        public override void Move()
        {
            if (this.healthState == Enums.HealthState.Dead)
                return;

            Enums.ContainerType resOfContainerToGo = containerToGo();
            if (this.currentContainerType != resOfContainerToGo)
            {
                // Trace.TraceInformation("Moved to {0}", resOfContainerToGo);
                MessageTransfer.Instance.AddToGoto(this, resOfContainerToGo);
                currentContainerType = resOfContainerToGo;
            }
        }

        // Запуск агента
        public override void Run()
        {
            switch (healthState)
            {
                case Enums.HealthState.Susceptible:
                    // Агент заражается
                    if (isBeingInfected)
                    {
                        SetHealthState(Enums.HealthState.Exposed);
                        changeTime = GlobalTime.Time + exposedTime;
                        isBeingInfected = false;
                    }
                    break;
                case Enums.HealthState.Exposed:
                    // Когда наступает время, агент переходит в состояние Infectious
                    if (GlobalTime.Time == changeTime)
                    {
                        SetHealthState(Enums.HealthState.Infectious);
                        changeTime = GlobalTime.Time + infectiousTime;
                    }
                    break;
                case Enums.HealthState.Infectious:
                    // Заражаем кого-либо каждые ? часов
                    if ((changeTime - GlobalTime.Time) % infectedOtherAgentTime == 0)
                        SendMessage();

                    switch(deathMode)
                    {
                        case DeathMode.Immediate:
                            // Когда наступает время, агент переходит в состояние Recovered
                            //может умереть, а не выздороветь:
                            if (GlobalTime.Time == changeTime)
                            {
                                if (GlobalAgentDescriptorTable.random.NextDouble() >= FUNERAL_PROBABILITY)  //r.Next(0, 99); — получить следующее случайное число
                                {

                                    SetHealthState(Enums.HealthState.Recovering);
                                    changeTime = GlobalTime.Time + recoveringTime;
                                }
                                else
                                {
                                    SetHealthState(Enums.HealthState.Funeral);
                                    changeTime = GlobalTime.Time + funeralTime;
                                }
                            }
                            break;
                        case DeathMode.AlmostImmediate:
                            if ((changeTime - GlobalTime.Time) / 24 <= 2)
                            {
                                if (GlobalAgentDescriptorTable.random.NextDouble() < FUNERAL_PROBABILITY)
                                {
                                    SetHealthState(Enums.HealthState.Funeral);
                                    changeTime = GlobalTime.Time + funeralTime;
                                    break;
                                }
                            }
                            if (GlobalTime.Time == changeTime)
                            {
                                SetHealthState(Enums.HealthState.Recovering);
                                changeTime = GlobalTime.Time + recoveringTime;
                            }
                            break;
                        case DeathMode.Long:
                            // Когда наступает время, агент переходит в состояние Recovered
                            //может умереть, а не выздороветь:
                            if (GlobalTime.realTime == 0 && GlobalAgentDescriptorTable.random.NextDouble() < FUNERAL_PROBABILITY * (infectiousTime - (changeTime - GlobalTime.Time)) / 24)  //r.Next(0, 99); — получить следующее случайное число
                            {
                                SetHealthState(Enums.HealthState.Funeral);
                                changeTime = GlobalTime.Time + funeralTime;

                            }
                            else
                            {
                                if (GlobalTime.Time == changeTime)
                                {
                                    SetHealthState(Enums.HealthState.Recovering);
                                    changeTime = GlobalTime.Time + recoveringTime;
                                }
                            }
                            break;
                    }
                    break;
                case Enums.HealthState.Recovering:
                    // Заражаем кого-либо каждые ? часов
                    if ((changeTime - GlobalTime.Time) % infectedOtherAgentTime == 0)
                        SendMessage();

                    if (GlobalTime.Time == changeTime)
                    {
                        SetHealthState(Enums.HealthState.Recovered);
                        changeTime = GlobalTime.Time + recoveredTime;
                    }
                    break;
                case Enums.HealthState.Funeral:
                    if ((changeTime - GlobalTime.Time) % infectedOtherAgentTime == 0)
                        SendMessage();

                    if (GlobalTime.Time == changeTime)
                    {
                        if (GlobalAgentDescriptorTable.random.NextDouble() < DEATH_PROBABILITY)
                        {
                            SetHealthState(Enums.HealthState.Dead);
                        }
                        else
                        {
                            changeTime = GlobalTime.Time - 1;
                        }
                    }
                    break;

                /*case Enums.HealthState.Recovered:
                    // Когда наступает время, агент переходит в состояние Susceptible
                    if (GlobalTime.Time == changeTime)
                    {
                        SetHealthState(Enums.HealthState.Susceptible);
                    }
                    break;*/
            }

        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }

        // Вспомогательная функция для создания агентов определенного состояния. К какому-либо агенту не имеет отношения.
/*      public static List<Person> PersonList(CoreAMS.Enums.HealthState healthState, int count, string locationProbabilitiesFile)
        {
            List<Person> persons = new List<Person>();

            for (int i = 0; i < count; ++i)
                persons.Add(new Person(GlobalAgentDescriptorTable.GetNewId, healthState, locationProbabilitiesFile));

            return persons;
        }*/

    }
}
