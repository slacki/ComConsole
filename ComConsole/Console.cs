using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;

namespace ComConsole
{
    class Console
    {
        public RichTextBox rtb1, rtb2;
        private Thread readThread;
        private SerialPort sPort;

        public Console(RichTextBox rtb1, RichTextBox rtb2, SerialPort sPort)
        {
            this.rtb1 = rtb1;
            this.rtb2 = rtb2;
            this.sPort = sPort;

            this.readThread = new Thread(Read);
            this.readThread.IsBackground = true;
            this.readThread.Start();
        }

        public void Write()
        {
            if (!(this.sPort == null || !this.sPort.IsOpen)) {
                if (this.rtb2.Text != "") {
                    String data = this.rtb2.Text;
                    this.sPort.Write(data + "\n");
                    this.rtb1.AppendText("[Sent] " + data + "\n");
                }
            }
            else {
                this.rtb1.AppendText("[!] The port is not opened. Open the port before sending a command.");
            }

            this.rtb2.Text = "";
        }

        private void Read()
        {
            while (true) {
                if (this.sPort == null || !this.sPort.IsOpen) { break; }
                try {
                    string message = this.sPort.ReadLine();
                    this.rtb1.Invoke(new Action(delegate()
                    {
                        this.rtb1.AppendText("[Recieved] " + message + "\n");
                    }));
                }
                catch (Exception e) {
                    this.rtb1.Invoke(new Action(delegate()
                    {
                        this.rtb1.AppendText(e.Message);
                    }));
                }
            }
        }
    }
}
