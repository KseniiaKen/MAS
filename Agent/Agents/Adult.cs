using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent.Agents
{
    class Adult : Person
    {
        public Adult(int Id, CoreAMS.Enums.HealthState healthState, string locationProbabilitiesDir)
            : base(Id, healthState, locationProbabilitiesDir + "/Adult.csv") 
        {
        }

        public static List<Adult> AdultList(CoreAMS.Enums.HealthState healthState, int count, string locationProbabilitiesFile)
        {
            List<Adult> adults = new List<Adult>();

            for (int i = 0; i < count; ++i)
                adults.Add(new Adult(GlobalAgentDescriptorTable.GetNewId, healthState, locationProbabilitiesFile));

            return adults;
        }
    }
}
