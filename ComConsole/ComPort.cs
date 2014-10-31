using System;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Collections.Generic;
using System.Threading;


namespace ComConsole
{
    /// <summary>
    /// The ComPort class handles the basic operations on COM port
    /// It is using System.IO.Ports.SerialPort()
    /// </summary>
    public class ComPort
    {
        /// <summary>
        /// The System.IO.Ports.SerialPort() instance
        /// </summary>
        public SerialPort sPort;

        /// <summary>
        /// Thread for reading the bytes
        /// </summary>
        public Thread readThread;
        public bool continueReadThread;

        /// <summary>
        /// Event handler for DataRecieved and StatusChanged
        /// </summary>
        /// <param name="str">String with returned message</param>
        public delegate void EventHandler(string str);

        /// <summary>
        /// Event for data recieve
        /// </summary>
        public EventHandler DataRecieved;

        /// <summary>
        /// Event for status change
        /// </summary>
        public EventHandler StatusChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public ComPort()
        {
            this.sPort = new SerialPort();
            this.readThread = null;
            this.continueReadThread = false;
        }

        /// <summary>
        /// Fires up the readThread
        /// </summary>
        private void StartReading()
        {
            if (!this.continueReadThread) {
                this.continueReadThread = true;
                this.readThread = new Thread(Read);
                this.readThread.Start();
            }
        }

        /// <summary>
        /// Stops the readThread
        /// </summary>
        private void StopReading()
        {
            if (this.continueReadThread) {
                this.continueReadThread = false;
                this.readThread.Join(); // block until exist
                this.readThread = null;
            }
        }

        /// <summary>
        /// readThread's method used for reading data from port
        /// </summary>
        private void Read()
        {
            while (this.continueReadThread) {
                if (IsOpen) {
                    byte[] readBuffer = new byte[this.sPort.ReadBufferSize + 1];
                    try {
                        int count = this.sPort.Read(readBuffer, 0, this.sPort.ReadBufferSize);
                        string serialIn = System.Text.Encoding.GetEncoding(1250).GetString(readBuffer, 0, count);
                        this.DataRecieved(serialIn);
                    } catch (TimeoutException) { }
                } else {
                    TimeSpan waitTime = new TimeSpan(0, 0, 0, 0, 50);
                    Thread.Sleep(waitTime);
                }
            }
        }

        /// <summary>
        /// Writes data to the port
        /// </summary>
        /// <param name="data">The data string</param>
        public void Send(string data)
        {
            if (IsOpen) {
                string lineEnding = Properties.Settings.Default.appendToString;
                byte[] utf8string = System.Text.Encoding.GetEncoding(1250).GetBytes(data);

                try {
                    this.sPort.Write(utf8string, 0, utf8string.Length);
                    this.sPort.Write(lineEnding);
                } catch (TimeoutException) { }
            }
        }

        /// <summary>
        /// Opens connection with port
        /// </summary>
        /// <param name="port">Port name</param>
        /// <param name="rate">Baud rate</param>
        /// <param name="parity">Parity</param>
        /// <param name="databits">Data bits</param>
        /// <param name="stopbits">Stop bits</param>
        public void Open(string port, int rate, Parity parity, int databits, StopBits stopbits, Handshake handshake)
        {
            this.Close();

            try {
                this.sPort.PortName = port;
                this.sPort.BaudRate = rate;
                this.sPort.Parity = parity;
                this.sPort.StopBits = stopbits;
                this.sPort.DataBits = databits;
                this.sPort.Handshake = handshake;

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
                string welcomeMessage = String.Format("{0}: {1} bps, {2}{3}{4}, {5}\n",
                    port, rate, databits, parityFirstChar, (int)stopbits, handshake.ToString());
                this.StatusChanged(welcomeMessage);
            }
        }

        /// <summary>
        /// Closes connection with port
        /// </summary>
        public void Close()
        {
            this.StopReading();
            this.sPort.Close();
        }

        /// <summary>
        /// Checks wether port is open
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return this.sPort != null && this.sPort.IsOpen;
            }
        }

        /// <summary>
        /// Returns array of available ports
        /// </summary>
        /// <returns></returns>
        public string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}
