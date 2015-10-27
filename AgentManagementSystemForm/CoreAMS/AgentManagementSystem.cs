using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreAMS.AgentCore;

namespace CoreAMS.AgentManagementSystem
{
    // Класс для управления временем системы.
    public static class GlobalTime
    {
        private static int time = 0;
        private static int day = Enums.HoursDay;
        private static int delay = 10;

        // Количество часов с момента запуска системы
        public static int Time
        {
            get { return time; }
            set { time = value; }
        }

        // Количество дней с момента запуска системы
        public static int Day
        {
            get { return time / day; }
        }

        // Возвращает true если настал новый день
        public static bool isNextDay
        {
            get { return (time + 1) % Enums.HoursDay == 0; }
        }
    }

    // Класс, в котором хранятся все агенты и их состояния.
    public static class GlobalAgentDescriptorTable
    {
        
        private static Dictionary<int, IAgent> agentDictionary = new Dictionary<int,IAgent>(); // хранятся список агентов и их ID
        private static int key = 0;
        private static object threadLock = new object();


        public static int GetNewId
        {
            get
            {
                lock (threadLock) key++;
                return key;
            }
        }

        // Добавляем агентов в общий каталог
        public static void AddAgents(List<IAgent> agents)
        {
            foreach (var agent in agents)
            {
                agentDictionary.Add(agent.GetId(), agent);
            }
        }

        public static IAgent GetAgentById(int agentId)
        {
            return agentDictionary[agentId];
        }

        // Возвращаем список всех агентов
        public static List<IAgent> GetAgents()
        {
            return agentDictionary.Values.ToList();
        }

        public static int Count { get { return agentDictionary.Count; } }

        // Получаем случайного агента для заражения
        public static IAgent GetRandomAgentExceptSenderAgentId(int agentId)
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            int randomId = agentId;
            while (randomId == agentId) randomId = rand.Next(key) + 1;
            return agentDictionary[randomId];
        }
    }

    // Менеджер агентов, для запуска всех агентов.
    public static class AgentManagementSystem
    {
        // Количество агентов каждого состояния
        public static int susceptibleAgentsCount;
        public static int exposedAgentsCount;
        public static int infectiousAgentsCount;
        public static int recoveredAgentsCount;

        // Запуск всех агентов
        public static void RunAgents()
        {
            while (true)
            {
                // получаем список всех существующих агентов
                var agents = GlobalAgentDescriptorTable.GetAgents();

                // запускаем каждый агент (агенты меняют свои состояния)
                for (int i = 0; i < agents.Count; ++i)
                {
                    agents[i].Run();
                }

                RefreshAgentsStateCount();

                // после того как все агенты за этот час изменили свои состояния, мы увеличиваем время
                GlobalTime.Time += 1;
            }
        }

        // Обновляем количество агентов каждого состояния
        public static void RefreshAgentsStateCount()
        {
            // каждый день обновляем количество агентов каждого состояния
            if (GlobalTime.isNextDay)
            {
                // получаем список всех существующих агентов
                var agents = GlobalAgentDescriptorTable.GetAgents();

                susceptibleAgentsCount = agents.Count(a => ((AgentCore.AbstractPerson)a).GetHealthState() == Enums.HealthState.Susceptible);
                exposedAgentsCount = agents.Count(a => ((AgentCore.AbstractPerson)a).GetHealthState() == Enums.HealthState.Exposed);
                infectiousAgentsCount = agents.Count(a => ((AgentCore.AbstractPerson)a).GetHealthState() == Enums.HealthState.Infectious);
                recoveredAgentsCount = agents.Count(a => ((AgentCore.AbstractPerson)a).GetHealthState() == Enums.HealthState.Recovered);
            }
        }
    }
}
