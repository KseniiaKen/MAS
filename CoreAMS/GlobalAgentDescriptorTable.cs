using CoreAMS.AgentCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS.AgentManagementSystem
{
    // здесь хранятся все агенты и их состояния.
    public static class GlobalAgentDescriptorTable
    {
        public static void deleteAllAgents() { agentDictionary.Clear(); }//метод, удаляющий всех агентов

        private static Dictionary<int, IAgent> agentDictionary = new Dictionary<int, IAgent>(); // хранятся список агентов и их ID
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

        public static void AddOneAgent(IAgent agent)//добавляем одного агента в общий каталог
        {
            agentDictionary.Add(agent.GetId(), agent);
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

        // Получаем агента из той же локации для заражения
        public static IAgent SameLocationAgent(int agentID)
        {
            IAgent agentInCurrentContainer = agentDictionary[agentID];
            if (agentInCurrentContainer is AbstractPerson)
            {
                AbstractPerson abstractPerson = (AbstractPerson)agentInCurrentContainer;
                ContainersCore currentContainer = abstractPerson.currentContainer; //текущий контейнер известен. Каждый контейнер хранит список агентов, находящихся в нём.
                //TODO: Если кроме текущего агента в списке кто-то есть, то берём первого из них и его и возвращаем.
                // Если никого нет, возвращаем null.
                if (currentContainer.abstractPersonsInCurrentContainer.Count > 1)
                {
                    if (currentContainer.abstractPersonsInCurrentContainer.First() != abstractPerson) //сравниваем с текущим агентом (с abstractPerson)
                    {
                        return currentContainer.abstractPersonsInCurrentContainer.First();
                    }
                    else { return currentContainer.abstractPersonsInCurrentContainer.ElementAt(1); }
                }
                else { return null; }
            }
            else { return null; }
        }
    }

    // Там, где был вызов метода GetRandomAgentExceptSenderAgentId, заменить на этот метод и учесть, что может вернуться null,
    // т.е. агент не найден.
}
