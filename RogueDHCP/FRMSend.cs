﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PacketCapture
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
            try
            {
                FRMCapture.device.SendPacket(packet);
            }
            catch(Exception ex)
            { MessageBox.Show(ex.Message); }
        }
    }
}