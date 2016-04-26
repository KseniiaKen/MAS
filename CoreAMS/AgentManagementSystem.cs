using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreAMS.AgentCore;
using System.ComponentModel;
using CoreAMS.Global;
using System.Diagnostics;

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

        public static int totalAgents;
        public static int totalContainers;
        public static int totalWorkers;

        public static AutoResetEvent NextTimeEvent = new AutoResetEvent(false);

        // Запуск всех агентов
        public static void RunAgents()
        {
            int avgNumberOfAgents = totalAgents / totalWorkers;

            while (true)
            {
                NextTimeEvent.WaitOne();

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

                if (agents.Count - avgNumberOfAgents > 100)
                {
                    Trace.TraceInformation("Too many agents: {0}. Average: {1}", agents.Count, avgNumberOfAgents);
                }

                // запускаем каждый агент (агенты меняют свои состояния)
                for (int i = 0; i < agents.Count; ++i)
                {
                    if (GlobalTime.Time % 24 == 0)
                    {
                        agents[i].Move();
                    }
                    agents[i].Run();
                }

                RefreshAgentsStateCount();

                // после того как все агенты за этот час изменили свои состояния, мы увеличиваем время
                GlobalTime.Time += 1;

                if (GlobalTime.Time % 24 == 0)
                {
                    //Trace.TraceInformation("New day: {0}", GlobalTime.Day);
                    //Trace.TraceInformation("Susceptible: {0}\nRecovered: {3}\nInfectious: {4}\nFuneral: {1}\nDead: {2}", AgentManagementSystem.susceptibleAgentsCount, AgentManagementSystem.funeralAgentsCount,
                    //        AgentManagementSystem.deadAgentsCount, AgentManagementSystem.recoveredAgentsCount, AgentManagementSystem.infectiousAgentsCount);
                    //Trace.TraceInformation("Agents count: {0}", agents.Count);
                    //foreach(var g in GlobalAgentDescriptorTable.GetAgents().Select(a => (AbstractPerson)a).GroupBy(p => p.currentContainerId))
                    //{
                    //    var fst = g.First();
                    //    Trace.TraceInformation("{0} ({1}) : {2}", fst.currentContainerId, fst.currentContainerType, g.Count());
                    //}
                    Trace.TraceInformation("{5}: {0} {3} {4} {1} {2}", AgentManagementSystem.susceptibleAgentsCount, AgentManagementSystem.funeralAgentsCount,
        AgentManagementSystem.deadAgentsCount, AgentManagementSystem.recoveredAgentsCount, AgentManagementSystem.infectiousAgentsCount, GlobalTime.Day);

                }

                //MessageTransportSystem.MessageTransfer.SendTickEnd();
                MessageTransportSystem.MessageTransfer.Instance.SendGoto();

                // if ((GlobalTime.Time > 1000 && exposedAgentsCount == 0 && infectiousAgentsCount == 0) || GlobalTime.Day >= 80) 
                if (GlobalTime.Day >= 80)
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
