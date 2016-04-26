using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreAMS.AgentManagementSystem;
using CoreAMS.AgentCore;
using CoreAMS;
using Agent.Agents;
using CoreAMS.Global;
using Agent.Containers;
using System.IO;

namespace VisualSimulation
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private static void fillContainers() 
        {
            Home home = new Home(0, 50, 12);
            Containers.Instance.Add(0, home); //Containers.Instance — глобальная коллекция, содержащая контейнеры.

            Hospital hospital = new Hospital(1, 237, 19);
            Containers.Instance.Add(1, hospital);

            Mall mall = new Mall(2, 578, 90);
            Containers.Instance.Add(2, mall);

            Office office = new Office(3, 236, 20);
            Containers.Instance.Add(3, office);

            University university = new University(4, 300, 25);
            Containers.Instance.Add(4, university);

            School school = new School(5, 250, 30);
            Containers.Instance.Add(5, school);

            Nursery nursery = new Nursery(6, 60, 23);
            Containers.Instance.Add(6, nursery);

        }

        private static void fillAgents()
        {
            List<IAgent> p = new List<IAgent>(); // создаем пустой список агентов
            p.AddRange(Adolescent.AdolescentList(Enums.HealthState.Infectious, 6, "LocationProbabilities"));
            p.AddRange(Adolescent.AdolescentList(Enums.HealthState.Susceptible, 750, "LocationProbabilities"));
            p.AddRange(Adult.AdultList(Enums.HealthState.Infectious, 6, "LocationProbabilities"));
            p.AddRange(Adult.AdultList(Enums.HealthState.Susceptible, 2450, "LocationProbabilities"));
            p.AddRange(Child.ChildList(Enums.HealthState.Infectious, 6, "LocationProbabilities"));
            p.AddRange(Child.ChildList(Enums.HealthState.Susceptible, 250, "LocationProbabilities"));
            p.AddRange(Elder.ElderList(Enums.HealthState.Infectious, 6, "LocationProbabilities"));
            p.AddRange(Elder.ElderList(Enums.HealthState.Susceptible, 900, "LocationProbabilities"));
            p.AddRange(Youngster.YoungsterList(Enums.HealthState.Infectious, 6, "LocationProbabilities"));
            p.AddRange(Youngster.YoungsterList(Enums.HealthState.Susceptible, 650, "LocationProbabilities"));

            GlobalAgentDescriptorTable.AddAgents(p); // добавляем созданные агенты в класс, в котором хранятся все агенты
        }

        Thread startAgentsThread = new Thread(() => {
            for (int i = 0; i < 80; i++)
            {
                fillContainers();
                fillAgents();
                AgentManagementSystem.RunAgents();
                String lastStringInResultsFile = String.Format("{0},{1},{2},{3},{4}", AgentManagementSystem.susceptibleAgentsCount, AgentManagementSystem.funeralAgentsCount,
                    AgentManagementSystem.deadAgentsCount, AgentManagementSystem.recoveredAgentsCount, GlobalTime.Time);

                if (!File.Exists("D:/FileOfResults.csv"))
                {
                    File.Create("D:/FileOfResults.csv").Dispose();
                }
                StreamWriter resultsFile = File.AppendText("D:/FileOfResults.csv");
                resultsFile.WriteLine(lastStringInResultsFile);
                resultsFile.Dispose();

                GlobalAgentDescriptorTable.DeleteAllAgents();
                Containers.Instance.Clear();
                GlobalTime.Time = 0;
            }
            
        }); //инициализация потока, в котором происходит жизнь; поле

        // События, которые происходят при нажатии на кнопку Start
        private void button1_Click(object sender, EventArgs e)
        {
            startAgentsThread.Start(); // в отдельном потоке запускаем всех агентов
            timer1.Start(); // запускаем счетчик времени, для обновления окошка (ко времени системы не имеет никакого отношения)

            button1.Enabled = false;
            button2.Enabled = true;
        }

        // По таймеру обновляем количество агентов в каждом из состояний
        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = AgentManagementSystem.susceptibleAgentsCount.ToString();
            label2.Text = AgentManagementSystem.exposedAgentsCount.ToString();
            label3.Text = AgentManagementSystem.infectiousAgentsCount.ToString();
            label4.Text = AgentManagementSystem.recoveredAgentsCount.ToString();
            label12.Text = AgentManagementSystem.funeralAgentsCount.ToString();
            label11.Text = AgentManagementSystem.deadAgentsCount.ToString();
        }

        // События, которые происходят при нажатии на кнопку Stop
        private void button2_Click(object sender, EventArgs e)
        {
            //TODO: сейчас: при нажатии кнопки Stop всё останавливается и всё.
            // сделать: чтобы при нажатии кнопки Start после нажатия кнопки Stop всё продолжилось с того места, где остановилось.
            this.startAgentsThread.Interrupt(); // остановка потока при нажатии на кнопку Stop
            button1.Enabled = true;
            button2.Enabled = false;
            this.timer1.Stop(); // остановка счётчика времени

        }
    }
}
