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

namespace VisualSimulation
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void fillContainers() 
        {
            Theater theater = new Theater(356, 23);
            Containers.Instance.Add(theater); //Containers.Instance — глобальная коллекция, содержащая контейнеры.

            Mall mall = new Mall(578, 90);
            Containers.Instance.Add(mall);

            Home home = new Home(200, 12);
            Containers.Instance.Add(home);

            Hospital hospital = new Hospital(237, 19);
            Containers.Instance.Add(hospital);

            Office office = new Office(100, 20);
            Containers.Instance.Add(office);
        }

        private void fillAgents()
        {
            List<IAgent> p = new List<IAgent>(); // создаем пустой список агентов

            p.AddRange(Person.PersonList(Enums.HealthState.Infectious, 10000)); // добавляем инфицированных агентов
            p.AddRange(Person.PersonList(Enums.HealthState.Susceptible, 90000)); // добавляем здоровых агентов

            GlobalAgentDescriptorTable.AddAgents(p); // добавляем созданные агенты в класс, в котором хранятся все агенты

            Child child = new Child(0, Enums.HealthState.Susceptible, "locationProbabilities.csv");
            GlobalAgentDescriptorTable.AddOneAgent(child);

        }

        Thread startAgentsThread = new Thread(() => AgentManagementSystem.RunAgents()); //инициализация потока, в котором происходит жизнь; поле

        // События, которые происходят при нажатии на кнопку Start
        private void button1_Click(object sender, EventArgs e)
        {
            fillContainers();
            fillAgents();
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
