﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueDHCP
{
    //basic structure of a DHCP Packet (will need to look up specifices for different types but the basic headers will remain the same)
    /*
     *
     * #DHCP Request
        [0,1000]
        <DHCP REQUEST>{

        #Ethernet Frame ---------------------------------------

        ff ff ff ff ff ff        #Destination address-broadcast
        98 90 96 D2 3E 62        #Source address
        08 00                    #Type = IP

        #IP packet --------------------------------------------

        45                       #IP Version 4 packet header is 5x4 = 20 bytes
        00                       #Differentiated service field (not sure what this one is!)
        01 4d                    #Total length of IP packet (including payload)
        00 00                    #Identification
        00                       #Flags
        00                       #Fragment offset
        80                       #Time to live
        11                       #Protocol (17=UDP)
        39 a1                    #Header checksum
        00 00 00 00              #Source IP (do not need - response is broadcast)
        ff ff ff ff              #Destination IP (broadcast)

        #UDP packet ------------------------------------------

        00 44                    #Source port
        00 43                    #Destination port
        01 39                    #Length (including header)
        7d e4                    #Checksum

        #DHCP packet -----------------------------------------

        01                       #Message type (1=bootstrap request)
        01                       #Hardware type (1=Ethernet)
        06                       #Hardware address length (MAC=6 bytes)
        00                       #Number of hops to get to the DHCP server
        7e 6b e9 1b              #Transaction ID
        00 00                    #Seconds elapsed 
        80 00                    #Bootp flags
        00 00 00 00              #Client IP address
        00 00 00 00              #Your IP address
        00 00 00 00              #Backup (Next) server IP address
        00 00 00 00              #Relay agent IP address  
        00 0b db 40 ce 33        #Client MAC address
        00 00 00 00 00           #Client hardware address padding
        00 00 00 00 00
        00 00 00 00 00 00 00 00  #Server host name
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00  #Boot file name
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        00 00 00 00 00 00 00 00
        63 82 53 63              #Magic cookie
        35 01 03                 #DHCP message type (35=code 01=length 01=discover 02=offer
                                 #03=request 04 = decline 05=DHCPAck 06=DHCPNack 07=DHCPRelease
                                 #07=DHCPInform)
        74 01 01                 #DHCP auto-configuration (74=auto 01=length 01=value)
        3d 07 01 00 0b db 40 ce  #Client identifier (3d=client ID 07=length)
        33 
        32 04 8d a5 d3 cf        #Requested Ip address (New!)
        0c 06 4a 69 6d 2d 50 43  #Host name (12=host name 06=length value="Jim-PC")
        51 09 00 00 00 4a 69 6d  #Client fully qualified domain name (New!)
        2d 50 43
        3c 08 4d 53 46 54 20 35  #Vendor class identifier (60=Client ID 08=length value="<.MSFT  5.0"
        2e 30 
        37 0c 01 0f 03 06 2c 2e  #Parameter request list (37=PRL  12=length
        2f 1f 21 79 f9 2b       
                                 #1  = Subnet mask
                                 #15 = Domain name
                                 #3  = Router
                                 #6  = Domain name server
                                 #31 = Static route
                                 #44 = Netbios over TCP/IP name server
                                 #46 = Netbios over TCP/IP node type
                                 #47 = Netbios over TCP/IP scope
                                 #121= Classless static route
                                 #249= Private/Classless static route
                                 #43 = vendor specific
        ff                       #End option
        }            
    */
    class DHCP
    {
        private string rawPacketText;
        private byte[] packet;
        private string senderIp, targetIp, senderMAC, targetMAC;

        public byte[] GetPacket()
        {
            return packet;
        }
        //when building, beware checksums
        public DHCP DHCPDiscover()
        {
            //MIGHT NOT NEED TO BUILD BUT DO NEED TO READ
            //build the DHCP Discover packet (client to server[Broadcast])
            return new DHCP();
        }
        public DHCP DHCPOffer()
        {
            //build the DHCP Offer packet (server to client[Broadcast])
            return new DHCP();
        }
        public DHCP DHCPRequest()
        {
            //MIGHT NOT NEED TO BUILD BUT DO NEED TO READ
            //build the DHCP Request packet (client to server[Brodcast])
            return new DHCP();
        }
        public DHCP DHCPACK()
        {
            //build the DHCP ACK packet (server to client)
            return new DHCP();
        }
        public DHCP DHCPRelease()
        {
            //MIGHT NOT NEED TO BUILD BUT DO NEED TO READ
            //build the DHCP Release packet (client to server)
            return new DHCP();
        }
        //do we want to worry about DHCP Information
    }
}
