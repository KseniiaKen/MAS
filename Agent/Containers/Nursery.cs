using CoreAMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent.Containers
{
    public class Nursery : ContainersCore
    {
        public Nursery(double area, double dencity) : base(Enums.ContainerType.Nursery, area, dencity) { }
    }
}
