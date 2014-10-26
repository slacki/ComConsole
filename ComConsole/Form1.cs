using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace ComConsole
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();

            this.AddComPorts();
            this.AddBitrate();
            this.AddDataBits();
            this.AddStopBits();
            this.AddParity();
            //this.addHandshake();

            this.RevokePreviousPortSettings();
            this.PrintWelcomeMessage();
        }

        private void RevokePreviousPortSettings()
        {
            comboBoxPort.Text = Properties.Settings.Default.port;
            comboBoxRate.Text = Convert.ToString(Properties.Settings.Default.rate);
            comboBoxDataBits.Text = Convert.ToString(Properties.Settings.Default.databits);
            comboBoxStopBits.Text = Convert.ToString(Properties.Settings.Default.stopbits);
            comboBoxParity.Text = Convert.ToString(Properties.Settings.Default.parity);
        }

        private void PrintWelcomeMessage()
        {
            string message;
            message = "--- COM Console v.0.0.1 Pre-Relase Alpha ---\n";
            message += "Connected on port: \n";
            message += "For Chris with Love <3";

            richTextBox1.AppendText(message);
        }

        private void AddComPorts()
        {
            string[] comPortsNames = null;
            int index = -1;
            string comPortName = null;

            comPortsNames = SerialPort.GetPortNames();
            do {
                index = index + 1;
                comboBoxPort.Items.Add(comPortsNames[index]);
            } while (!((comPortsNames[index] == comPortName) ||
              (index == comPortsNames.GetUpperBound(0))));
            Array.Sort(comPortsNames);

            comboBoxPort.Text = comboBoxPort.Items[0].ToString();
        }

        private void AddBitrate()
        {
            comboBoxRate.Items.Add(300);
            comboBoxRate.Items.Add(600);
            comboBoxRate.Items.Add(1200);
            comboBoxRate.Items.Add(2400);
            comboBoxRate.Items.Add(9600);
            comboBoxRate.Items.Add(14400);
            comboBoxRate.Items.Add(19200);
            comboBoxRate.Items.Add(38400);
            comboBoxRate.Items.Add(57600);
            comboBoxRate.Items.Add(115200);

            comboBoxRate.Text = comboBoxRate.Items[4].ToString();
        }

        private void AddDataBits()
        {
            comboBoxDataBits.Items.Add(5);
            comboBoxDataBits.Items.Add(6);
            comboBoxDataBits.Items.Add(7);
            comboBoxDataBits.Items.Add(8);

            comboBoxDataBits.Text = comboBoxDataBits.Items[3].ToString();
        }

        private void AddStopBits()
        {
            comboBoxStopBits.Items.Add("One");
            comboBoxStopBits.Items.Add("Two");
            
            comboBoxStopBits.Text = comboBoxStopBits.Items[0].ToString();
        }

        private void AddParity()
        {
            comboBoxParity.Items.Add("None");
            comboBoxParity.Items.Add("Even");
            comboBoxParity.Items.Add("Odd");
            comboBoxParity.Items.Add("Mark");
            comboBoxParity.Items.Add("Space");

            comboBoxParity.Text = comboBoxParity.Items[0].ToString();
        }

        private void AddHandshake()
        {
            comboBoxHandshake.Items.Add("None");
            comboBoxHandshake.Items.Add("XOnXOff");
            comboBoxHandshake.Items.Add("RequestToSend");
            comboBoxHandshake.Items.Add("RequestToSendXOnXOff");

            comboBoxHandshake.Text = comboBoxHandshake.Items[0].ToString();
        }

        private void SavePortInfo()
        {
            // we use Properties, the simplest and fastest way to achieve that
            Properties.Settings.Default["port"] = comboBoxPort.Text.ToString();
            Properties.Settings.Default["rate"] = Convert.ToInt32(comboBoxRate.Text);
            Properties.Settings.Default["databits"] = Convert.ToInt32(comboBoxDataBits.Text);
            Properties.Settings.Default["stopbits"] = (StopBits)Enum.Parse(typeof(StopBits), comboBoxStopBits.Text);
            Properties.Settings.Default["parity"] = (Parity)Enum.Parse(typeof(Parity), comboBoxParity.Text);

            Properties.Settings.Default.Save();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // we save the port settings
            this.SavePortInfo();
        }
    }
}
