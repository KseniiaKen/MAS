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
using Agent;

namespace VisualSimulation
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // События, которые происходят при нажатии на кнопку Start
        private void button1_Click(object sender, EventArgs e)
        {
            List<IAgent> p = new List<IAgent>(); // создаем пустой список агентов

            p.AddRange(Person.PersonList(Enums.HealthState.Infectious, 10000)); // добавляем инфицированных агентов
            p.AddRange(Person.PersonList(Enums.HealthState.Susceptible, 90000)); // добавляем здоровых агентов

            GlobalAgentDescriptorTable.AddAgents(p); // добавляем созданные агенты в класс, в котором хранятся все агенты

            new Thread(() => AgentManagementSystem.RunAgents()).Start(); // в отдельном потоке запускаем всех агентов

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
        }
    }
}
