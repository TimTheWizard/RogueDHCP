using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RogueDHCP;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void IPChecksumKnownCase_Clean()
        {
            Assert.AreEqual("4D11", DHCP.ChecksumCalc("4500", "0156", "0fdf", "0000", "ff11", "0000", "8DA5", "D001", "FFFF", "FFFF"), true);
        }
        [TestMethod]
        public void BuildDHCPOffer_Clean()
        {
            try
            {
                DHCP dhcp = null;//DHCP.DHCPOffer("989096D1BFFC", "8DA5D001", "33553355", "8DA5D54F", "D8CB8A61FCEB", "FFFFF800", "8DA5D001");
                var data=dhcp.GetPacket();
                Assert.IsNotNull(data);
                string dataString="";
                foreach (byte b in data)
                {
                    var bVal=Convert.ToString(b,16);
                    dataString+=bVal.Length<2? "0"+bVal: bVal;
                }
                string expected = "ffffffffffff989096D1BFFC0800450001560FDF0000FF114D118DA5D001FFFFFFFF00430044014243E4020106013355335500008000000000008DA5D54F0000000000000000D8CB8A61FCEB00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005c534d53426f6f745c7836345c7764736e62702e636F6D0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000006382536335010236048DA5D001330400000e100104FFFFF8000F1367656F72676961736F75746865726E2E65647503048DA5D00106048DA5D001FF";
                Assert.AreEqual(expected.ToUpper(),dataString.ToUpper());
            }
            catch(Exception e)
            {
                Assert.Fail(e.Message,e);
            }
        }
    }
}
