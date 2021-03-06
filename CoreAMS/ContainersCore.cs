﻿using CoreAMS.AgentCore;
using CoreAMS.AgentManagementSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS
{
    public abstract class ContainersCore
    {
        //заполнение параметров контейнера
        protected ContainersCore(Enums.ContainerType containerType, int id, double area, double dencity) {
            this.containerType = containerType;
            this.area = area;
            this.dencity = dencity;
            this.id = id;
        }

        protected int id;
        public int Id
        {
            get { return this.id; }
        }

        protected Enums.ContainerType containerType;
        public Enums.ContainerType ContainerType {
            get { return this.containerType; }
        }

        private double area;
        public double Area {
            get { return this.area; }        
        }

        private double dencity;
        public double Dencity {
            get { return this.dencity; }
        }

        // список абстрактных person-ов, находящихся в контейнере в данный момент времени
        private List<int> agentIdsInCurrentContainer = new List<int>(); 

        //метод, добавляющий persona в контейнер
        public void AddPersonInContainer(int agentId) {
            agentIdsInCurrentContainer.Add(agentId);
        }

        //метод, убирающий persona из контейнера
        public void DeletePersonFromContainer(int agentId) {
            agentIdsInCurrentContainer.Remove(agentId);
        }

        public int GetRandomAgent()
        {
            //Trace.TraceInformation("Agents in container: {0}", agentIdsInCurrentContainer.Count);
            int idx = GlobalAgentDescriptorTable.random.Next(0, this.agentIdsInCurrentContainer.Count - 1);
            return agentIdsInCurrentContainer[idx];
        }

        public int AgentCount
        {
            get {
                return this.agentIdsInCurrentContainer.Count;
            }
        }
    }

}
