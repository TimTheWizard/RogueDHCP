using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RogueDHCP
{
    public partial class FRMSend : Form
    {
        public static int instantiations = 0;
        public FRMSend()
        {
            InitializeComponent();
            instantiations++;
        }


        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Text Files|*.txt|All Files|*.*";
            openFileDialog1.Title = "Open Previously Captured Packets";
            openFileDialog1.ShowDialog();

            if (openFileDialog1.FileName != "")
            {
                txtPacket.Text = System.IO.File.ReadAllText(openFileDialog1.FileName);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Text Files|*.txt|All Files|*.*";
            saveFileDialog1.Title = "Save the Captured Packets";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                System.IO.File.WriteAllText(saveFileDialog1.FileName, txtPacket.Text);
            }
        }

        private void FRMSend1_FormClosed(object sender, FormClosedEventArgs e)
        {
            instantiations--;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            /*
            string bytesToSend = "";
            //get Hex from the text
            foreach (string s in txtPacket.Lines)
            {
                //No Comments (after # char)
                bytesToSend +=s.Split('#')[0]+Environment.NewLine;
            }
            //convert to byte array
            string[] bytes = bytesToSend.Split(new string[]{" ", "\n", "\r\n", "\t"}, StringSplitOptions.RemoveEmptyEntries);

            byte[] packet = new byte[bytes.Length];
            int i = 0;
            foreach (string s in bytes) 
            { 
                packet[i] = Convert.ToByte(s, 16); 
                i++;
            }
             * */
            try
            {
                RogueDHCP.DHCP offer = RogueDHCP.DHCP.DHCPOffer("D8CB8A61FCEB", "8DA5D5CD", "28A1475F", "00000000", "D8CB8A61FCEB", "FFFFF800", "8DA5D001", "00000e10", "08080808", "08080606");
                //RogueDHCP.DHCP offer = RogueDHCP.DHCP.DHCPOffer(FRMCapture.localMAC.ToString(), FRMCapture.ConvertIpToHex(FRMCapture.localIp), "33553355", FRMCapture.ConvertIpToHex("192.168.200.200"), FRMCapture.localMAC.ToString(), FRMCapture.ConvertIpToHex(FRMCapture.subnet), FRMCapture.ConvertIpToHex(FRMCapture.gateway), "00000e10", FRMCapture.ConvertIpToHex(FRMCapture.gateway));
                //RogueDHCP.DHCP offer = RogueDHCP.DHCP.DHCPACK(FRMCapture.localMAC.ToString(), FRMCapture.ConvertIpToHex(FRMCapture.localIp), "33553355", FRMCapture.ConvertIpToHex("192.168.200.200"), FRMCapture.localMAC.ToString(), FRMCapture.ConvertIpToHex(FRMCapture.subnet), FRMCapture.ConvertIpToHex(FRMCapture.gateway), "00000e10", FRMCapture.ConvertIpToHex(FRMCapture.gateway));
                //RogueDHCP.DHCP offer = RogueDHCP.DHCP.DHCPNACK(FRMCapture.localMAC.ToString(), FRMCapture.ConvertIpToHex(FRMCapture.localIp), "33553355", FRMCapture.localMAC.ToString(), FRMCapture.ConvertIpToHex("192.168.200.200"), FRMCapture.ConvertIpToHex(FRMCapture.gateway), "00000e10");
                FRMCapture.device.SendPacket(offer.GetPacket());
            }
            catch(Exception ex)
            { MessageBox.Show(ex.Message); }
        }
    }
}
