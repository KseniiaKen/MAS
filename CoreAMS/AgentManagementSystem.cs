using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreAMS.AgentCore;
using System.ComponentModel;
using CoreAMS.Global;

namespace CoreAMS.AgentManagementSystem
{
    // Менеджер агентов, для запуска всех агентов.
    public static class AgentManagementSystem
    {
        // Количество агентов каждого состояния
        public static int susceptibleAgentsCount;
        public static int exposedAgentsCount;
        public static int infectiousAgentsCount;
        public static int recoveredAgentsCount;
        public static int funeralAgentsCount;
        public static int deadAgentsCount;

        // Запуск всех агентов
        public static void RunAgents()
        {
            while (true)
            {
                // получаем список всех существующих агентов
                var agents = GlobalAgentDescriptorTable.GetAgents();
               /* foreach (ContainersCore c in Containers.Instance) {
                    if (c.abstractPersonsInCurrentContainer.Count > 0)
                    {
                        Console.WriteLine("{0} ({1})", c.ContainerType, GlobalTime.realTime);
                        foreach (AbstractPerson p in c.abstractPersonsInCurrentContainer)
                        {
                            Console.Write(p.GetId());
                            Console.Write(" ");
                        }
                        Console.WriteLine();
                    }
                }*/

                // запускаем каждый агент (агенты меняют свои состояния)
                for (int i = 0; i < agents.Count; ++i)
                {
                    agents[i].Run();
                }

                RefreshAgentsStateCount();

                // после того как все агенты за этот час изменили свои состояния, мы увеличиваем время
                GlobalTime.Time += 1;

                if (GlobalTime.Time > 1000 && exposedAgentsCount == 0 && infectiousAgentsCount == 0) 
                {
                    break;
                }
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
                funeralAgentsCount = agents.Count(a => ((AgentCore.AbstractPerson)a).GetHealthState() == Enums.HealthState.Funeral);
                deadAgentsCount = agents.Count(a => ((AgentCore.AbstractPerson)a).GetHealthState() == Enums.HealthState.Dead);
            }
        }
    }
}
