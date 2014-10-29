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
        private ComPort cPort;

        private string port;
        private int rate;
        private int databits;
        private StopBits stopbits;
        private Parity parity;

        public Form1()
        {
            InitializeComponent();

            this.AddComPorts();
            this.AddBitrate();
            this.AddDataBits();
            this.AddStopBits();
            this.AddParity();

            this.RevokePreviousPortSettings();

            this.cPort = new ComPort();
            this.cPort.StatusChanged += this.OnStatusChanged;
            this.cPort.DataRecieved += this.OnDataRecieved;
            this.FireOpen();
        }

        protected override void OnClosed(EventArgs e)
        {
            this.cPort.Close();
            base.OnClosed(e);
        }

        private void FireOpen()
        {
            this.port = comboBoxPort.Text.ToString();
            this.rate = Convert.ToInt32(comboBoxRate.Text);
            this.databits = Convert.ToInt32(comboBoxDataBits.Text);
            this.stopbits = (StopBits)Enum.Parse(typeof(StopBits), comboBoxStopBits.Text);
            this.parity = (Parity)Enum.Parse(typeof(Parity), comboBoxParity.Text);

            this.cPort.Open(this.port, this.rate, this.parity, this.databits, this.stopbits);
        }

        private void PrintWelcomeMessage()
        {
            string message = "# COM Console v1.2\n";
            message += "# For Chris with Love <3\n\n";
            richTextBox1.AppendText(message);
        }

        private void SendData()
        {
            string command = this.richTextBox2.Text.ToString();
            this.richTextBox2.Text = "";
            this.richTextBox2.Focus();

            if (command.Length > 0) {
                this.cPort.Send(command);
                // local echo
                this.richTextBox1.AppendText(String.Format("[S] {0} \r", command));
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

        private void AddComPorts()
        {
            string[] comPortsNames = null;
            comPortsNames = SerialPort.GetPortNames();
            string comPortName = null;
            int index = -1;
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

        #region Events handling

        // delegate used for Invoke()
        internal delegate void StringDelegate(string data);

        private String PrepareData(string StringIn)
        {
            // The names of the first 32 characters
            string[] charNames = { "NUL", "SOH", "STX", "ETX", "EOT",
				"ENQ", "ACK", "BEL", "BS", "TAB", "LF", "VT", "FF", "CR", "SO", "SI",
				"DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB", "CAN", "EM", "SUB",
				"ESC", "FS", "GS", "RS", "US", "Space"};

            string StringOut = "";

            foreach (char c in StringIn) {
                if (c < 32 && c != 9) {
                    StringOut = StringOut + "<" + charNames[c] + ">";

                    //Uglier "Termite" style
                    //StringOut = StringOut + String.Format("[{0:X2}]", (int)c);
                } else {
                    StringOut = StringOut + c;
                }
            }
            return StringOut;
        }

        public void OnStatusChanged(string status)
        {
            if (InvokeRequired) {
                Invoke(new StringDelegate(OnStatusChanged), new object[] { status });
                return;
            }
            this.richTextBox1.Clear();
            // as we are at this point every time we reconnect to the port
            // let me print welcome message here
            this.PrintWelcomeMessage();
            this.richTextBox1.AppendText(status + "\n");
        }

        public void OnDataRecieved(string dataIn)
        {
            if (InvokeRequired) {
                Invoke(new StringDelegate(OnDataRecieved), new object[] { dataIn });
                return;
            }

            this.richTextBox1.AppendText("[R] " + dataIn);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.SavePortInfo();
            this.FireOpen();
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            this.SendData();
        }

        private void richTextBox2_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13) {
                this.SendData();
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.ScrollToCaret();
        }

        #endregion
    }
}
