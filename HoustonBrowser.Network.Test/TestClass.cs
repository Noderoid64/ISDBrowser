using Xunit;
using System;

using HoustonBrowser.HttpModule.Parsers;
using HoustonBrowser.HttpModule.Builders;
using HoustonBrowser.HttpModule.Model;

namespace HoustonBrowser.HttpModule.Test
{
    public class TestClass
    {
        //http://www.netside.net/boba/webmasters.html
        //192.168.0.110
        //houstonbrowsertest.ddns.net
        [Fact]
        public void TestVoid(){
            IHttpClient client = new HttpClient();

            string real = client.GetHtml("127.0.0.1");

            Assert.NotEmpty(real);
        }
    }
}