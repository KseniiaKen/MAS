using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;

namespace Agent.Containers
{
    public class Hospital : ContainersCore
    {
        public Hospital(double area, double dencity) : base(Enums.ContainerType.Hospital, area, dencity) 
        { 

        }
    }
}
