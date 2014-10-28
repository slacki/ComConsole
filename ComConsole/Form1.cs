using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO.Ports;

namespace ComConsole
{
    public partial class Form1 : Form
    {
        SerialPort SPort = null;

        public Form1()
        {
            InitializeComponent();

            this.AddComPorts();
            this.AddBitrate();
            this.AddDataBits();
            this.AddStopBits();
            this.AddParity();

            this.RevokePreviousPortSettings();
            this.PrintInitMessage();

            this.Connect();
            Thread readThread = new Thread(Read);
            readThread.IsBackground = true; // hell yeah, finally.
            readThread.Start();
        }

        private void Read()
        {
            while (true) {
                try {
                    string message = this.SPort.ReadLine();
                    richTextBox1.Invoke(new Action(delegate()
                    {
                        richTextBox1.AppendText("[Recieved] " + message + "\n");
                    }));
                } catch (Exception e) { }
            }
        }

        private void Write()
        {
            String data = richTextBox2.Text;
            this.SPort.Write(data + "\n");
            richTextBox1.AppendText("[Sent] " + data + "\n");
            richTextBox2.Text = "";
        }

        private void Connect()
        {
            if (this.SPort != null) {
                if (this.SPort.IsOpen) {
                    this.SPort.Close();
                }
            }
            
            string port = comboBoxPort.Text.ToString();
            int rate = Convert.ToInt32(comboBoxRate.Text);
            int databits = Convert.ToInt32(comboBoxDataBits.Text);
            StopBits stopbits = (StopBits)Enum.Parse(typeof(StopBits), comboBoxStopBits.Text);
            Parity parity = (Parity)Enum.Parse(typeof(Parity), comboBoxParity.Text);

            this.OpenConnection(port, rate, parity, databits, stopbits);
        }

        private void Reconnect()
        {
            this.Connect();
        }

        private void OpenConnection(String port, int rate, Parity parity, int databits, StopBits stopbits)
        {
            try {
                this.SPort = new SerialPort(port, rate, parity, databits, stopbits);
                this.SPort.Open();
            } catch (Exception e) {
                richTextBox1.AppendText("Error: " + e.Message);
            }
        }

        private void RevokePreviousPortSettings()
        {
            comboBoxPort.Text = Properties.Settings.Default.port;
            comboBoxRate.Text = Convert.ToString(Properties.Settings.Default.rate);
            comboBoxDataBits.Text = Convert.ToString(Properties.Settings.Default.databits);
            comboBoxStopBits.Text = Convert.ToString(Properties.Settings.Default.stopbits);
            comboBoxParity.Text = Convert.ToString(Properties.Settings.Default.parity);
        }

        private void PrintInitMessage()
        {
            richTextBox1.Clear(); // when we reconnect and reprint this after configuration change

            string message = "--- COM Console Initialization ---\n";
            message += "   Connected on port: " + comboBoxPort.Text.ToString() + "\n";
            message += "   Rate:              " + comboBoxRate.Text.ToString() + "\n";
            message += "   Data bits:         " + comboBoxDataBits.Text.ToString() + "\n";
            message += "   Stop bits:         " + comboBoxStopBits.Text.ToString() + "\n";
            message += "   Parity:            " + comboBoxParity.Text.ToString() + "\n";
            message += "--- COM Console Initialization ---\n\n\n";

            richTextBox1.AppendText(message);
        }

        private void AddComPorts()
        {
            string[] comPortsNames = null;
            int index = -1;
            string comPortName = null;

            comPortsNames = SerialPort.GetPortNames();
            if (!(comPortsNames == null || comPortsNames.Length == 0)) {
                do {
                    index = index + 1;
                    comboBoxPort.Items.Add(comPortsNames[index]);
                } while (!((comPortsNames[index] == comPortName) ||
                    (index == comPortsNames.GetUpperBound(0))));
                Array.Sort(comPortsNames);

                comboBoxPort.Text = comboBoxPort.Items[0].ToString();
            }
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
            this.SavePortInfo();
            this.Reconnect();
            this.PrintInitMessage();
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            this.Write();
        }

        private void richTextBox2_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13) {
                this.Write();
            }
        }
    }
}
