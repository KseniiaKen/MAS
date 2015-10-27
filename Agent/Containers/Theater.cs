using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;

namespace Agent.Containers
{
    public class Theater : ContainersCore
    {
        public Theater(double area, double dencity) : base(Enums.ContainerType.Theater, area, dencity)
        { 
            
        }
    }
}
