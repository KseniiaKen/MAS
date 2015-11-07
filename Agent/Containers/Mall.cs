using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;

namespace Agent.Containers
{
    public class Mall : ContainersCore
    {
        public Mall(double area, double dencity) : base(Enums.ContainerType.Mall, area, dencity) 
        {
        
        }
    }
}
