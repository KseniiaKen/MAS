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
    }
}
