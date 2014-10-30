using System;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Collections.Generic;
using System.Threading;


namespace ComConsole
{
    public class ComPort
    {
        public SerialPort sPort;
        public Thread readThread;
        public bool continueReadThread;

        public delegate void EventHandler(string param);
        public EventHandler DataRecieved;
        public EventHandler StatusChanged;

        public ComPort()
        {
            this.sPort = new SerialPort();
            this.readThread = null;
            this.continueReadThread = false;
        }

        private void StartReading()
        {
            if (!this.continueReadThread) {
                this.continueReadThread = true;
                this.readThread = new Thread(Read);
                this.readThread.Start();
            }
        }

        private void StopReading()
        {
            if (this.continueReadThread) {
                this.continueReadThread = false;
                this.readThread.Join(); // block until exist
                this.readThread = null;
            }
        }

        private void Read()
        {
            while (this.continueReadThread) {
                if (IsOpen) {
                    byte[] readBuffer = new byte[this.sPort.ReadBufferSize + 1];
                    try {
                        int count = this.sPort.Read(readBuffer, 0, this.sPort.ReadBufferSize);
                        String SerialIn = System.Text.Encoding.GetEncoding(1250).GetString(readBuffer, 0, count);
                        this.DataRecieved(SerialIn);
                    } catch (TimeoutException) { }
                } else {
                    TimeSpan waitTime = new TimeSpan(0, 0, 0, 0, 50); // 50ms
                    Thread.Sleep(waitTime);
                }
            }
        }

        public void Send(string data)
        {
            if (IsOpen) {
                // this is equal to Append LF
                string lineEnding = "\n";
                byte[] utf8string = System.Text.Encoding.GetEncoding(1250).GetBytes(data);

                try {
                    this.sPort.Write(utf8string, 0, utf8string.Length);
                    this.sPort.Write(lineEnding);
                } catch (TimeoutException) { }
            }
        }

        public void Open(string port, int rate, Parity parity, int databits, StopBits stopbits)
        {
            this.Close();

            try {
                this.sPort.PortName = port;
                this.sPort.BaudRate = rate;
                this.sPort.Parity = parity;
                this.sPort.StopBits = stopbits;
                this.sPort.DataBits = databits;

                this.sPort.ReadTimeout = 50;
                this.sPort.WriteTimeout = 50;

                this.sPort.Encoding = System.Text.Encoding.GetEncoding(1250);

                this.sPort.Open();
                this.StartReading();
            } catch (IOException) {
                this.StatusChanged(String.Format("[!] {0} does not exist", port));
            } catch (UnauthorizedAccessException) {
                this.StatusChanged(String.Format("[!] {0} is already in use", port));
            }

            if (IsOpen) {
                string parityFirstChar = this.sPort.Parity.ToString().Substring(0, 1);
                string handshake = "No handshake"; // not supported... yet
                string welcomeMessage = String.Format("{0}: {1} bps, {2}{3}{4}, {5}\n",
                    port, rate, databits, parityFirstChar, (int)stopbits, handshake);
                this.StatusChanged(welcomeMessage);
            }
        }

        public void Close()
        {
            this.StopReading();
            this.sPort.Close();
        }

        public bool IsOpen
        {
            get
            {
                return this.sPort != null && this.sPort.IsOpen;
            }
        }

        public string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}
