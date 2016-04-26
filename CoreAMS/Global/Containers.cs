using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreAMS.Global
{
    public class Containers :  Dictionary<int, ContainersCore>     //ICollection<ContainersCore> // ICollection — общий интерфейс для всех коллекций
    {
        private Containers() {
        }

        private static Containers instance = new Containers();

        public static Containers Instance {
            get { return instance; }
        }
    }
}
