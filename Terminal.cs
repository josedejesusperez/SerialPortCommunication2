/* 
 * Project:    SerialPort Terminal
 * Company:    Coad .NET, http://coad.net
 * Author:     Noah Coad, http://coad.net/noah
 * Created:    March 2005
 * 
 * Notes:      This was created to demonstrate how to use the SerialPort control for
 *             communicating with your PC's Serial RS-232 COM Port
 * 
 *             It is for educational purposes only and not sanctified for industrial use. :)
 *             Written to support the blog post article at: http://msmvps.com/blogs/coad/archive/2005/03/23/39466.aspx
 * 
 *             Search for "comport" to see how I'm using the SerialPort control.
 */

#region Namespace Inclusions
using System;
using System.Linq;
using System.Data;
using System.Text;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;

using SerialPortTerminal.Properties;
using System.Threading;
using System.IO;
#endregion

namespace SerialPortTerminal
{
  #region Public Enumerations
  public enum DataMode { Text, Hex }
  public enum LogMsgType { Incoming, Outgoing, Normal, Warning, Error };
  #endregion

  public partial class frmTerminal : Form
  {
    #region Local Variables

        // The main control for communicating through the RS-232 port
        private SerialPort comport = new SerialPort();

        // Various colors for logging info
        private Color[] LogMsgTypeColor = { Color.Blue, Color.Green, Color.Black, Color.Orange, Color.Red };

        // Temp holder for whether a key was pressed
        private bool KeyHandled = false;

        private Settings settings = Settings.Default;
    #endregion

    #region Constructor
    public frmTerminal()
    {
        // Load user settings
        settings.Reload();

        // Build the form
        InitializeComponent();

        // Restore the users settings
        InitializeControlValues();

        // Enable/disable controls based on the current state
        EnableControls();

        // When data is recieved through the port, call this method
        comport.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
        comport.PinChanged += new SerialPinChangedEventHandler(comport_PinChanged);
    }

        void comport_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            // Show the state of the pins
            UpdatePinState();
        }

        private void UpdatePinState()
        {
            this.Invoke(new ThreadStart(() => {
                // Show the state of the pins
                chkCD.Checked = comport.CDHolding;
                chkCTS.Checked = comport.CtsHolding;
                chkDSR.Checked = comport.DsrHolding;
                // WHEN FINISHES TX FROM MACHINE DSR GOES LOW
                LogProcessed(LogMsgType.Incoming, "comport.DsrHolding = " + (comport.DsrHolding ? "True" : "False") + "\n");
            }));
        }
    #endregion

    #region Local Methods
    
    /// <summary> Save the user's settings. </summary>
    private void SaveSettings()
    {
            settings.BaudRate = int.Parse(cmbBaudRate.Text);
            settings.DataBits = int.Parse(cmbDataBits.Text);
            //settings.DataMode = CurrentDataMode;
            settings.Parity = (Parity)Enum.Parse(typeof(Parity), cmbParity.Text);
            settings.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cmbStopBits.Text);
            settings.PortName = cmbPortName.Text;
            settings.ClearOnOpen = chkClearOnOpen.Checked;
            settings.ClearWithDTR = chkClearWithDTR.Checked;

