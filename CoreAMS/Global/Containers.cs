using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS.Global
{
    public class Containers :  List<ContainersCore>     //ICollection<ContainersCore> // ICollection — общий интерфейс для всех коллекций
    {
        //TODO: сделать этот класс Синглтоном (будет одним глобалным объектом)
        private Containers() {   //сделали конструктор private, чтобы нельзя было создавать экземпляры класса.

        }

        private static Containers instance = new Containers();

        public static Containers Instance {
            get { return instance; }
        } //TODO: заполнить коллекцию, где заполняются агенты - в Form1. Посмотреть, какие методы есть у List<ContainersCore>
    }
}
