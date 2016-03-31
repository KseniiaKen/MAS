using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent.Agents
{
    class Youngster : Person
    {
        public Youngster(int Id, CoreAMS.Enums.HealthState healthState, string locationProbabilitiesDir)
            : base(Id, healthState, locationProbabilitiesDir + "/Youngster.csv") 
        {
        }

        public static List<Youngster> YoungsterList(CoreAMS.Enums.HealthState healthState, int count, string locationProbabilitiesFile)
        {
            List<Youngster> youngsters = new List<Youngster>();

            for (int i = 0; i < count; ++i)
                youngsters.Add(new Youngster(GlobalAgentDescriptorTable.GetNewId, healthState, locationProbabilitiesFile));

            return youngsters;
        }
    }
}
