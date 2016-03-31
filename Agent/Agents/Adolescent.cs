using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent.Agents
{
    class Adolescent : Person
    {
        public Adolescent(int Id, CoreAMS.Enums.HealthState healthState, string locationProbabilitiesDir)
            : base(Id, healthState, locationProbabilitiesDir + "/Adolescent.csv")
        {
        }

        public static List<Adolescent> AdolescentList(CoreAMS.Enums.HealthState healthState, int count, string locationProbabilitiesFile)
        {
            List<Adolescent> adolescents = new List<Adolescent>();

            for (int i = 0; i < count; ++i)
                adolescents.Add(new Adolescent(GlobalAgentDescriptorTable.GetNewId, healthState, locationProbabilitiesFile));

            return adolescents;
        }
    }
}
