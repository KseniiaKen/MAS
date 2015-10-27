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
        public enum HealthState
        {
            Susceptible = 0,
            Exposed = 1,
            Infectious = 2,
            Recovered = 3
        }

        // Типы сообщений, которые могут передаваться между агентами
        public enum MessageType
        {
            Infected = 0
        }

        public enum ContainerType 
        {
            Theater,
            Home,
            Hospital,
            Mall,
            Office

        }

        public static int HoursDay = 24;
        public static int HoursWeek = HoursDay * 7;
    }
}
