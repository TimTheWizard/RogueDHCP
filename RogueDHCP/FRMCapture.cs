using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PacketDotNet;
using SharpPcap;
using System.Collections.Concurrent;
using SharpPcap.WinPcap;
using NetFwTypeLib;
using System.Text.RegularExpressions;
using SharpPcap.LibPcap;
using System.Net.NetworkInformation;
using System.Threading;
using System.Net;

namespace PacketCapture
{
    public partial class FRMCapture : Form
    {
        CaptureDeviceList devices;
        public static ICaptureDevice device;
        public static string rawPacketData = "";
        public static List<string> ipList = new List<string>();
        public static int UDP = 0;
        public static int TCP = 0;
        //the address of the local box
        public static string localIp;
        //the mac of the local box
        public static PhysicalAddress localMAC;
        private PcapAddress Address;
        static long numPackets = 0;
        FRMSend fSend;

        public FRMCapture()
        {
            InitializeComponent();
            devices = CaptureDeviceList.Instance;
            if (devices.Count < 1)
            {
                MessageBox.Show("Error, no Capture Devices Found");
                Application.Exit();
            }
            foreach (ICaptureDevice dev in devices)
            {
                cmbDevices.Items.Add(dev.Description);
            }
        }
        private static void device_OnPacketArrival(object sender, CaptureEventArgs args) {
            
                byte[] data = args.Packet.Data;
                int byteCounter = 0;
                foreach (byte b in data)
                {
                    byteCounter++;
                    //add byte to sting in hex
                    rawPacketData += b.ToString("X2");
                }
                //MAC
                //first 8 are destination
                //second 8 are source
                //udp = 11 on 24th byte
                //tcp = 06 on 24th byte
                //IP
                //sorce=27-30
                //destination = 31-34

                //0806 arp
                //
                string type = "";
                string sourceIp = "";
                string destinationIp = "";
                string sourceMac = "";
                string destinationMac = "";
            //mac destination
                for (int i = 0; i < 6; i++)
                    destinationMac += rawPacketData[i * 2] + "" + rawPacketData[i * 2 + 1];
            //mad source
                for (int i = 8; i < 12; i++)
                    sourceMac += rawPacketData[i * 2] + "" + rawPacketData[i * 2 + 1];
            //get ethernet type
                for (int i = 12; i <= 13; i++)
                    type += rawPacketData[i * 2] + "" + rawPacketData[i * 2 + 1];
                switch (type)
                {
                    case "0800":
                        type = "(IP)";
                        break;
                    case "0806":
                        type = "(ARP)";
                        break;
                }
                //Console.WriteLine(type);
                //eval source
                //Console.WriteLine();
                if (type == "(IP)")
                {
                    //get source ip
                    for (int i = 26; i <= 29; i++)
                    {
                        sourceIp+=Convert.ToInt32((rawPacketData[i * 2] +""+ rawPacketData[i * 2 + 1]), 16);
                        if (i != 29)
                            sourceIp += ".";
                    }
                    //get destination ip  31-34
                    for (int i = 30; i <= 33; i++)
                    {
                        destinationIp += Convert.ToInt32((rawPacketData[i * 2] + "" + rawPacketData[i * 2 + 1]), 16);
                        if (i != 33)
                            destinationIp += ".";
                    }                                        
                }
                else if (type == "(ARP)")
                {

                    var arpOp = rawPacketData[20 * 2] + "" + rawPacketData[20 * 2 + 1] + " " + rawPacketData[21 * 2] + "" + rawPacketData[21 * 2 + 1];
                    for (int i = 28; i <= 31; i++)
                    {
                        sourceIp += Convert.ToInt32((rawPacketData[i * 2] + "" + rawPacketData[i * 2 + 1]), 16);
                        if (i != 31)
                            sourceIp += ".";
                    }
                    for (int i = 38; i <= 41; i++)
                    {
                        destinationIp += Convert.ToInt32((rawPacketData[i * 2] + "" + rawPacketData[i * 2 + 1]), 16);
                        if (i != 41)
                            destinationIp += ".";
                    }
                    if (destinationIp == localIp)
                    {
                        if (!ipList.Contains(sourceIp))
                            ipList.Add(sourceIp);
                        Console.WriteLine(sourceIp + " Replied");
                    }
                }
                //Console.WriteLine();
            /*
                capturedData += Environment.NewLine
                    + "Source IP: "+sourceIp + Environment.NewLine
                    + "Destination MAC: " + destinationMac + Environment.NewLine
                    + "Source MAC: " + sourceMac + Environment.NewLine
                    + "EtherType: " + type + Environment.NewLine;
            */
                rawPacketData = "";
        }
        private void updateTable()
        {            
            int temp1=dataGridView1.FirstDisplayedScrollingRowIndex;
            ipList.Sort();
            var ips = ipList.ToArray();
            dataGridView1.Rows.Clear();
            foreach (var ip in ips)
            {
                bool temp = dataGridView1.Columns.Contains(ip);
                dataGridView1.Rows.Add(ip);
            }
            dataGridView1.FirstDisplayedScrollingRowIndex = temp1;
        }
        private void btnStartStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnStartStop.Text == "Start") {
                    try
                    {                        
                        regDevice();
                        int readTimeoutMilliseconds = 1000;
                        device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);
                        //pull address info for the nic... probably not the best way to do this, but it works so far....
                        Address = ((WinPcapDevice)device).Addresses.FirstOrDefault(x => x.Addr.ipAddress != null && (x.Addr.ipAddress + "").Length <= 15);
                        localMAC = ((WinPcapDevice)device).Addresses.FirstOrDefault(x => x.Addr.hardwareAddress != null).Addr.hardwareAddress;
                        localIp = Address.Addr.ipAddress.ToString();

                        var name =device.Description;
                        device.StartCapture();
                        timer1.Enabled = true;
                        btnStartStop.Text = "Stop";
                        cmbDevices.Enabled = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                else
                {
                    try
                    {
                        timer1.Enabled = false;
                        try
                        {
                            device.StopCapture();
                        }
                        catch (PcapException pex)
                        {
                            Console.WriteLine(pex.Message);
                        }
                        device.Close();
                        btnStartStop.Text = "Start";
                        cmbDevices.Enabled = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void regDevice()
        {
            device = devices.Where(x => x.Description == cmbDevices.SelectedItem.ToString()).FirstOrDefault();
            device.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(device_OnPacketArrival);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {            
            updateTable();
        }

        private void SelectedIndexChange(object sender, EventArgs e)
        {
            regDevice();
            clearScreen();
        }

        private void clearScreen()
        {            
            numPackets = 0;
            ipList.Clear();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = "Text Files|*.txt|All Files|*.*";
            saveFileDialog1.Title = "Save the Captured Packets";
            saveFileDialog1.ShowDialog();

            if(saveFileDialog1.FileName!="")
            {
                //System.IO.File.WriteAllText(saveFileDialog1.FileName, txtCapturedData.Text);
            }
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "Text Files|*.txt|All Files|*.*";
            openFileDialog1.Title = "Open Previously Captured Packets";
            openFileDialog1.ShowDialog();

            if (openFileDialog1.FileName != "")
            {
                //txtCapturedData.Text=System.IO.File.ReadAllText(openFileDialog1.FileName);
            }
        }

        private void sendWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FRMSend.instantiations < 3)
            {
                fSend = new FRMSend();
                fSend.Show();
            }
        }
        private static string selectedIp;

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ipList.Clear();
        }

        public void sendPacket(string bytesToSend)
        {
            //must be Hex
            bytesToSend=bytesToSend ?? "";
            //convert to byte array
            string[] bytes = bytesToSend.Split(new string[] { " ", "\n", "\r\n", "\t" }, StringSplitOptions.RemoveEmptyEntries);

            byte[] packet = new byte[bytes.Length];
            int i = 0;
            foreach (string s in bytes)
            {
                packet[i] = Convert.ToByte(s, 16);
                i++;
            }
            try
            {
                FRMCapture.device.SendPacket(packet);
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message); }
        }
        Task<PingReply> PingAsync(string address)
        {
            var tcs = new TaskCompletionSource<PingReply>();
            Ping ping = new Ping();
            ping.PingCompleted += (obj, sender) =>
            {
                if(sender.Reply.Status==IPStatus.Success)
                    dataGridView1.Rows.Add(sender.Reply.Address);
                tcs.SetResult(sender.Reply);
            };
            ping.SendAsync(address, new object());
            return tcs.Task;
        }

