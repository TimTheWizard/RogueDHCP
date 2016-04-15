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
using RogueDHCP;

namespace RogueDHCP
{
    public partial class FRMCapture : Form
    {
        CaptureDeviceList devices;
        public static ICaptureDevice device;
        //public static string rawPacketData = "";
        //ip, mac, time it will expire
        //public static List<Tuple<string, string, DateTime>> ipList = new List<Tuple<string, string, DateTime>>();
        //public static List<string> possibleAddresses = new List<string>();
        private static IPTables ipLists;
        private static Settings settings = new Settings();
        private static bool DHCPisActive = false;
        public static int UDP = 0;
        public static int TCP = 0;
        //the address of the local box
        public static string localIp;
        private static string lastPacket="";
        //the mac of the local box
        public static PhysicalAddress localMAC;
        private PcapAddress Address;

        public FRMCapture()
        {
            InitializeComponent();
            devices = CaptureDeviceList.Instance;
            if (devices==null || devices.Count < 1)
            {
                MessageBox.Show("Error, no Capture Devices Found");
                Application.Exit();
            }
            return;
            foreach (ICaptureDevice dev in devices)
            {
                cmbDevices.Items.Add(dev.Description);
            }
        }
        public static string ConvertIpToHex(string ip)
        {
            string[] parts = ip.Split('.');
            int[] intParts = new int[4];
            string[] macAddress = new string[4];
            if (parts.Length == 4)
            {
                bool valid = true;
                for (int i = 0; i < parts.Length; i++)
                {
                    valid = valid && Int32.TryParse(parts[i], out intParts[i]);
                    if (valid)
                    {
                        valid = (intParts[i] <= 255 && intParts[i] >= 0);
                    }
                    if (valid)
                    {
                        macAddress[i] = intParts[i].ToString("X");
                        if (macAddress[i].Length < 2)
                        {
                            macAddress[i] = "0" + macAddress[i];
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                if (valid)
                {
                    return macAddress[0] + macAddress[1] + macAddress[2] + macAddress[3];
                }
                return "ERROR";
            }
            return "ERROR";
        }
        public static string ConvertHexIpToStandard(string ip)
        {
            if (ip.Length < 8)
            {
                throw new Exception("Invalid IP to convert to Hex");
            }
            return Convert.ToInt32(ip.Substring(0, 2), 16) + "." + Convert.ToInt32(ip.Substring(2, 2), 16) + "." + Convert.ToInt32(ip.Substring(4, 2), 16) + "." + Convert.ToInt32(ip.Substring(6, 2), 16);
        }
        private static void device_OnPacketArrival(object sender, CaptureEventArgs args) {
            
                byte[] data = args.Packet.Data;
                int byteCounter = 0;
                var rawPacketData = "";
                foreach (byte b in data)
                {
                    byteCounter++;
                    //add byte to sting in hex
                    rawPacketData += b.ToString("X2");
                }
                if (rawPacketData == lastPacket)
                    return;
                lastPacket = rawPacketData;
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
            //get ethernet type
                for (int i = 12; i <= 13; i++)
                    type += rawPacketData[i * 2] + "" + rawPacketData[i * 2 + 1];

                if (type == "0800")// an ip packet
                {
                    //is it udp (11)
                    if (rawPacketData[23 * 2].ToString() + rawPacketData[23 * 2 + 1].ToString() == "11" && rawPacketData.Length > 282 * 2)
                    {
                        //check for magic cookie
                        if (DHCPisActive && "63825363"==rawPacketData[278 * 2].ToString() + rawPacketData[278 * 2 + 1].ToString() 
                            + rawPacketData[279 * 2].ToString() + rawPacketData[279 * 2 + 1].ToString() 
                            + rawPacketData[280 * 2].ToString() + rawPacketData[280 * 2 + 1].ToString() 
                            + rawPacketData[281 * 2].ToString() + rawPacketData[281 * 2 + 1].ToString())
                        {
                            //we have a DHP packet.... we need tp parse it
                            //might be a bad assumption, but its option 53 (35 in hex) and it is usualy the first one after the magic cookie
                            if (rawPacketData[282 * 2].ToString() + rawPacketData[282 * 2 + 1].ToString() == "35")
                            {
                                switch (rawPacketData[284 * 2].ToString() + rawPacketData[284 * 2 + 1].ToString())
                                {
                                    case "01":
                                        //DHCP Discover
                                        RogueDHCP.DHCP dhcp = new RogueDHCP.DHCP(ConvertIpToHex(localIp), localMAC.ToString(), settings.leaseTime.ToString("X8"), FRMCapture.ConvertIpToHex(settings.subnet), FRMCapture.ConvertIpToHex(settings.gateway));
                                        dhcp.DHCPDiscover(rawPacketData);
                                        //check to see if they requested an IP address, if so, try and give it to them
                                        string requestedIp;
                                        if (dhcp.TryGetOption("32", out requestedIp) && !ipLists.isAvailable(ConvertHexIpToStandard(requestedIp)))
                                        {
                                            device.SendPacket(dhcp.DHCPOffer(ConvertIpToHex(requestedIp), settings.GetDnsHex()).GetPacket());
                                        }
                                        else
                                        {
                                            var offer = dhcp.DHCPOffer(ConvertIpToHex(ipLists.GetAvalible().First()), settings.GetDnsHex());
                                            device.SendPacket(offer.GetPacket());
                                        }
                                        break;
                                    case "02":
                                        //DHCP Offer

                                        break;
                                    case "03":
                                        //DHCP Request

                                        //Make DHCP object from data
                                        RogueDHCP.DHCP dhcpRequest = new RogueDHCP.DHCP(ConvertIpToHex(localIp), localMAC.ToString(), settings.leaseTime.ToString("X8"), FRMCapture.ConvertIpToHex(settings.subnet), FRMCapture.ConvertIpToHex(settings.gateway));
                                        dhcpRequest.DHCPRequest(rawPacketData);//sets other information derived from the packet amd check for options
                                        if (dhcpRequest.TryGetOption("32", out requestedIp))
                                        {
                                            // the ipAdress is avalible or the ipAddress is being used by the mac already
                                            if (ipLists.isAvailable(ConvertHexIpToStandard(requestedIp)) || ipLists.WhoHas(ConvertHexIpToStandard(requestedIp)) == dhcpRequest.GetClientMAC())
                                            {
                                                var ack = dhcpRequest.DHCPACK(requestedIp);
                                                device.SendPacket(ack.GetPacket());
                                                ipLists.reserveIp(ConvertHexIpToStandard(requestedIp), ack.GetClientMAC(), DateTime.Now.AddSeconds(Convert.ToInt32(ack.GetLeaseTime(),16)));
                                            }
                                            else
                                                device.SendPacket(dhcpRequest.DHCPNACK().GetPacket());
                                        }
                                        else if (ipLists.WhoHas(ConvertHexIpToStandard(dhcpRequest.targetIp)) == dhcpRequest.GetClientMAC())
                                        {
                                            var ack = dhcpRequest.DHCPACK(dhcpRequest.targetIp);
                                            device.SendPacket(ack.GetPacket());
                                            ipLists.reserveIp(ConvertHexIpToStandard(dhcpRequest.targetIp), ack.GetClientMAC(), DateTime.Now.AddSeconds(Convert.ToInt32(ack.GetLeaseTime(), 16)));
                                        }
                                    break;
                                    case "04":
                                        //DHCP Decline
                                        break;
                                    case "05":
                                        //DHCP ACK (server-side, will probably ignore [May want to scrape to know what address was assigned so we know not to assign and how long it was assigned)
                                        break;
                                    case "06":
                                        //DHCP NACK (server-side, will probably ignore)
                                        break;
                                    case "07":
                                        //DHCP Release
                                        break;
                                    case "08":
                                        //DHCP Inform
                                        break;
                                    default:
                                        MessageBox.Show("Unknown dhcp packet");
                                        break;
                                }
                            }
                        }
                    }                                   
                }
                else if (type == "0806") //an arp packet
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
                        //if we get an arp back from an ip we thought was avalible...
                        if (ipLists!=null && ipLists.isAvailable(sourceIp))
                        {
                            //mac source
                            string sourceMac="";
                            for (int i = 6; i < 12; i++)
                                sourceMac += rawPacketData[i * 2] + "" + rawPacketData[i * 2 + 1];
                            //the mac, and the time it will expire We dont know, so just put the max
                            ipLists.reserveIp(sourceIp, sourceMac, DateTime.MaxValue);
                        }
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
            if (ipLists!=null && ipLists.isUpdated())
            {
                int temp1 = dataGridView1.FirstDisplayedScrollingRowIndex;
                var ips = ipLists.GetIPsInUse();
                dataGridView1.Rows.Clear();
                foreach (var ip in ips)
                {
                    //just add in the ip address for now, may latter add in the mac and date expiring
                    dataGridView1.Rows.Add(ip.Item1, ip.Item2, ip.Item3);
                }
                if (dataGridView1.RowCount < temp1)
                    temp1 = dataGridView1.RowCount - 1;
                dataGridView1.FirstDisplayedScrollingRowIndex = temp1;
            }
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
                        string file = getFileSafeNameWithDefaultPath(device.Description);
                        bool loadSettingsSuccess = Settings.TryLoad(file, out settings);
                        if (!loadSettingsSuccess)
                        {
                            //load settings default
                            settings.NICName = device.Description;
                            settings.subnet = Address.Netmask.ToString();
                            var devices = NetworkInterface.GetAllNetworkInterfaces();
                            foreach (var nic in devices)
                            {
                                if (settings.gateway == "0.0.0.0")
                                    foreach (var addressProperties in nic.GetIPProperties().UnicastAddresses)
                                    {
                                        if (addressProperties.Address.ToString() == localIp)
                                        {
                                            settings.gateway = nic.GetIPProperties().GatewayAddresses.First().Address.ToString();
                                            break;
                                        }
                                    }
                            }
                            if (settings.gateway == "0.0.0.0")
                                MessageBox.Show("Error extracting Gateway Address");
                            //save settings
                            settings.Serialize(file);
                        }
                        var name =device.Description;
                        device.StartCapture();
                        timer1.Enabled = true;
                        btnStartStop.Text = "Stop";
                        cmbDevices.Enabled = false;
                        updateSettingsView();
                        dataGridView1.Visible = true;
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
                        dataGridView1.Visible = false;
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
            killExpiredIpLeases();
            updateTable();
        }
        //nuke all the ip leases that have expired
        private void killExpiredIpLeases()
        {
            if(ipLists!=null)
                ipLists.updateLists(DateTime.Now);
        }

        private void SelectedIndexChange(object sender, EventArgs e)
        {
            regDevice();
            clearScreen();
        }

        private void clearScreen()
        {         
            if(ipLists!=null)
                ipLists.Reset();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "XML Files | *.xml | All Files | *.*";
            saveFileDialog1.FileName = getFileSafeName(settings.NICName.Length>0? settings.NICName: "Settings");
            saveFileDialog1.Title = "Save the Captured Packets";
            saveFileDialog1.ShowDialog();

            if(saveFileDialog1.FileName!="")
            {
                string[] fileParts = saveFileDialog1.FileName.Split('\\');
                settings.NICName = fileParts[fileParts.Length - 1].Replace(".xml","");
                settings.Serialize(saveFileDialog1.FileName);
            }
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "XML Files|*.xml|All Files|*.*";
            openFileDialog1.Title = "Open Previously Captured Packets";
            openFileDialog1.ShowDialog();

            if (openFileDialog1.FileName != "")
            {
                try
                {
                    settings = Settings.Deserialize(openFileDialog1.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid or corrupted Settings file");
                }
            }
        }

        private static string selectedIp;

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ipLists != null)
                ipLists.Reset();
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
        
        public void ARP(IPAddress ipAddress)
        {
            if (ipAddress == null)
                throw new Exception("ARP IP address Cannot be null");
            var ethernetPacket = new PacketDotNet.EthernetPacket(localMAC, PhysicalAddress.Parse("FF-FF-FF-FF-FF-FF"), PacketDotNet.EthernetPacketType.Arp);
            
            var arpPacket = new PacketDotNet.ARPPacket(PacketDotNet.ARPOperation.Request, PhysicalAddress.Parse("00-00-00-00-00-00"), ipAddress, localMAC, Address.Addr.ipAddress);
            ethernetPacket.PayloadPacket = arpPacket;

            device.SendPacket(ethernetPacket);
        }

        private void pingStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //possibleAddresses = IPTables.gennerateIPRange(localIp, localMAC.ToString());
                ipLists = new IPTables(localIp, settings.subnet);
                var list = ipLists.GetAvalible();
                List<Task<PingReply>> pingTasks = new List<Task<PingReply>>();
                foreach (var address in list)
                {
                    pingTasks.Add(PingAsync(address));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void aRPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //possibleAddresses = IPTables.gennerateIPRange(localIp, localMAC.ToString());
                ipLists = new IPTables(localIp, settings.subnet);
                var list = ipLists.GetAvalible();
                List<Task<string>> arpTasks = new List<Task<string>>();
                foreach (var address in list)
                {
                    arpTasks.Add(ARPAsync(address));
                }
            }
            catch (NullReferenceException nul)
            {
                MessageBox.Show("NIC must be selected and started");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (DHCPisActive)
            {
                button1.Text = "Turn on DHCP";
                DHCPisActive = false;
            }
            else
            {
                button1.Text = "Turn off DHCP";
                DHCPisActive = true;
            }
        }

        private void FRMCapture_FormClosed(object sender, FormClosedEventArgs e)
        {
            if(device!=null && device.Started)
                device.Close();
        }

        private void tabView_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if(device==null || !device.Started)
            {
                e.Cancel=true;
                MessageBox.Show("Select NIC First and Start");
            }
        }
        private void updateSettingsView()
        {
            var dns = settings.GetDns();
            if(dns.Length>0)
                textBoxDNS1.Text = settings.dns[0];
            if (dns.Length > 1)
                textBoxDNS2.Text = settings.dns[1];
            textBoxLeaseTime.Text = settings.leaseTime.ToString();
            textBoxSubnet.Text = settings.subnet;
            textGateway.Text = settings.gateway;
            labelNIC.Text = settings.NICName;
        }

        private void textBoxLeaseTime_TextChanged(object sender, EventArgs e)
        {
            try{
                settings.leaseTime=Convert.ToInt32(textBoxLeaseTime.Text);
            }catch(Exception ex){
                MessageBox.Show("Invalid Lease Time");
            }
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            try
            {
                string path = getFileSafeNameWithDefaultPath(settings.NICName);
                settings.Serialize(path);
                MessageBox.Show("Settings Saved");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.StackTrace);

            }
        }

        private string getFileSafeNameWithDefaultPath(string nICName)
        {
      
            string pattern = "\\W";
            string replacement = "DT3";//random replacement
            Regex rgx = new Regex(pattern);
            string result = @"settings\"+rgx.Replace(nICName.Trim(), replacement);
            return result + ".xml";
        }

        private string getFileSafeName(string nICName)
        {

            string pattern = "\\W";
            string replacement = "DT3";//random replacement
            Regex rgx = new Regex(pattern);
            return rgx.Replace(nICName.Trim(), replacement) + ".xml";
        }

        private void textGateway_TextChanged(object sender, EventArgs e)
        {
            if (IPTables.validIp(textGateway.Text))
            {
                settings.gateway = textGateway.Text;
                string path = getFileSafeNameWithDefaultPath(settings.NICName);
                settings.Serialize(path);
            }
        }

        private void textBoxSubnet_TextChanged(object sender, EventArgs e)
        {
            if (IPTables.validIp(textBoxSubnet.Text))
            {
                settings.subnet = textBoxSubnet.Text;
                string path = getFileSafeNameWithDefaultPath(settings.NICName);
                settings.Serialize(path);
            }
        }

        private void textBoxDomainName_TextChanged(object sender, EventArgs e)
        {
            settings.domainName = textBoxDomainName.Text;
            string path = getFileSafeNameWithDefaultPath(settings.NICName);
            settings.Serialize(path);
        }

        private void textBoxDNS1_TextChanged(object sender, EventArgs e)
        {

            if (IPTables.validIp(textBoxDNS1.Text))
            {
                settings.setDNS1(textBoxDNS1.Text);
                string path = getFileSafeNameWithDefaultPath(settings.NICName);
                settings.Serialize(path);
            }
        }

        private void textBoxDNS2_TextChanged(object sender, EventArgs e)
        {
            if (IPTables.validIp(textBoxDNS2.Text))
            {
                settings.setDNS2(textBoxDNS2.Text);
                string path = getFileSafeNameWithDefaultPath(settings.NICName);
                settings.Serialize(path);
            }
        }
    }
}
