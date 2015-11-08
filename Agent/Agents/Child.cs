using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreAMS;
using Agent.Containers;
using CoreAMS.Global;
using System.IO;
using System.Globalization;

namespace Agent.Agents
{
    public class Child : Person //TODO: the same in Person.
    { 
        private struct LocationProbabilitiesKey // для описания ключа, в котором содержится информация о том, какой контейнер и какое время. 
            //Если бы это был класс, то не работали бы сравнения ключей в Dictionary; они бы были ссылками на разные места в куче.
        {
            public int startTime;
            public int endTime;
            public ContainersCore container; // какой-то контейнер, для которого определяем вероятность.
        }

        private Dictionary<LocationProbabilitiesKey, double> locationProbabilities = new Dictionary<LocationProbabilitiesKey, double>();

        public Child(int Id, CoreAMS.Enums.HealthState healthState, string locationProbabilitiesFile) : base(Id, healthState)
        {
            string[] rowValues = null;
            string[] rows = File.ReadAllLines(locationProbabilitiesFile);
            for (int i = 0; i < rows.Length; i++) 
            {
                if (!String.IsNullOrEmpty(rows[i]))
                {
                    rowValues = rows[i].Split(',');
                    LocationProbabilitiesKey key = new LocationProbabilitiesKey()
                    {
                        startTime = Int32.Parse(rowValues[0]),
                        endTime = Int32.Parse(rowValues[1]),
                        container = CoreAMS.Global.Containers.Instance.ElementAt(Int32.Parse(rowValues[2]))
                    };
                    this.locationProbabilities.Add(key, (double)Decimal.Parse(rowValues[3], CultureInfo.InvariantCulture));
                }
            }

            //int st = 10;
            //int et = 13;
            //Hospital h1 = new Hospital(0.6, 1000.0);

            //var key = new LocationProbabilitiesKey() {
            //    startTime = st,
            //    endTime = et,
            //    container = h1
            //};
            //double pr = locationProbabilities[key];
        }

    }
}
