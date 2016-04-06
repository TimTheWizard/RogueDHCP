using System;
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
    public class DHCP
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
        public static bool OnlyHexInString(string test)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[0-9a-fA-F]+\b\Z");
        }
        public static string ChecksumCalc(params string[] byteChunks)
        {
            int sum=0;
            string checksum = "0000";
            foreach (var chunk in byteChunks)
            {
                if (chunk.Length == 4 && OnlyHexInString(chunk))
                    sum += Convert.ToInt32(chunk, 16);
                else
                    throw new Exception("Invalid data element length");
            }
            checksum = sum.ToString("X4");
            while (checksum.Length > 4)
            {
                sum = Convert.ToInt32(checksum.Substring(checksum.Length-4,4),16);
                sum += Convert.ToInt32(checksum.Substring(0, checksum.Length - 4), 16);
                checksum = sum.ToString("X4");
            }
            //sum should be the int value of the checksum
            //take the inverse
            sum = Convert.ToInt32("FFFF", 16) - sum;
            checksum = sum.ToString("X4");
            return checksum;
        }
        private static string BuildIPHeader(string id, string ttl ,string protocalType,string serverIp, string destinationIp)
        {
            //validate for ipv4
            if (serverIp.Length != 8 && OnlyHexInString(serverIp))
                throw new Exception("Invalid Source IP (Must be Hex IPv4)");
            if (destinationIp.Length != 8 && OnlyHexInString(destinationIp))
                throw new Exception("Invalid Destination IP (Must be Hex IPv4)");
            if (!OnlyHexInString(id))
                throw new Exception("Invalid ID (Must be Hex)");
            if (!OnlyHexInString(ttl))
                throw new Exception("Invalid Time To Live (Must be Hex)");
            if (!OnlyHexInString(protocalType))
                throw new Exception("Invalid Protocal Type (Must be Hex)");
            if (id.Length > 4)
                id.Substring(0, 4);
            while(id.Length < 4)
                id += "0";
            if (ttl.Length > 2)
                ttl.Substring(0, 2);
            while (ttl.Length < 2)
                ttl += "0";
            if (protocalType.Length > 2)
                protocalType.Substring(0, 2);
            while (protocalType.Length < 2)
                protocalType += "0";
            //actual work
            string ipHeader =
                "4500" +
                "0156" + //length
                id + //id ?might need to gen
                "0000" + //flag
                ttl +
                protocalType +
                "0000" + //checksum need to gen
                serverIp +
                destinationIp;
            var ipHeaderparts = new string[ipHeader.Length / 4];
            for (int i = 0; i < ipHeader.Length; i++)
            {
                ipHeaderparts[i / 4] += ipHeader[i];
            }
            //insert checksum
            ipHeader =
                ipHeader.Substring(0, ipHeader.Length - 20) +
                ChecksumCalc(ipHeaderparts) +
                ipHeader.Substring(ipHeader.Length - 16, 16);
            return ipHeader;
        }
        public static DHCP DHCPOffer(string serverMAC, string serverIp, string transId, string offeredIp, string clientMac, string subnet, string routerIp, string leaseTime="00000e10")
        {
            //build the DHCP Offer packet (server to client[Broadcast])
                //ETHERNET HEADER
            string ethernetHeader =
                "ffffffffffff" + //destinationMac
                serverMAC +//98 90 96 D1 BF FC  //sourceMAc
                "0800";//IPv4
            //IP HEADER
            string ipHeader = BuildIPHeader("0FDF", "FF", "11", serverIp, "FFFFFFFF");
            //UDP HEADER
            string udpHeader =
                "0043" + //source port
                "0044" + //destination port
                "0142" + //length need to check?
                "43E4"; //checksum ned to gen
            //PAYLOAD
            string bootP=
                "020106" + //type, ethernet, mac length
                "01" + //hops 
                transId +//trans id
                "0000" + //sec elapsed
                "8000" + //bootp flag
                "00000000" + //client ip
                offeredIp + //offered ip
                "00000000" + //next server ip
                "00000000" + //relay agent
                clientMac + //client mac
                //server host name
                "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                //boot file
                "5c534d53426f6f745c7836345c7764736e62702e636F6D000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "63825363" +//magic cookie
                "350102" +//DHCP Offer
                "3604" + serverIp + //dhcp server identifire
                "3304" + leaseTime + //ip lease time
                "0104" + subnet + //subnet
                "0F1367656F72676961736F75746865726E2E656475" +//domain name
                "0304" + routerIp + //router
                "0604"+routerIp+ //88DA506068DA50109" + //domain name server
                "FF"; //end
            string offer = ethernetHeader + ipHeader + udpHeader + bootP;
            //convert offer to byteStream
            byte[] packetData = new byte[offer.Length / 2];
            for (int i = 0; i < offer.Length; i+=2)
            {
                packetData[i / 2] = Convert.ToByte(offer.Substring(i,2), 16);
            }
            return new DHCP() { packet = packetData };
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
