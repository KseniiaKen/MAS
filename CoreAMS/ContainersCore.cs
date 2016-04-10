using CoreAMS.AgentCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS
{
    public abstract class ContainersCore
    {
        //заполнение параметров контейнера
        protected ContainersCore(Enums.ContainerType containerType, double area, double dencity) {
            this.containerType = containerType;
            this.area = area;
            this.dencity = dencity;
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
        public List<AbstractPerson> abstractPersonsInCurrentContainer = new List<AbstractPerson>(); 

        //метод, добавляющий persona в контейнер
        public void AddPersonInContainer(AbstractPerson person) {
            abstractPersonsInCurrentContainer.Add(person);
        }

        //метод, убирающий persona из контейнера
        public void DeletePersonFromContainer(AbstractPerson person) {
            abstractPersonsInCurrentContainer.Remove(person);
        }
    }

}
