using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;

namespace Agent.Containers
{
    public class Home : ContainersCore
    {
        public Home(int id, double area, double dencity) : base(Enums.ContainerType.Home, id, area, dencity) 
        { 
        
        }
    }
}
