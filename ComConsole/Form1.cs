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

        public Form1()
        {
            InitializeComponent();

            this.AddComPorts();
            this.AddBitrate();
            this.AddDataBits();
            this.AddStopBits();
            this.AddParity();
            this.AddHandshake();

            this.RevokePreviousSettings();

            this.cPort = new ComPort();
            this.cPort.OnStatusChanged += this.OnStatusChanged;
            this.cPort.OnDataRecieved += this.OnDataRecieved;
            this.FireOpen();
        }

        protected override void OnClosed(EventArgs e)
        {
            this.cPort.Close();
            base.OnClosed(e);
        }

        private void FireOpen()
        {
            string port = comboBoxPort.Text.ToString();
            int rate = Convert.ToInt32(comboBoxRate.Text);
            int databits = Convert.ToInt32(comboBoxDataBits.Text);
            StopBits stopbits = (StopBits)Enum.Parse(typeof(StopBits), comboBoxStopBits.Text);
            Parity parity = (Parity)Enum.Parse(typeof(Parity), comboBoxParity.Text);
            Handshake handshake = (Handshake)Enum.Parse(typeof(Handshake), comboBoxHandshake.Text);

            this.cPort.Open(port, rate, parity, databits, stopbits, handshake);
        }

        private void SavePortInfo()
        {
            Properties.Settings.Default["port"] = this.comboBoxPort.Text.ToString();
            Properties.Settings.Default["rate"] = Convert.ToInt32(this.comboBoxRate.Text);
            Properties.Settings.Default["databits"] = Convert.ToInt32(this.comboBoxDataBits.Text);
            Properties.Settings.Default["stopbits"] = (StopBits)Enum.Parse(typeof(StopBits), this.comboBoxStopBits.Text);
            Properties.Settings.Default["parity"] = (Parity)Enum.Parse(typeof(Parity), this.comboBoxParity.Text);
            Properties.Settings.Default["handshake"] = (Handshake)Enum.Parse(typeof(Handshake), this.comboBoxHandshake.Text);

            Properties.Settings.Default.Save();
        }

        private void PrintWelcomeMessage()
        {
            string message = "# COM Console v1.2\n";
            message += "# Copyright (C) 2014 Kacper Czochara\n";
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

        private void PrintLine(string dataIn)
        {
            if (dataIn.Length > 0) {
                this.richTextBox1.AppendText("[R] " + dataIn + "\n");
            }
        }

        private void RevokePreviousSettings()
        {
            this.comboBoxPort.Text = Properties.Settings.Default.port;
            this.comboBoxRate.Text = Convert.ToString(Properties.Settings.Default.rate);
            this.comboBoxDataBits.Text = Convert.ToString(Properties.Settings.Default.databits);
            this.comboBoxStopBits.Text = Convert.ToString(Properties.Settings.Default.stopbits);
            this.comboBoxParity.Text = Convert.ToString(Properties.Settings.Default.parity);
            this.comboBoxHandshake.Text = Convert.ToString(Properties.Settings.Default.handshake);

            // append to text
            switch (Properties.Settings.Default.append) {
                case (int)AppendToText.CR:
                    this.radioButtonAppendCR.Checked = true; break;
                case (int)AppendToText.LF:
                    this.radioButtonAppendLF.Checked = true; break;
                case (int)AppendToText.CRLF:
                    this.radioButtonAppendCRLF.Checked = true; break;
                default:
                    this.radioButtonAppendNothing.Checked = true; break;
            }
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
                    this.comboBoxPort.Items.Add(comPortsNames[index]);
                } while (!((comPortsNames[index] == comPortName) ||
                    (index == comPortsNames.GetUpperBound(0))));
                Array.Sort(comPortsNames);

                this.comboBoxPort.Text = comboBoxPort.Items[0].ToString();
            }
        }

        private void AddBitrate()
        {
            this.comboBoxRate.Items.Add(300);
            this.comboBoxRate.Items.Add(600);
            this.comboBoxRate.Items.Add(1200);
            this.comboBoxRate.Items.Add(2400);
            this.comboBoxRate.Items.Add(9600);
            this.comboBoxRate.Items.Add(14400);
            this.comboBoxRate.Items.Add(19200);
            this.comboBoxRate.Items.Add(38400);
            this.comboBoxRate.Items.Add(57600);
            this.comboBoxRate.Items.Add(115200);

            comboBoxRate.Text = comboBoxRate.Items[4].ToString();
        }

        private void AddDataBits()
        {
            this.comboBoxDataBits.Items.Add(5);
            this.comboBoxDataBits.Items.Add(6);
            this.comboBoxDataBits.Items.Add(7);
            this.comboBoxDataBits.Items.Add(8);

            this.comboBoxDataBits.Text = this.comboBoxDataBits.Items[3].ToString();
        }

        private void AddStopBits()
        {
            this.comboBoxStopBits.Items.Add(StopBits.None.ToString());
            this.comboBoxStopBits.Items.Add(StopBits.One.ToString());
            this.comboBoxStopBits.Items.Add(StopBits.OnePointFive.ToString());
            this.comboBoxStopBits.Items.Add(StopBits.Two.ToString());

            this.comboBoxStopBits.Text = this.comboBoxStopBits.Items[1].ToString();
        }

        private void AddParity()
        {
            this.comboBoxParity.Items.Add(Parity.None.ToString());
            this.comboBoxParity.Items.Add(Parity.Even.ToString());
            this.comboBoxParity.Items.Add(Parity.Odd.ToString());
            this.comboBoxParity.Items.Add(Parity.Mark.ToString());
            this.comboBoxParity.Items.Add(Parity.Space.ToString());

            this.comboBoxParity.Text = this.comboBoxParity.Items[0].ToString();
        }

        private void AddHandshake()
        {
            this.comboBoxHandshake.Items.Add(Handshake.None.ToString());
            this.comboBoxHandshake.Items.Add(Handshake.RequestToSend.ToString());
            this.comboBoxHandshake.Items.Add(Handshake.RequestToSendXOnXOff.ToString());
            this.comboBoxHandshake.Items.Add(Handshake.XOnXOff.ToString());

            this.comboBoxHandshake.Text = this.comboBoxHandshake.Items[0].ToString();
        }

        #region Events handling

        // delegates used for Invoke()
        internal delegate void DataRecievedDelegate(object sender, DataRecievedEventArgs e);
        internal delegate void StatusChangedDelegate(object sender, StatusChangedEventArgs e);

        private String PrepareData(string stringIn)
        {
            // The names of the first 32 characters
            string[] charNames = { "NUL", "SOH", "STX", "ETX", "EOT",
				"ENQ", "ACK", "BEL", "BS", "TAB", "LF", "VT", "FF", "CR", "SO", "SI",
				"DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB", "CAN", "EM", "SUB",
				"ESC", "FS", "GS", "RS", "US", "Space"};

            string StringOut = "";

            foreach (char c in stringIn) {
                if (c < 32 && c != 9) {
                    StringOut = StringOut + "<" + charNames[c] + ">";
                } else {
                    StringOut = StringOut + c;
                }
            }
            return StringOut;
        }

        private string partialLine = null;

        private String AddDataToPartialLine(string stringIn)
        {
            string stringOut = this.PrepareData(stringIn);

            // if there is a partial line, we add to it
            if (this.partialLine != null) {
                this.partialLine += stringOut;
                return this.partialLine;
            }

            // we dont have partial line, and we push whole line 
            this.PrintLine(stringOut);
            return "";
        }

        public void OnDataRecieved(object sender, DataRecievedEventArgs e)
        {
            if (InvokeRequired) {
                Invoke(new DataRecievedDelegate(this.OnDataRecieved), new object[] { sender, e });
                return;
            }

            string dataIn = e.data;

            // if we detect a line terminator, add line to output
            int index;
            while (dataIn.Length > 0 &&
                ((index = dataIn.IndexOf("\r")) != -1 ||
                (index = dataIn.IndexOf("\n")) != -1)) {
                string stringIn = dataIn.Substring(0, index);
                dataIn = dataIn.Remove(0, index + 1);

                this.PrintLine(this.AddDataToPartialLine(stringIn));

                partialLine = null;	// terminate partial line
            }

            // but if we have partial line, we add to it
            if (dataIn.Length > 0) {
                this.partialLine = AddDataToPartialLine(dataIn);
            }
        }

        public void OnStatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (InvokeRequired) {
                Invoke(new StatusChangedDelegate(this.OnStatusChanged), new object[] { sender, e });
                return;
            }

            this.richTextBox1.Clear();
            // as we are at this point every time we reconnect to the port
            // let me print welcome message here
            this.PrintWelcomeMessage();
            this.richTextBox1.AppendText(e.status + "\n");
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
            this.richTextBox1.ScrollToCaret();
        }

        private void OnRadioButtonCheck(object sender, EventArgs e)
        {
            if (this.radioButtonAppendCR.Checked) {
                Properties.Settings.Default["append"] = (int)AppendToText.CR;
            } else if (this.radioButtonAppendLF.Checked) {
                Properties.Settings.Default["append"] = (int)AppendToText.LF;
            } else if (this.radioButtonAppendCRLF.Checked) {
                Properties.Settings.Default["append"] = (int)AppendToText.CRLF;
            } else {
                Properties.Settings.Default["append"] = (int)AppendToText.Nothing;
            }
            Properties.Settings.Default.Save();
        }

        #endregion
    }
}
