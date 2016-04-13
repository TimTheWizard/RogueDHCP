using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RogueDHCP;
using PacketCapture;
using System.Windows.Forms;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void validIp_True()
        {
            Assert.IsTrue(IPTables.validIp("195.168.10.10"));
        }
        [TestMethod]
        public void validIp_InvalidChar()
        {
            Assert.IsFalse(IPTables.validIp("A.168.10.10"));
        }
        [TestMethod]
        public void validIp_OutRange()
        {
            Assert.IsFalse(IPTables.validIp("300.168.10.10"));
        }
        [TestMethod]
        public void IPChecksumKnownCase_Clean()
        {
            Assert.AreEqual("4D11", DHCP.ChecksumCalc("4500", "0156", "0fdf", "0000", "ff11", "0000", "8DA5", "D001", "FFFF", "FFFF"), true);
        }
        [TestMethod]
        public void IPConvertToStandard_Clean()
        {
            Assert.AreEqual("c0a80b03", FRMCapture.ConvertIpToHex("192.168.11.3"), true);
        }
        [TestMethod]
        public void ConvertHexIpToStandard_Clean()
        {
            Assert.AreEqual("192.168.11.3", FRMCapture.ConvertHexIpToStandard("c0a80b03"), true);
        }
        [TestMethod]
        public void BuildBaseDHCP_Clean()
        {
            DHCP baseObj = new DHCP(FRMCapture.ConvertIpToHex("192.168.11.3"), "8DA5D001", "FF", "FFFFFF00", FRMCapture.ConvertIpToHex("192.168.11.1"));
            DHCP offer = baseObj.DHCPOffer(FRMCapture.ConvertIpToHex("192.168.11.3"), FRMCapture.ConvertIpToHex("8.8.8.8"), FRMCapture.ConvertIpToHex("8.8.4.4"));
            var data = offer.GetPacket();
            Assert.IsNotNull(data);
            string dataString="";
            foreach (byte b in data)
            {
                var bVal=Convert.ToString(b,16);
                dataString+=bVal.Length<2? "0"+bVal: bVal;
            }
            string expected = "ffffffffffff8da5d00108004500012f0fdf0000ff11df33c0a80b03ffffffff00430044011b000002010601000000000000800000000000c0a80b03000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005c534d53426f6f745c7836345c7764736e62702e636f6d000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000638253633501023604c0a80b033304ff0104ffffff000304c0a80b0106080808080808080404ff";
            Console.WriteLine(expected);
            Console.WriteLine(dataString);
            Assert.AreEqual(expected, dataString, true);
        }
        [TestMethod]
        public void BuildDHCPOffer_Clean()
        {
            try
            {
                DHCP dhcp = DHCP.DHCPOffer("989096D1BFFC", "8DA5D001", "33553355", "8DA5D54F", "D8CB8A61FCEB", "FFFFF800", "8DA5D001", "00000e10", "08080808", "08080606");
                var data=dhcp.GetPacket();
                Assert.IsNotNull(data);
                string dataString="";
                foreach (byte b in data)
                {
                    var bVal=Convert.ToString(b,16);
                    dataString+=bVal.Length<2? "0"+bVal: bVal;
                }
                string expected = "ffffffffffff989096d1bffc0800450001320fdf0000ff114d358da5d001ffffffff00430044011e0000020106013355335500008000000000008da5d54f0000000000000000d8cb8a61fceb00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005c534d53426f6f745c7836345c7764736e62702e636f6d0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000006382536335010236048da5d001330400000e100104fffff80003048da5d00106080808080808080606ff";
                Assert.AreEqual(expected.ToUpper(),dataString.ToUpper());
            }
            catch(Exception e)
            {
                Assert.Fail(e.Message,e);
            }
        }
    }
}
