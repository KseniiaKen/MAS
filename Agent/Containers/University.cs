﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;

namespace Agent.Containers
{
    public class University : ContainersCore
    {
        public University(double area, double dencity)
            : base(Enums.ContainerType.University, area, dencity)
        { 
        
        }
    }
}