        Task<string> ARPAsync(string address)
        {
            var tcs = new TaskCompletionSource<string>();
            ARP(IPAddress.Parse(address));
            tcs.SetResult(address);
            return tcs.Task;
        }
        private void scanNetworkToolStripMenuItem_Click(object sender, EventArgs e)
        {
                        
        }
        private List<string> gennerateIPRange()
        {
            List<string> range = new List<string>();
            var netmask = Address.Netmask;
            string[] ipparts = localIp.Split('.');
            string[] netparts = netmask.ipAddress.ToString().Split('.');
            string ipBinary = "";
            string netBinary = "";
            foreach (string oct in ipparts)
            {
                string temp =Convert.ToString(Convert.ToInt32(oct), 2);
                while (temp.Length < 8)
                {
                    temp = "0" + temp;
                }
                ipBinary += temp;
            }
            foreach (string oct in netparts)
            {
                string temp = Convert.ToString(Convert.ToInt32(oct), 2);
                while (temp.Length < 8)
                {
                    temp = "0" + temp;
                }
                netBinary += temp;
            }
            int net = 1 + netBinary.LastIndexOf('1');
            string root = ipBinary.Substring(0, net);
            string rootMin = root, rootMax = root;
            for (int length = rootMin.Length; length < 32; length++)
            {
                rootMin += "0";
                rootMax += "1";
            }
            int[] rootMinparts = Enumerable.Range(0, rootMin.Length / 8).Select(i => Convert.ToInt32(rootMin.Substring(i * 8, 8), 2)).ToArray<int>();
            int[] rootMaxparts = Enumerable.Range(0, rootMax.Length / 8).Select(i => Convert.ToInt32(rootMax.Substring(i * 8, 8),2)).ToArray<int>();
            int oct1 = rootMinparts[0], oct2 = rootMinparts[1], oct3 = rootMinparts[2], oct4 = rootMinparts[3];
            do
            {
                oct2 = rootMinparts[1];
                do
                {
                    oct3 = rootMinparts[2];
                    do
                    {
                        oct4 = rootMinparts[3];
                        do
                        {
                            range.Add(oct1 + "." + oct2 + "." + oct3 + "." + oct4);
                            oct4++;
                        } while (oct4 <= rootMaxparts[3]);
                        oct3++;
                    } while (oct3 <= rootMaxparts[2]);
                    oct2++;
                } while (oct2 <= rootMaxparts[1]);
                oct1++;
            } while(oct1 <= rootMaxparts[0]);
            range.RemoveAt(0);
            range.RemoveAt(range.Count-1);
            return range;
        }
        public void ARP(IPAddress ipAddress)
        {
            if (ipAddress == null)
                throw new Exception("ARP IP address Cannot be null");
            var ethernetPacket = new PacketDotNet.EthernetPacket(localMAC, PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF"), PacketDotNet.EthernetPacketType.Arp);
            
            var arpPacket = new PacketDotNet.ARPPacket(PacketDotNet.ARPOperation.Request, PhysicalAddress.Parse("00-00-00-00-00-00"), ipAddress, localMAC, Address.Addr.ipAddress);
            ethernetPacket.PayloadPacket = arpPacket;

            device.SendPacket(ethernetPacket);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var list = gennerateIPRange();
            List<Task<PingReply>> pingTasks = new List<Task<PingReply>>();
            foreach (var address in list)
            {
                pingTasks.Add(PingAsync(address));
            }
        }

        private void aRPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var list = gennerateIPRange();
            List<Task<string>> arpTasks = new List<Task<string>>();
            foreach (var address in list)
            {
                arpTasks.Add(ARPAsync(address));
            }
        }
    }
}
