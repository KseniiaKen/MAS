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
using CoreAMS.AgentManagementSystem;

namespace Agent.Agents
{
    public class Child : Person 
    {
        public Child(int Id, CoreAMS.Enums.HealthState healthState, string locationProbabilitiesDir)
            : base(Id, healthState, locationProbabilitiesDir + "/LPChild.csv")
        {
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

        public static List<Child> ChildList(CoreAMS.Enums.HealthState healthState, int count, string locationProbabilitiesFile)
        {
            List<Child> childrens = new List<Child>();

            for (int i = 0; i < count; ++i)
                childrens.Add(new Child(GlobalAgentDescriptorTable.GetNewId, healthState, locationProbabilitiesFile));

            return childrens;
        }

    }
}
//TODO: 1. создать папку, в которой хранятся файлы с правилами +
//      2. конструктору Child должно передаваться имя папки, а не файла +
//      3. в нём конструировать (зная папку, в которой он находится) имя файла и передавать его в конструктор базового класса, т.е. Person. +
//      4. в момент создания нового child в Form1 передавать ему третьим параметром не путь к файлу, а путь к папке. +

// Form1.cs  49 строка: создать новый файл правил для Person, прописать путь к этому файлу.