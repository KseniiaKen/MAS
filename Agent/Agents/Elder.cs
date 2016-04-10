using CoreAMS.AgentManagementSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent.Agents
{
    public class Elder : Person
    {
        public Elder(int Id, CoreAMS.Enums.HealthState healthState, string locationProbabilitiesDir)
            : base(Id, healthState, locationProbabilitiesDir + "/Elder.csv") 
        {
        }

        public static List<Elder> ElderList(CoreAMS.Enums.HealthState healthState, int count, string locationProbabilitiesFile)
        {
            List<Elder> elders = new List<Elder>();

            for (int i = 0; i < count; ++i)
                elders.Add(new Elder(GlobalAgentDescriptorTable.GetNewId, healthState, locationProbabilitiesFile));

            return elders;
        }
    }
}
