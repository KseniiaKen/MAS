using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent.Agents
{
    class Adult : Person
    {
        public Adult(int Id, CoreAMS.Enums.HealthState healthState, string locationProbabilitiesFile) : base(Id, healthState) 
        {

        }
    }
}
