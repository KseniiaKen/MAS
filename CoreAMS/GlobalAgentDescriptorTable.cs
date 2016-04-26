using CoreAMS.AgentCore;
using CoreAMS.Global;
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
        private static Dictionary<int, IAgent> agentDictionary = new Dictionary<int, IAgent>(); // хранятся список агентов и их ID
        private static int key = 0;
        private static object threadLock = new object();
        private static Random random = new Random();


        public static int GetNewId
        {
            get
            {
                lock (threadLock) key++;
                return key;
            }
            set
            {
                lock (threadLock)
                {
                    key = value - 1;
                }
            }
        }

        // Добавляем агентов в общий каталог
        public static void AddAgents(List<IAgent> agents)
        {
            lock (agentDictionary)
            {
                foreach (var agent in agents)
                {
                    agentDictionary.Add(agent.GetId(), agent);
                }
            }
        }

        public static void AddOneAgent(IAgent agent)//добавляем одного агента в общий каталог
        {
            lock (agentDictionary)
            {
                agentDictionary.Add(agent.GetId(), agent);
            }
        }

        //метод, удаляющий всех агентов
        public static void DeleteAllAgents()
        {
            lock (agentDictionary)
            {
                agentDictionary.Clear();
            }
        }

        public static void DeleteOneAgent(IAgent agent)
        {
            lock (agentDictionary)
            {
                var found = agentDictionary.Where(kvp => kvp.Value == agent).ToList();
                foreach (var kvp in found)
                {
                    agentDictionary.Remove(kvp.Key);
                }
            }
        }

        public static IAgent GetAgentById(int agentId)
        {
            lock (agentDictionary)
            {
                return agentDictionary[agentId];
            }
        }

        // Возвращаем список всех агентов
        public static List<IAgent> GetAgents()
        {
            lock (agentDictionary)
            {
                return agentDictionary.Values.ToList();
            }
        }

        public static int Count
        {
            get
            {
                lock (agentDictionary)
                {
                    return agentDictionary.Count;
                }
            }
        }


        // Получаем случайного агента для заражения
        //public static IAgent GetRandomAgentExceptSenderAgentId(int agentId)
        //{
        //    Random rand = new Random(Guid.NewGuid().GetHashCode());
        //    int randomId = agentId;
        //    while (randomId == agentId) randomId = rand.Next(key) + 1;
        //    return agentDictionary[randomId];
        //}

        // Получаем агента из той же локации для заражения
        public static AbstractPerson SameLocationPerson(int agentID)
        {

            IAgent agentInCurrentContainer = agentDictionary[agentID];
            if (agentInCurrentContainer is AbstractPerson)
            {
                AbstractPerson abstractPerson = (AbstractPerson)agentInCurrentContainer;
                ContainersCore currentContainer = Containers.Instance.Find(c => c.Id == abstractPerson.currentContainerId); //текущий контейнер известен.

                lock (agentDictionary)
                {

                    var abstractPersonsInCurrentContainer =
                    agentDictionary.Values
                    .Where(a => a is AbstractPerson)
                    .Select(a => (AbstractPerson)a)
                    .Where(a => a.currentContainerId == abstractPerson.currentContainerId && a != abstractPerson)
                    .ToList();

                    // Если кроме текущего агента в списке кто-то есть, то берём случайного из них и его и возвращаем.
                    // Если никого нет, возвращаем null.
                    if (abstractPersonsInCurrentContainer.Count > 0)
                    {
                        AbstractPerson personToInfect = abstractPersonsInCurrentContainer[random.Next(0, abstractPersonsInCurrentContainer.Count - 1)];

                        return personToInfect;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }
            
        }
    }

    // Там, где был вызов метода GetRandomAgentExceptSenderAgentId, заменить на этот метод и учесть, что может вернуться null,
    // т.е. агент не найден.
}
