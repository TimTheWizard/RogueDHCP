using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueDHCP
{
    public class Settings
    {
        public string domainName;
        public string gateway;
        public string subnet;
        public string[] dns;
        public string NICName;
        public int leaseTime;

        public Settings():this("", "0.0.0.0", "255.255.255.0", "", 3600, "8.8.8.8", "8.8.4.4") {}
        public Settings(string domainName, string gateway, string subnet, string NICName, int sec, params string[] dns){
            gateway = gateway.Trim();
            subnet = subnet.Trim();

            if (!IPTables.validIp(gateway))
                throw new Exception("Invalid Gateway Address");
            if (!IPTables.validIp(subnet))
                throw new Exception("Invalid Subnet Address");
            foreach(var dnsItem in dns){
                if (dnsItem =="" && !IPTables.validIp(dnsItem))
                    throw new Exception("Invalid DNS 2 Address");
            }
            this.domainName = domainName;
            this.gateway = gateway;
            this.subnet = subnet;
            this.dns = dns;
            this.NICName = NICName;
            this.leaseTime = sec;
        }
        public string[] GetDns()
        {
            List<string> dnsTemp = new List<string>();
            foreach (var dnsItem in dns)
            {
                dnsTemp.Add(dnsItem);
            }
            return dnsTemp.ToArray();
        }

        public string[] GetDnsHex()
        {
            List<string> dnsTemp = new List<string>();
            foreach (var dnsItem in dns)
            {
                dnsTemp.Add(FRMCapture.ConvertIpToHex(dnsItem));
            }
            return dnsTemp.ToArray();
        }
    }
}
