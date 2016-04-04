using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PacketCapture
{
    public class IPDetails
    {
        public string city="";
        public string country = "";
        public string countryCode = "";
        public string isp = "";
        public int lat=0;
        public int lon=0;
        public string org = "";
        public string query = "";
        public string region = "";
        public string regionName = "";
        public string status = "";
        public string timezone = "";
        public string zip = "";

        private WebClient web;
        public IPDetails() {
            web = new WebClient();
        }
        public static string parseIP(string ip)
        {
            try
            {
                return new WebClient().DownloadString("http://ip-api.com/json/" + ip);
            }
            catch (Exception e)
            {
                return "Error";            
            }
        }
        public static IPDetails BuildFromIP(string ip)
        {
            var json = parseIP(ip);
            var details = new IPDetails();
            details.query = ip;
            if (json != "Error")
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    details.city = data.ContainsKey("city") ? data["city"] : "N/A";
                    details.country = data.ContainsKey("country") ? data["country"] : "N/A";
                    details.isp = data.ContainsKey("isp") ? data["isp"] : "N/A";
                    details.org = data.ContainsKey("org") ? data["org"] : "N/A";
                    details.query = data.ContainsKey("query") ? data["query"] : ip;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    return details;
                }
            }
            return details;
        }
    }
}