            settings.Save();
    }

    /// <summary> Populate the form's controls with default settings. </summary>
    private void InitializeControlValues()
    {
      cmbParity.Items.Clear(); cmbParity.Items.AddRange(Enum.GetNames(typeof(Parity)));
      cmbStopBits.Items.Clear(); cmbStopBits.Items.AddRange(Enum.GetNames(typeof(StopBits)));

            cmbParity.Text = settings.Parity.ToString();
            cmbStopBits.Text = settings.StopBits.ToString();
            cmbDataBits.Text = settings.DataBits.ToString();
            cmbParity.Text = settings.Parity.ToString();
            cmbBaudRate.Text = settings.BaudRate.ToString();
            //CurrentDataMode = settings.DataMode;

            RefreshComPortList();

            chkClearOnOpen.Checked = settings.ClearOnOpen;
            chkClearWithDTR.Checked = settings.ClearWithDTR;

            // If it is still avalible, select the last com port used
            if (cmbPortName.Items.Contains(settings.PortName)) cmbPortName.Text = settings.PortName;
      else if (cmbPortName.Items.Count > 0)
          cmbPortName.SelectedIndex = cmbPortName.Items.Count - 1;
      else
      {
          MessageBox.Show(this, "There are no COM Ports detected on this computer.\nPlease install a COM Port and restart this app.", "No COM Ports Installed", MessageBoxButtons.OK, MessageBoxIcon.Error);
          this.Close();
      }
    }

    /// <summary> Enable/disable controls based on the app's current state. </summary>
    private void EnableControls()
    {
      // Enable/disable controls based on whether the port is open or not
      gbPortSettings.Enabled = !comport.IsOpen;
      ///txtSendData.Enabled = btnSend.Enabled = comport.IsOpen;
            //chkDTR.Enabled = chkRTS.Enabled = comport.IsOpen;

      if (comport.IsOpen) btnOpenPort.Text = "&Close Port";
      else btnOpenPort.Text = "&Open Port";
    }

    /// <summary> Send the user's data currently entered in the 'send' box.</summary>
        private void SendData()
        {
          try
            {
                byte[] messageToMachine = vendingParser.encodeMessage(txtMessageIndex.Text, cmbCommand.Text, txtData.Text);

                for (int i = 0; i < messageToMachine.Length; i++)
                {
                    if (i == 0)
                        txtSendData.Text = String.Format("{0:x2}", messageToMachine[i]);
                    else
                        txtSendData.Text += String.Format("{0:x2}", messageToMachine[i]);
                }

              // Convert the user's string of hex digits (ex: B4 CA E2) to a byte array
              byte[] data = HexStringToByteArray(txtSendData.Text);

              // Send the binary data out the port
              comport.Write(data, 0, data.Length);

                    // Show the hex digits on in the terminal window
                    LogProcessed(LogMsgType.Outgoing, "Data sent: " + ByteArrayToHexString(data) + "\n");
            }
            catch (FormatException)
            {
                    // Inform the user if the hex string was not properly formatted
                    LogProcessed(LogMsgType.Error, "Not properly formatted hex string: " + txtSendData.Text + "\n");
            }
          txtSendData.SelectAll();
        }

    

    private void getBinary(string msg)
        {
            for (int i = 0; i < msg.Length; i+=2)
            {

            }
        }
        /// <summary> Log data to the terminal window. </summary>
        /// <param name="msgtype"> The type of message to be written. </param>
        /// <param name="msg"> The string containing the message to be shown. </param>
        private void Log(LogMsgType msgtype, string msg)
        {
          rtfTerminal.Invoke(new EventHandler(delegate
          {
            rtfTerminal.SelectedText = string.Empty;
            rtfTerminal.SelectionFont = new Font(rtfTerminal.SelectionFont, FontStyle.Bold);
            rtfTerminal.SelectionColor = LogMsgTypeColor[(int)msgtype];
            rtfTerminal.AppendText(msg);
            rtfTerminal.ScrollToCaret();
          }));
        }
        private void LogProcessed(LogMsgType msgtype, string msg)
        {
            rtfProcessedOutput.Invoke(new EventHandler(delegate
            {
                rtfProcessedOutput.SelectedText = string.Empty;
                rtfProcessedOutput.SelectionFont = new Font(rtfProcessedOutput.SelectionFont, FontStyle.Bold);
                rtfProcessedOutput.SelectionColor = LogMsgTypeColor[(int)msgtype];
                rtfProcessedOutput.AppendText(msg);
                rtfProcessedOutput.ScrollToCaret();
            }));
        }
        /// <summary> Convert a string of hex digits (ex: E4 CA B2) to a byte array. </summary>
        /// <param name="s"> The string containing the hex digits (with or without spaces). </param>
        /// <returns> Returns an array of bytes. </returns>
        private byte[] HexStringToByteArray(string s)
        {
          s = s.Replace(" ", "");
          byte[] buffer = new byte[s.Length / 2];
          for (int i = 0; i < s.Length; i += 2)
            buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
          return buffer;
        }

        /// <summary> Converts an array of bytes into a formatted string of hex digits (ex: E4 CA B2)</summary>
        /// <param name="data"> The array of bytes to be translated into a string of hex digits. </param>
        /// <returns> Returns a well formatted string of hex digits with spacing. </returns>
        private string ByteArrayToHexString(byte[] data)
            {
                int count = 0;
                StringBuilder sb = new StringBuilder(data.Length * 3);
                foreach (byte b in data)
                {
                    sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
                    count++;
                    //if ((count % 2) == 0)
                    //    sb.Append("\n");
                }
                return sb.ToString().ToUpper();
            }
            #endregion
        private string convertToBinary(byte[] data)
            {
                int count = 0;
                //storage for the resulting string
                string result = string.Empty;
                //iterate through the byte[]
                foreach (byte value in data)
                {
                    //storage for the individual byte
                    string binarybyte = Convert.ToString(value, 2);
                    //if the binarybyte is not 8 characters long, its not a proper result
                    while (binarybyte.Length < 8)
                    {
                        //prepend the value with a 0
                        binarybyte = "0" + binarybyte;
                        //if (binarybyte.Length % 4 == 0)
                        //    binarybyte = binarybyte + "|";
                    }
                    //append the binarybyte to the result
                    result += binarybyte + " ";
                    count++;
                    if((count % 2) == 0)
                        result = result + "\n";
                }
                //return the result
                return result;
            }
            //input.ToString().PadLeft(length, '0');
        private string convertToInteger(byte[] data)
        {
            int count = 0;
            //storage for the resulting string
            string result = string.Empty;
            //iterate through the byte[]
            foreach (byte value in data)
            {
                ;
                //append the binarybyte to the result
                result += value.ToString().PadLeft(4, '0') + " ";
                count++;
                if ((count % 2) == 0)
                    result = result + "\n";
            }
            //return the result
            return result;
        }

        #region Local Properties
        
        #endregion

    #region Event Handlers
    private void lnkAbout_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
      // Show the user the about dialog
      (new frmAbout()).ShowDialog(this);
    }
    
    private void frmTerminal_Shown(object sender, EventArgs e)
    {
            LogProcessed(LogMsgType.Normal, String.Format("Application Started at {0}\n", DateTime.Now));
    }
    private void frmTerminal_FormClosing(object sender, FormClosingEventArgs e)
    {
      // The form is closing, save the user's preferences
      SaveSettings();
    }

    private void cmbBaudRate_Validating(object sender, CancelEventArgs e)
    { int x; e.Cancel = !int.TryParse(cmbBaudRate.Text, out x); }

    private void cmbDataBits_Validating(object sender, CancelEventArgs e)
    { int x; e.Cancel = !int.TryParse(cmbDataBits.Text, out x); }

    private void btnOpenPort_Click(object sender, EventArgs e)
    {
            bool error = false;

      // If the port is open, close it.
      if (comport.IsOpen)
                comport.Close();
      else
      {
            // Set the port's settings
            comport.BaudRate = 9600;
            comport.DataBits = 8;
            comport.StopBits = StopBits.One;
            comport.Parity = Parity.None;
            comport.PortName = cmbPortName.Text;
            comport.DtrEnable = true;
            comport.RtsEnable = true;
            // WHEN FINISHES TX FROM MACHINE DTR GOES LOW

            try
            {
                // Open the port
                comport.Open();
            }
            catch (UnauthorizedAccessException) { error = true; }
            catch (IOException) { error = true; }
            catch (ArgumentException) { error = true; }

            if (error)
                    MessageBox.Show(this, "Could not open the COM port.  Most likely it is already in use, has been removed, or is unavailable.", "COM Port Unavalible", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            else
            {
                // Show the initial pin states
                UpdatePinState();
                chkDTR.Checked = comport.DtrEnable;
                chkRTS.Checked = comport.RtsEnable;
            }
      }

      // Change the state of the form's controls
      EnableControls();

      // If the port is open, send focus to the send data box
      if (comport.IsOpen)
      {
        txtSendData.Focus();
        if (chkClearOnOpen.Checked) ClearTerminal();
      }
    }
    private void btnSend_Click(object sender, EventArgs e)
    { SendData(); }

    private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!comport.IsOpen) return;

            // This method will be called when there is data waiting in the port's buffer

            // Obtain the number of bytes waiting in the port's buffer
            int bytes = comport.BytesToRead;

            // Create a byte array buffer to hold the incoming data
            byte[] buffer = new byte[bytes];

            // Read the data from the port and store it in our buffer
            comport.Read(buffer, 0, bytes);

            // Show the user the incoming data in hex format
            Log(LogMsgType.Incoming, ByteArrayToHexString(buffer));
            //decodeMessage(buffer);
            //Log(LogMsgType.Incoming, "\n" + convertToBinary(buffer));
            //Log(LogMsgType.Incoming, "\n" + convertToInteger(buffer));
        }

    private void txtSendData_KeyDown(object sender, KeyEventArgs e)
    { 
      // If the user presses [ENTER], send the data now
      if (KeyHandled = e.KeyCode == Keys.Enter) { e.Handled = true; SendData(); } 
    }
    private void txtSendData_KeyPress(object sender, KeyPressEventArgs e)
    { e.Handled = KeyHandled; }
    #endregion

        private void chkDTR_CheckedChanged(object sender, EventArgs e)
        {
            comport.DtrEnable = chkDTR.Checked;
            if (chkDTR.Checked && chkClearWithDTR.Checked) ClearTerminal();
        }

        private void chkRTS_CheckedChanged(object sender, EventArgs e)
        {
            comport.RtsEnable = chkRTS.Checked;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearProcessedTerminal();
        }

        private void ClearTerminal()
        {
            rtfTerminal.Clear();
        }
        private void ClearProcessedTerminal()
        {
            rtfProcessedOutput.Clear();
        }
        private void tmrCheckComPorts_Tick(object sender, EventArgs e)
        {
            // checks to see if COM ports have been added or removed
            // since it is quite common now with USB-to-Serial adapters
            RefreshComPortList();
        }

        private void RefreshComPortList()
        {
            // Determain if the list of com port names has changed since last checked
            string selected = RefreshComPortList(cmbPortName.Items.Cast<string>(), cmbPortName.SelectedItem as string, comport.IsOpen);

            // If there was an update, then update the control showing the user the list of port names
            if (!String.IsNullOrEmpty(selected))
            {
                cmbPortName.Items.Clear();
                cmbPortName.Items.AddRange(OrderedPortNames());
                cmbPortName.SelectedItem = selected;
            }
        }

        private string[] OrderedPortNames()
        {
            // Just a placeholder for a successful parsing of a string to an integer
            int num;

            // Order the serial port names in numberic order (if possible)
            return SerialPort.GetPortNames().OrderBy(a => a.Length > 3 && int.TryParse(a.Substring(3), out num) ? num : 0).ToArray(); 
        }
        
        private string RefreshComPortList(IEnumerable<string> PreviousPortNames, string CurrentSelection, bool PortOpen)
        {
            // Create a new return report to populate
            string selected = null;

            // Retrieve the list of ports currently mounted by the operating system (sorted by name)
            string[] ports = SerialPort.GetPortNames();

            // First determain if there was a change (any additions or removals)
            bool updated = PreviousPortNames.Except(ports).Count() > 0 || ports.Except(PreviousPortNames).Count() > 0;

            // If there was a change, then select an appropriate default port
            if (updated)
            {
                // Use the correctly ordered set of port names
                ports = OrderedPortNames();

                // Find newest port if one or more were added
                string newest = SerialPort.GetPortNames().Except(PreviousPortNames).OrderBy(a => a).LastOrDefault();

                // If the port was already open... (see logic notes and reasoning in Notes.txt)
                if (PortOpen)
                {
                    if (ports.Contains(CurrentSelection)) selected = CurrentSelection;
                    else if (!String.IsNullOrEmpty(newest)) selected = newest;
                    else selected = ports.LastOrDefault();
                }
                else
                {
                    if (!String.IsNullOrEmpty(newest)) selected = newest;
                    else if (ports.Contains(CurrentSelection)) selected = CurrentSelection;
                    else selected = ports.LastOrDefault();
                }
            }

            // If there was a change to the port list, return the recommended default selection
            return selected;
        }
        
        private void cmbCommand_SelectedIndexChanged(object sender, EventArgs e)
        {
            byte[] messageToMachine = vendingParser.encodeMessage(txtMessageIndex.Text, cmbCommand.Text, txtData.Text);

            for (int i = 0; i < messageToMachine.Length; i++)
            {
                if (i == 0)
                    txtSendData.Text = String.Format("{0:x2}", messageToMachine[i]);
                else
                    txtSendData.Text += String.Format("{0:x2}", messageToMachine[i]);
            }
        }

        private void cmbCommand_TextUpdate(object sender, EventArgs e)
        {
            byte[] messageToMachine = vendingParser.encodeMessage(txtMessageIndex.Text, cmbCommand.Text, txtData.Text);

            //crcAscii = String.Format("{0:x2} ", crc).ToUpper();
            //asciiBytes = Encoding.ASCII.GetBytes(crcAscii);
            byte[] crcBytes = new byte[4];
            for (int i = 0; i < 4; i++)
                crcBytes[i] = messageToMachine[messageToMachine.Length - 4 + i];
            txtCRC.Text = System.Text.Encoding.ASCII.GetString(crcBytes);

            for (int i = 0; i < messageToMachine.Length; i++)
            {
                if (i == 0)
                    txtSendData.Text = String.Format("{0:x2}", messageToMachine[i]);
                else
                    txtSendData.Text += String.Format("{0:x2}", messageToMachine[i]);
            }
        }

        private void txtData_TextChanged(object sender, EventArgs e)
        {
            byte[] messageToMachine = vendingParser.encodeMessage(txtMessageIndex.Text, cmbCommand.Text, txtData.Text);

            for (int i = 0; i < messageToMachine.Length; i++)
            {
                if (i == 0)
                    txtSendData.Text = String.Format("{0:x2}", messageToMachine[i]);
                else
                    txtSendData.Text += String.Format("{0:x2}", messageToMachine[i]);
            }
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            byte[] dataToProcess = HexStringToByteArray(rtfTerminal.Text);

            int generalIndex = 0;

            for (generalIndex = 0; generalIndex < dataToProcess.Length; generalIndex++)
            {
                if (dataToProcess[generalIndex] == 0x02) //DATA START DETECTED
                {
                    //DATA SIZE
                    string dataSizeString = ((char)dataToProcess[generalIndex + 1]).ToString() +
                        ((char)dataToProcess[generalIndex + 2]).ToString() +
                        ((char)dataToProcess[generalIndex + 3]).ToString();
                    ushort dataSize = (ushort)Int16.Parse(dataSizeString);
                    dataSize += (ushort)9; //Adding 9 Bytes of protocol overhead

                    byte[] rawMessage = new byte[dataSize];
                    for (int i = 0; i < dataSize; i++)
                    {
                        rawMessage[i] = dataToProcess[generalIndex + i];
                    }
                    LogProcessed(LogMsgType.Incoming, "Processing: " + ByteArrayToHexString(rawMessage) + "\n");

                    string[] output = vendingParser.decodeMessage(rawMessage);
                    LogProcessed(LogMsgType.Outgoing, "Data size: " + output[0] + "\n");
                    LogProcessed(LogMsgType.Outgoing, "Command: " + output[1] + "\n");
                    LogProcessed(LogMsgType.Outgoing, "Data: " + output[2] + "\n");
                    LogProcessed(LogMsgType.Outgoing, "CRC: " + output[3] + "\n");
                    LogProcessed(LogMsgType.Normal, "------------------------------------\n");

                    generalIndex += dataSize;
                }
            }

            rtfTerminal.Text = "";

        }

        private void btnClearInput_Click(object sender, EventArgs e)
        {
            ClearTerminal();
        }
    }

    class vendingMachine : Control
    {
        public enum STATE { POWEROFF, POWERON, BUSY };
        public enum COMSTATE { IDLE, TRANSMIT, RECEIVE, BUSYONRECEIVE, BUSYONTRANSMIT };
        //private enum MACHINESTATE { DISCONNECTED, CONNECTED, SEND, RECEIVE, BUSYSEND, BUSYRECEIVE };

        private SerialPort comport = new SerialPort();

        private STATE state = STATE.POWEROFF;
        private COMSTATE comstate = COMSTATE.IDLE;
        //private MACHINESTATE machinestate = MACHINESTATE.DISCONNECTED;
        #region PROPERTIES
        public STATE State
        {
            get { return state; }
            //set { seconds = value * 3600; }
        }
        public COMSTATE ComState
        {
            get { return comstate; }
            //set { seconds = value * 3600; }
        }
        #endregion
        public vendingMachine()
        {
            // When data is recieved through the port, call this method
            comport.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            comport.PinChanged += new SerialPinChangedEventHandler(comport_PinChanged);
        }
        #region PIN_CHANGED
        void comport_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            // Show the state of the pins
            UpdatePinState();
        }
        private void UpdatePinState()
        {
            this.Invoke(new ThreadStart(() =>
            {
                //// Show the state of the pins
                ////chkCD.Checked = comport.CDHolding;
                ////chkCTS.Checked = comport.CtsHolding;
                ////chkDSR.Checked = comport.DsrHolding;
                //// WHEN FINISHES TX FROM MACHINE DSR GOES LOW
                //LogProcessed(LogMsgType.Incoming, "comport.DsrHolding = " + (comport.DsrHolding ? "True" : "False") + "\n");
            }));
        }
        #endregion
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // If the com port has been closed, do nothing
            if (!comport.IsOpen) return;

            // This method will be called when there is data waiting in the port's buffer

            // Obtain the number of bytes waiting in the port's buffer
            int bytes = comport.BytesToRead;

            // Create a byte array buffer to hold the incoming data
            byte[] buffer = new byte[bytes];

            // Read the data from the port and store it in our buffer
            comport.Read(buffer, 0, bytes);

            //// Show the user the incoming data in hex format
            //Log(LogMsgType.Incoming, ByteArrayToHexString(buffer));
            ////decodeMessage(buffer);
            ////Log(LogMsgType.Incoming, "\n" + convertToBinary(buffer));
            ////Log(LogMsgType.Incoming, "\n" + convertToInteger(buffer));
        }

        public int connect(string port)
        {
            bool error = false;

            // If the port is open, close it.
            if (comport.IsOpen)
                comport.Close();
            else
            {
                // Set the port's settings
                comport.BaudRate = 9600;
                comport.DataBits = 8;
                comport.StopBits = StopBits.One;
                comport.Parity = Parity.None;
                comport.PortName = port;
                comport.DtrEnable = true;
                comport.RtsEnable = true;
                // WHEN FINISHES TX FROM MACHINE DTR GOES LOW

                try
                {
                    // Open the port
                    comport.Open();
                }
                catch (UnauthorizedAccessException) { error = true; }
                catch (IOException) { error = true; }
                catch (ArgumentException) { error = true; }

                if (error)
                {
                    //MessageBox.Show(this, "Could not open the COM port.  Most likely it is already in use, has been removed, or is unavailable.", "COM Port Unavalible", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    error = true;
                }
            }

            if (error)
                return 1;
            return 0;
        }
        public int disconnect()
        {
            if (comport.IsOpen)
                comport.Close();
            return 0;
        }

        public int sendMessage(string command, string data)
        {

            return 0;
        }

        public int receiveData()
        {
            return 0;
        }
    }
    class vendingParser
    {
        static public ushort calc_crc(ushort crc, byte dat)
        {
            int i;
            long x;

            x = dat;
            x = (x << 16) + crc;
            for (i = 0; i < 8; i++)
            {
                if ((x & 0x01) != 0)
                    x = (x >> 1) ^ (long)0x8408;
                else
                    x = x >> 1;
            }
            crc = (ushort)((int)x);
            return (crc);
        }
        static public string[] decodeMessage(byte[] inputData)
        {
            string[] parsedMessage = new string[4];
            int i = 0;

            parsedMessage[0] = "Error";
            parsedMessage[1] = "Error";
            parsedMessage[2] = "Error";
            parsedMessage[3] = "Error";

            //CHECKSUM
            ushort calculatedCRC = 0;
            string calculatedCRCString = "";
            for (i = 1; i < inputData.Length - 4; i++)
            {
                calculatedCRC = calc_crc(calculatedCRC, inputData[i]);
            }
            calculatedCRCString = String.Format("{0:x2} ", calculatedCRC).ToUpper();
            string incommingCRCString = "";
            for (i = inputData.Length - 4; i < inputData.Length; i++)
                incommingCRCString += ((char)inputData[i]).ToString();
            parsedMessage[3] = calculatedCRCString;
            //Log(LogMsgType.Normal, String.Format("Calculated CRC: {0}\n", calculatedCRCString));
            //Log(LogMsgType.Normal, String.Format("Incomming CRC: {0}\n", incommingCRCString));
            //DATA SIZE
            string dataSizeString = ((char)inputData[1]).ToString() +
                ((char)inputData[2]).ToString() +
                ((char)inputData[3]).ToString();
            ushort dataSize = (ushort)Int16.Parse(dataSizeString);
            parsedMessage[0] = dataSize.ToString();
            //Log(LogMsgType.Normal, String.Format("Message size: {0}\n", dataSizeString));
            //Command INDEX
            int commandIndex = inputData[4];
            //Log(LogMsgType.Normal, String.Format("Command index: {0}\n", commandIndex));
            //Command
            string command = ((char)inputData[5]).ToString() +
                ((char)inputData[6]).ToString() +
                ((char)inputData[7]).ToString();
            parsedMessage[1] = command;
            //Log(LogMsgType.Normal, String.Format("Message: {0}\n", command));
            //Data
            string dataString = "";
            if (dataSize > 4) //There is actually payload
            {
                for (i = 0; i < dataSize - 1 - 3; i++) //3 for message size and 1 because of reasons
                    dataString += ((char)inputData[i + 8]).ToString();
                parsedMessage[2] = dataString;
                //Log(LogMsgType.Normal, String.Format("Data: [{0}]\n", dataString));
            }
            return parsedMessage;
        }
        static public byte[] encodeMessage(string index, string command, string data)
        {
            int i = 0;
            byte[] asciiBytes = null;

            int commandIndex = 0;

            ushort crc = 0;
            string crcAscii = "";

            int dataLenght = 1; //This is the lenght of the counter message byte
            string dataLenghtAccii = "";

            //Log(LogMsgType.Normal, String.Format("Application Started at {0}\n", DateTime.Now));
            dataLenght += command.Length;
            dataLenght += data.Length;
            dataLenghtAccii = dataLenght.ToString().PadLeft(3, '0');

            //MESSAGE TO MACHINE
            Byte[] messageToMachine = new Byte[dataLenght + 9];
            int messageToMachineIndex = 0;

            messageToMachine[messageToMachineIndex++] = 0x02;

            //Log(LogMsgType.Normal, String.Format("Data lenght: {0}\n", dataLenghtAccii));

            // SIZE
            asciiBytes = Encoding.ASCII.GetBytes(dataLenghtAccii);
            for (i = 0; i < asciiBytes.Length; i++)
            {
                //Log(LogMsgType.Normal, String.Format("dataLenghtAccii: char index = {0}, char = {1}, ascii = {2}, hex = {3:x2}\n", 
                //    i,
                //    asciiBytes[i].ToString(),
                //    asciiBytes[i],
                //    asciiBytes[i]));

                messageToMachine[messageToMachineIndex++] = asciiBytes[i];
            }

            //MESSAGE INDEX
            commandIndex = Int32.Parse(index); // Int32.Parse(txtMessageIndex.Text);
            //Log(LogMsgType.Normal, String.Format("Message index = {0}\n", messageIndex));
            messageToMachine[messageToMachineIndex++] = (byte)commandIndex;

            //COMMAND
            asciiBytes = Encoding.ASCII.GetBytes(command);
            for (i = 0; i < asciiBytes.Length; i++)
            {
                //Log(LogMsgType.Normal, String.Format("dataLenghtAccii: char index = {0}, char = {1}, ascii = {2}, hex = {3:x2}\n",
                //    i,
                //    (string)asciiBytes[i].ToString(),
                //    asciiBytes[i],
                //    asciiBytes[i]));

                messageToMachine[messageToMachineIndex++] = asciiBytes[i];
            }
            //DATA
            asciiBytes = Encoding.ASCII.GetBytes(data);
            for (i = 0; i < asciiBytes.Length; i++)
            {
                //Log(LogMsgType.Normal, String.Format("dataLenghtAccii: char index = {0}, char = {1}, ascii = {2}, hex = {3:x2}\n",
                //    i,
                //    asciiBytes[i].ToString(),
                //    asciiBytes[i],
                //    asciiBytes[i]));

                messageToMachine[messageToMachineIndex++] = asciiBytes[i];
            }

            //Finalize message
            messageToMachine[messageToMachineIndex++] = 0x03;

            //Calculate CRC
            for (i = 1; i < messageToMachine.Length - 4; i++)
            {
                crc = calc_crc(crc, messageToMachine[i]);
            }
            crcAscii = String.Format("{0:x2} ", crc).ToUpper();
            asciiBytes = Encoding.ASCII.GetBytes(crcAscii);
            for (i = 0; i < 4; i++)
                messageToMachine[messageToMachineIndex++] = asciiBytes[i];
            //txtCRC.Text = crcAscii;

            decodeMessage(messageToMachine);

            return messageToMachine;
        }
    }
    class vendingUtilities
    {
        static public void getBinary(string msg)
        {
            for (int i = 0; i < msg.Length; i += 2)
            {

            }
        }
        static public byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }
        static public string ByteArrayToHexString(byte[] data)
        {
            int count = 0;
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
            {
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
                count++;
                //if ((count % 2) == 0)
                //    sb.Append("\n");
            }
            return sb.ToString().ToUpper();
        }
        static public string convertToBinary(byte[] data)
        {
            int count = 0;
            //storage for the resulting string
            string result = string.Empty;
            //iterate through the byte[]
            foreach (byte value in data)
            {
                //storage for the individual byte
                string binarybyte = Convert.ToString(value, 2);
                //if the binarybyte is not 8 characters long, its not a proper result
                while (binarybyte.Length < 8)
                {
                    //prepend the value with a 0
                    binarybyte = "0" + binarybyte;
                    //if (binarybyte.Length % 4 == 0)
                    //    binarybyte = binarybyte + "|";
                }
                //append the binarybyte to the result
                result += binarybyte + " ";
                count++;
                if ((count % 2) == 0)
                    result = result + "\n";
            }
            //return the result
            return result;
        }
        static public string convertToInteger(byte[] data)
        {
            int count = 0;
            //storage for the resulting string
            string result = string.Empty;
            //iterate through the byte[]
            foreach (byte value in data)
            {
                ;
                //append the binarybyte to the result
                result += value.ToString().PadLeft(4, '0') + " ";
                count++;
                if ((count % 2) == 0)
                    result = result + "\n";
            }
            //return the result
            return result;
        }
    }
}