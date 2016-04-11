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

namespace Agent.Agents
{
    public class Person : AbstractPerson
    {
        public struct LocationProbabilitiesKey // для описания ключа, в котором содержится информация о том, какой контейнер и какое время. 
        //Если бы это был класс, то не работали бы сравнения ключей в Dictionary; они бы были ссылками на разные места в куче.
        {
            public int startTime;
            public int endTime;
            public ContainersCore container; // какой-то контейнер, для которого определяем вероятность.
        }

        public Dictionary<LocationProbabilitiesKey, double> locationProbabilities = new Dictionary<LocationProbabilitiesKey, double>();

        private const double FUNERAL_PROBABILITY = 0.75; //вероятность смерти
        private const double DEATH_PROBABILITY = 0.99; //вероятность быть погребённым
        private const double INFECTION_PROBABILITY = 0.01; // вероятность заразиться при встрече с больным агентом
        private static Random r = new Random();   //генератор случайных чисел
        private CoreAMS.Enums.HealthState healthState; // состояние здоровья агента
        private int changeTime;                        // время, когда агент должен перейти из одного состояния в другое
        private bool isBeingInfected = false;          // true, если пошёл процесс заражения; после того, как заразился, true заменяетс на false.
      
        // Время протекания каждой стадии и время заражения других людей
        private int exposedTime = 10 * Enums.HoursDay;
        private int infectiousTime = 10 * Enums.HoursDay;
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
                        container = CoreAMS.Global.Containers.Instance.ElementAt(Int32.Parse(rowValues[2]))
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
            if (r.Next(0, 99) <= 100 * INFECTION_PROBABILITY)
            {
                MessageTransfer.MessageAgentToRandomAgent(new AgentMessage(Enums.HealthState.Infectious.ToString(), -1, Id));
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
        private ContainersCore containerToGo() {
            while (true)
            {
                for (int i = 0; i < locationProbabilities.Count; i++)
                {
                    var keyAndValue = locationProbabilities.ElementAt(i);
                    if (keyAndValue.Key.startTime <= GlobalTime.realTime && keyAndValue.Key.endTime > GlobalTime.realTime)
                    {
                        if (r.NextDouble() < keyAndValue.Value) { return keyAndValue.Key.container; }
                    }

                }
            }
 /*           int[] a = new int[5]{1,2,3,4,5};
            int x = 0;
            for (int i=0; i < a.Length; i++) {
                if (a[i] > 2) { x = a[i]; break; }
                
            }*/

        }
 


        // Запуск агента
        public override void Run()
        {
            ContainersCore resOfContainerToGo = containerToGo();
            if (currentContainer != resOfContainerToGo) {
                if (currentContainer != null) {
                    currentContainer.DeletePersonFromContainer(this);
                }
                resOfContainerToGo.AddPersonInContainer(this);
                currentContainer = resOfContainerToGo;
            }

            switch(healthState)
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

                    // Когда наступает время, агент переходит в состояние Recovered
                    if (GlobalTime.Time == changeTime)
                    {   
                        //может умереть, а не выздороветь:
                        if (r.Next(0, 99) >= 100 * FUNERAL_PROBABILITY)  //r.Next(0, 99); — получить следующее случайное число
                        {
                            SetHealthState(Enums.HealthState.Recovered);
                            changeTime = GlobalTime.Time + recoveredTime;
                        }
                        else
                        {
                            SetHealthState(Enums.HealthState.Funeral);
                            changeTime = GlobalTime.Time + funeralTime;
                        }
                        
                    }
                    break;
                case Enums.HealthState.Funeral:
                    if (GlobalTime.Time == changeTime)
                    {
                        int n = r.Next(0, 99);
                        if (n <= 100 * DEATH_PROBABILITY)
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
