using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS
{
    // Здесь хранятся все константные переменные.
    public class Enums
    {
        public enum AgentState
        {
            Stop = 0,
            Run = 1
        }

        // Перечисление всех состояний здоровья агента
        [Serializable]
        public enum HealthState
        {
            Susceptible,
            Exposed,
            Infectious,
            Funeral,
            Dead,
            Recovered

        }

        // Типы сообщений, которые могут передаваться между агентами
        public enum MessageType
        {
            Infected = 0
        }

        [Serializable]
        public enum ContainerType 
        {
            Home = 0,
            Hospital = 1,
            Mall = 2,
            Office = 3,
            University = 4,
            School = 5,
            Nursery = 6

        }

        public static int HoursDay = 24;
        public static int HoursWeek = HoursDay * 7;
    }
}
