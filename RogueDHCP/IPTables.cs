using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RogueDHCP
{
    /*
     * The point of this class is to hold the ip list together
     * We need to keep up of two things
     * 1: Who has the IP and when does it expire
     * 2: What IPs are availible for use
     * This should keep up if the values are updated and
     * if the leases expire to move them back to avalible
    */
    public class IPTables
    {
        private string[] _AllIP;
        private List<string> _AvailableIps = new List<string>();
        private List<Tuple<string, string, DateTime>> ipList = new List<Tuple<string, string, DateTime>>();
        private bool _updated = false;

        public IPTables(string ip, string netmask)
        {
            if (!validIp(ip))
                throw new Exception("Invalid ip");
            _AvailableIps = gennerateIPRange(ip, netmask);
            _AllIP = _AvailableIps.ToArray();
        }
        public bool reserveIp(string ip, string mac, DateTime kill)
        {
            if (!validIp(ip))
                throw new Exception("Invalid ip");
            //check that it is avalible
            if (_AvailableIps.Contains(ip))
            {
                //if so, move it over to the inuse list
                if (_AvailableIps.Remove(ip))
                {
                    ipList.Add(new Tuple<string, string, DateTime>(ip, mac, kill));
                    _updated = true;
                    return true;
                }
            }
            //if its an update, just reset the kill time
            var item = ipList.FirstOrDefault(x => x.Item1 == ip && x.Item2 == mac);
            if (item!=null)
            {
                ipList.Remove(item);
                ipList.Add(new Tuple<string, string, DateTime>(ip, mac, kill));
                _updated = true;
                return true;
            }
            return false;
        }
        public bool isAvailable(string ip)
        {
            return _AvailableIps.Contains(ip);
        }
        public bool releaseIp(string ip)
        {
            if (!validIp(ip))
                throw new Exception("Invalid ip");
            int numRemoved=ipList.RemoveAll(x => x.Item1==ip);
            _AvailableIps.Add(ip);
            _updated = true;
            return numRemoved>0;
        }
        public bool updateLists(DateTime dt)
        {
            foreach (var ip in ipList)
            {
                if (ip.Item3 < dt)
                    _AvailableIps.Add(ip.Item1);
            }
            //get the num of killed ips
            int numUpdated = ipList.RemoveAll(x => x.Item3 < dt);
            //if there are more than 0, then we have updated the tables
            if (numUpdated > 0)
                _updated = true;
            return numUpdated > 0;
        }
        public bool isUpdated()
        {
            return _updated;
        }
        public string[] GetAvalible()
        {
            _updated = false;
            return _AvailableIps.ToArray();
        }
        public Tuple<string, string, DateTime>[] GetIPsInUse()
        {
            _updated = false;
            ipList.Sort();
            return ipList.ToArray();
        }
        public static bool validIp(string ip)
        {
            var ipParts = ip.Split('.');
            if (ipParts.Length == 4)
            {
                foreach (var part in ipParts)
                {
                    if (part.All(x => Char.IsNumber(x)))
                    {
                        int val = Convert.ToInt32(part);
                        if (val > 255 || val < 0)
                        {
                            return false;
                        }
                    }
                    else
                        return false;
                }
                return true;
            }
            return false;
        }
        public static List<string> gennerateIPRange(string ip, string netmask)
        {
            if (!validIp(ip))
                throw new Exception("Invalid ip");

            if (!validIp(netmask))
                throw new Exception("Invalid Netmask");

            List<string> range = new List<string>();
            string[] ipparts = ip.Split('.');
            string[] netparts = netmask.Split('.');
            string ipBinary = "";
            string netBinary = "";
            foreach (string oct in ipparts)
            {
                string temp = Convert.ToString(Convert.ToInt32(oct), 2);
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
            int[] rootMaxparts = Enumerable.Range(0, rootMax.Length / 8).Select(i => Convert.ToInt32(rootMax.Substring(i * 8, 8), 2)).ToArray<int>();
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
            } while (oct1 <= rootMaxparts[0]);
            range.RemoveAt(0);
            range.RemoveAt(range.Count - 1);
            return range;
        }

        public void Reset()
        {
            _AvailableIps = _AllIP.ToList();
            ipList.Clear();
            _updated = true;
        }

        public string WhoHas(string ip)
        {
            var owner = ipList.FirstOrDefault(x => x.Item1 == ip);
            if(owner!=null)
            {
                return owner.Item2;
            }
            return null;
        }
    }
}
