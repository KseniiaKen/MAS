﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;

namespace Agent.Containers
{
    public class Hospital : ContainersCore
    {
        public Hospital(int id, double area, double dencity) : base(Enums.ContainerType.Hospital, id, area, dencity) 
        { 

        }
    }
}
