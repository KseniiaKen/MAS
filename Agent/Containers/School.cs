using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;

namespace Agent.Containers
{
    public class School : ContainersCore
    {
        public School(double area, double dencity) : base(Enums.ContainerType.School, area, dencity) 
        {
 
        }
    }
}
