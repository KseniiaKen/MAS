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

        public enum ContainerType 
        {
            Home,
            Hospital,
            Mall,
            Office,
            University,
            School,
            Nursery

        }

        public static int HoursDay = 24;
        public static int HoursWeek = HoursDay * 7;
    }
}
