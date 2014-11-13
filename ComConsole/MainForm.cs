using GlobalHotkeys;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Ports;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace ComConsole
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Instalce of ComConsole.ComPort class, provides access to the port
        /// </summary>
        private ComPort cPort;

        /// <summary>
        /// List of hotkeys binded to commands
        /// </summary>
        private List<GlobalHotkey> ghList = new List<GlobalHotkey>();

        /// <summary>
        /// Form constructor
        /// </summary>
        public MainForm()
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

            this.DeserializeHotkeys();
            this.RenewAllHotkeys();
            this.FillListViewWithRenewedHotkeys();

            this.Hide();
        }

        /// <summary>
        /// Fires open method of ComConsole.ComPort 
        /// and prepares all the data needed by it's constructor
        /// </summary>
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

        /// <summary>
        /// Saves the information about used port
        /// </summary>
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

        /// <summary>
        /// Sets the same port settings as used proviously
        /// </summary>
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


        #region Global hotkeys handling

        /// <summary>
        /// Catches Windows' message about pressed hotkey
        /// and fires up the HandleHotkey method
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            // we check if the message is about out hotkey
            var hotkeyInfo = HotkeyInfo.GetFromMessage(m);
            if (hotkeyInfo != null) {
                string command = null;
                foreach (GlobalHotkey gh in this.ghList) {
                    if (gh.Key.Equals((int)hotkeyInfo.Key)) {
                        command = gh.command;
                    }
                }
                this.HandleHotkey(hotkeyInfo, command);
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Adds the hotkey and binded command
        /// </summary>
        private void HandleKeyBindAdd()
        {
            int globalHotkeyId;

            var key = (Keys)Enum.Parse(typeof(Keys), textBoxKey.Text.ToUpper());
            // check if mod keys are checked
            var mod = Modifiers.NoMod;
            if (this.checkBoxAlt.Checked) { mod = mod | Modifiers.Alt; }
            if (this.checkBoxCtlr.Checked) { mod = mod | Modifiers.Ctrl; }
            if (this.checkBoxShift.Checked) { mod = mod | Modifiers.Shift; }
            if (this.checkBoxWinKey.Checked) { mod = mod | Modifiers.Win; }

            try {
                GlobalHotkey gh = new GlobalHotkey(mod, key, this, true);
                globalHotkeyId = gh.Id;
                gh.command = this.textBoxCommand.Text;
                this.ghList.Add(gh);
            } catch (GlobalHotkeyException e) {
                MessageBox.Show(e.Message);
                return;
            }

            // when shortcut registered successfully
            ListViewItem listItem = new ListViewItem(String.Format("{0} {1}", mod, key));
            listItem.SubItems.Add(this.textBoxCommand.Text);
            listItem.SubItems.Add(globalHotkeyId.ToString());

            this.listView1.Items.Add(listItem);
        }

        /// <summary>
        /// Removes hotkey and binded command
        /// </summary>
        private void HandleKeyBindRemove()
        {
            // it was checked if anything is selected
            // and we know for sure only one item can be selected 
            int globalHotkeyId = Int32.Parse(this.listView1.SelectedItems[0].SubItems[2].Text);
            GlobalHotkey ghToRemove = null;

            // we are looking for the gh to delete
            foreach (GlobalHotkey gh in this.ghList) {
                if (gh.Id.Equals(globalHotkeyId)) {
                    ghToRemove = gh;
                }
            }

            // then we find it's index in a List, remove it and call Dispose() to unregister
            int i = 0;
            while (i < this.ghList.Count) {
                GlobalHotkey currentGh = this.ghList[i];
                if (ghToRemove.Equals(currentGh)) {
                    this.ghList.RemoveAt(i);
                    ghToRemove.Dispose();
                    break;
                }
                i++;
            }

            this.listView1.SelectedItems[0].Remove();
        }

        /// <summary>
        /// Handles pressed hotkey (actually only sends the data to the port and displays them)
        /// </summary>
        /// <param name="hotkeyInfo">Information about the hotkey</param>
        /// <param name="command">String of command binded to the hotkey</param>
        private void HandleHotkey(HotkeyInfo hotkeyInfo, string command)
        {
            if (command == null) return;
            this.cPort.Send(command);
            this.richTextBox1.AppendText(String.Format("[S] {0} \n", command));
        }

        /// <summary>
        /// Hotkey add button pressed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAddHotkey_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(this.textBoxKey.Text) ||
                String.IsNullOrWhiteSpace(this.textBoxCommand.Text)) {
                return;
            }

            this.HandleKeyBindAdd();
        }

        /// <summary>
        /// Hotkey delete button pressed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonDeleteHotkey_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 0) {
                return;
            }

            this.HandleKeyBindRemove();
        }

        /// <summary>
        /// Serializes the hotkeys objects and writes them down into the file
        /// </summary>
        private void SerializeHotkeys()
        {
            DeflateStream defStream = new DeflateStream(File.OpenWrite("./hotkeys.dat"), CompressionMode.Compress);
            BinaryFormatter bFormatter = new BinaryFormatter();
            bFormatter.Serialize(defStream, this.ghList);

            defStream.Flush();
            defStream.Close();
            defStream.Dispose();

            bFormatter = null;
        }

        /// <summary>
        /// Deserializes the hotkeys objects from the file
        /// </summary>
        private void DeserializeHotkeys()
        {
            DeflateStream defStream = new DeflateStream(File.OpenRead("./hotkeys.dat"), CompressionMode.Decompress);
            BinaryFormatter bFormatter = new BinaryFormatter();
            object list = bFormatter.Deserialize(defStream);
            defStream.Close();
            defStream.Dispose();

            this.ghList = null;
            this.ghList = list as List<GlobalHotkey>;
        }

        /// <summary>
        /// Re-registers all the deserialized hotkeys
        /// </summary>
        private void RenewAllHotkeys()
        {
            foreach (GlobalHotkey gh in this.ghList) {
                gh.Renew(gh, this);
            }
        }

        /// <summary>
        /// Fills the listView control with the information about deserialized hotkeys
        /// </summary>
        private void FillListViewWithRenewedHotkeys()
        {
            foreach (GlobalHotkey gh in this.ghList) {
                var key = (Keys)Enum.Parse(typeof(Keys), gh.Key.ToString());
                ListViewItem listItem = new ListViewItem(String.Format("{0} {1}", gh.Modifier, key));
                listItem.SubItems.Add(gh.command);
                listItem.SubItems.Add(gh.Id.ToString());
                this.listView1.Items.Add(listItem);
            }
        }

        /// <summary>
        /// Unregisters all hotkeys
        /// </summary>
        private void UnregisterAllHotkeys()
        {
            foreach (GlobalHotkey gh in this.ghList) {
                try {
                    gh.Unregister();
                } catch { }
            }
        }

        #endregion


        #region Filling form with data

        /// <summary>
        /// Fills the control with available COM ports information
        /// </summary>
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

        /// <summary>
        /// Fills the control with bitrate
        /// </summary>
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

        /// <summary>
        /// Fills the control with databits
        /// </summary>
        private void AddDataBits()
        {
            this.comboBoxDataBits.Items.Add(5);
            this.comboBoxDataBits.Items.Add(6);
            this.comboBoxDataBits.Items.Add(7);
            this.comboBoxDataBits.Items.Add(8);

            this.comboBoxDataBits.Text = this.comboBoxDataBits.Items[3].ToString();
        }

        /// <summary>
        /// Fills the control with stopbits
        /// </summary>
        private void AddStopBits()
        {
            this.comboBoxStopBits.Items.Add(StopBits.None.ToString());
            this.comboBoxStopBits.Items.Add(StopBits.One.ToString());
            this.comboBoxStopBits.Items.Add(StopBits.OnePointFive.ToString());
            this.comboBoxStopBits.Items.Add(StopBits.Two.ToString());

            this.comboBoxStopBits.Text = this.comboBoxStopBits.Items[1].ToString();
        }

        /// <summary>
        /// Fills the control with the parity settings
        /// </summary>
        private void AddParity()
        {
            this.comboBoxParity.Items.Add(Parity.None.ToString());
            this.comboBoxParity.Items.Add(Parity.Even.ToString());
            this.comboBoxParity.Items.Add(Parity.Odd.ToString());
            this.comboBoxParity.Items.Add(Parity.Mark.ToString());
            this.comboBoxParity.Items.Add(Parity.Space.ToString());

            this.comboBoxParity.Text = this.comboBoxParity.Items[0].ToString();
        }

        /// <summary>
        /// Fills the control with the handshake settings
        /// </summary>
        private void AddHandshake()
        {
            this.comboBoxHandshake.Items.Add(Handshake.None.ToString());
            this.comboBoxHandshake.Items.Add(Handshake.RequestToSend.ToString());
            this.comboBoxHandshake.Items.Add(Handshake.RequestToSendXOnXOff.ToString());
            this.comboBoxHandshake.Items.Add(Handshake.XOnXOff.ToString());

            this.comboBoxHandshake.Text = this.comboBoxHandshake.Items[0].ToString();
        }

        #endregion


        #region Receiving and sending data

        // delegates used for Invoke()
        internal delegate void DataRecievedDelegate(object sender, DataRecievedEventArgs e);
        internal delegate void StatusChangedDelegate(object sender, StatusChangedEventArgs e);

        /// <summary>
        /// Stores the recieved bits that are incomplete and are waiting for the rest 
        /// to become complete message
        /// </summary>
        private string partialLine = null;

        /// <summary>
        /// Prepares data before sending it to the port
        /// </summary>
        /// <param name="stringIn"></param>
        /// <returns></returns>
        private String PrepareData(string stringIn)
        {
            // The names of the first 32 characters
            string[] charNames = 
            {
                "NUL", "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL", "BS", "TAB",
                "LF", "VT", "FF", "CR", "SO", "SI", "DLE", "DC1", "DC2", "DC3",
                "DC4", "NAK", "SYN", "ETB", "CAN", "EM", "SUB", "ESC", "FS", "GS",
                "RS", "US", "Space"
            };

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

        /// <summary>
        /// Adds incomplete data to partialLine variable
        /// </summary>
        /// <param name="stringIn"></param>
        /// <returns></returns>
        private String AddDataToPartialLine(string stringIn)
        {
            string stringOut = this.PrepareData(stringIn);

            this.partialLine += stringOut;
            return this.partialLine;
        }

        /// <summary>
        /// Sends commands to port and appends local echo
        /// </summary>
        private void SendData()
        {
            string command = this.richTextBox2.Text.ToString();
            this.richTextBox2.Text = "";
            this.richTextBox2.Focus();

            if (command.Length > 0) {
                this.cPort.Send(command);
                // local echo
                this.PrintLine(command, false);
            }
        }

        /// <summary>
        /// Prints line to the window
        /// </summary>
        /// <param name="dataIn"></param>
        /// <param name="recieved"></param>
        private void PrintLine(string dataIn, bool recieved = true)
        {
            string prefix;
            if (recieved) {
                prefix = "[R] ";
            } else {
                prefix = "[S] ";
            }

            if (dataIn.Length > 0) {
                this.richTextBox1.AppendText(prefix + dataIn + "\n");
            }
        }

        /// <summary>
        /// On data recieved event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnDataRecieved(object sender, DataRecievedEventArgs e)
        {
            if (InvokeRequired) {
                Invoke(new DataRecievedDelegate(this.OnDataRecieved), new object[] { sender, e });
                return;
            }

            string dataIn = e.data;

            // if we detect a line terminator
            int index;
            while (dataIn.Length > 0 && ((index = dataIn.IndexOf("\r")) != -1 || (index = dataIn.IndexOf("\n")) != -1)) {
                string stringIn = dataIn.Substring(0, index);
                dataIn = dataIn.Remove(0, index + 1);

                this.AddDataToPartialLine(stringIn);
            }

            // if there are bytes left
            if (dataIn.Length > 0) {
                this.partialLine = this.AddDataToPartialLine(dataIn);
                return;
            }

            this.PrintLine(this.partialLine);
            this.partialLine = null;
        }

        /// <summary>
        /// On port status changed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnStatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (InvokeRequired) {
                Invoke(new StatusChangedDelegate(this.OnStatusChanged), new object[] { sender, e });
                return;
            }

            this.richTextBox1.Clear();
            this.richTextBox1.AppendText("# " + e.status + "\n");
        }

        /// <summary>
        /// On send button pressed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendButton_Click(object sender, EventArgs e)
        {
            this.SendData();
        }

        /// <summary>
        /// On enter key pressed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBox2_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13) {
                this.SendData();
            }
        }

        #endregion


        #region The rest of events

        /// <summary>
        /// On save port data and reconnect button pressed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            this.SavePortInfo();
            // reopen the port after changeing it's parameters
            this.FireOpen();
        }

        /// <summary>
        /// On text changed event
        /// Performs auto scrolling the main text window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            this.richTextBox1.ScrollToCaret();
        }

        /// <summary>
        /// On append to text radio button checked event
        /// Changes append to text settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// On form resize event
        /// After clicking minimize button, we put an application to tray
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) {
                this.Hide();

                this.notifyIcon1.BalloonTipTitle = "ComConsole hidden";
                this.notifyIcon1.BalloonTipText = "The application is now hidden and is waiting to take your oders.";
                this.notifyIcon1.ShowBalloonTip(3000);
            }
        }

        /// <summary>
        /// On tray icon double click event
        /// We show the main window again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.allowVisible = true;
            this.Show();
            WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// On application closed event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            this.cPort.Close();
            this.SerializeHotkeys();
            this.UnregisterAllHotkeys();

            base.OnClosed(e);
        }

        /// <summary>
        /// Is application visible?
        /// </summary>
        private bool allowVisible = false;

        /// <summary>
        /// This method checks wether we allow application to be visible.
        /// If we do, then it will appear after tray icon is double clicked
        /// </summary>
        /// <param name="value"></param>
        protected override void SetVisibleCore(bool value)
        {
            if (!this.allowVisible) {
                value = false;
                if (!this.IsHandleCreated) this.CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        #endregion

    }
}
