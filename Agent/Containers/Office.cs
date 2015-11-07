using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;

namespace Agent.Containers
{
    public class Office : ContainersCore
    {
        public Office(double area, double dencity) : base(Enums.ContainerType.Office, area, dencity) 
        {
        
        }
    }
}
