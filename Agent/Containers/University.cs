using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;

namespace Agent.Containers
{
    public class University : ContainersCore
    {
        public University(int id, double area, double dencity)
            : base(Enums.ContainerType.University, id, area, dencity)
        { 
        
        }
    }
}
