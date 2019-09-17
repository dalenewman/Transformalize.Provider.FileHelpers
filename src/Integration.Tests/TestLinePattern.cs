using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;
using Transformalize.Provider.FileHelpers.Autofac;
using Transformalize.Providers.Console;

namespace Integration.Tests {
   [TestClass]
   public class TestLinePattern {

      [TestMethod]
      public void ThirdLineGetsStackTraceAppended() {
         const string xml = @"<add name='test'>
  <connections>
    <add name='input' provider='file' delimiter='' file='files\sample.01.txt' line-pattern='^\d{4}\-\d{2}\-\d{2}\s{1}\d{2}\:\d{2}\:\d{2}\,\d{3}\s{1}\[(\w|-)+\]\s+(INFO|WARN|ERROR|FATAL){1}\s+.*$' />
  </connections>
  <entities>
    <add name='File'>
      <fields>
        <add name='Line' length='max' />
      </fields>
    </add>
  </entities>
</add>";
         var logger = new ConsoleLogger(LogLevel.Debug);
         using (var outer = new ConfigurationContainer().CreateScope(xml, logger)) {
            var process = outer.Resolve<Process>();
            using (var inner = new Container(new FileHelpersModule()).CreateScope(process, logger)) {
               var controller = inner.Resolve<IProcessController>();
               controller.Execute();
               Assert.AreEqual(5, process.Entities[0].Rows.Count);
               Assert.AreEqual(@"2019-06-16 08:14:06,697 [13] ERROR log - Stopped processing file \\sbsan2\projects\10-HONE03-1000\ETL\FromHost\4000\fromhost-endpoint-electric-4000-20190616120814.txt : Unexpected error System.Net.WebException: The request failed with HTTP status 400: Bad Request.    at System.Web.Services.Protocols.SoapHttpClientProtocol.ReadResponse(SoapClientMessage message, WebResponse response, Stream responseStream, Boolean asyncCall)    at System.Web.Services.Protocols.SoapHttpClientProtocol.Invoke(String methodName, Object[] parameters)    at Clevest.HostExchange.Engine.Ws.HostService.FromHost(XmlNode xmlNode, String sessionId)    at Clevest.HostExchange.Engine.Runner.SendMFFRequest(XmlNode node, String filename, Int32 idx, Boolean& abortFlag, Boolean& configError, ResultSummary summary)", process.Entities[0].Rows[2]["Line"]);
               Assert.AreEqual(@"2019-06-16 08:14:06,956 [13] INFO  log - Moved to \\sbsan2\projects\10-HONE03-1000\ETL\FromHost\4000\save\fromhost-endpoint-electric-4000-20190616120814-20190616121406.txt", process.Entities[0].Rows[4]["Line"]);
            }
         }
      }

      [TestMethod]
      public void CsvWithLineBreaksButNoTextQualifier() {
         const string xml = @"<add name='test'>
  <connections>
    <add name='input' provider='file' delimiter='' file='files\sample.02.txt' start='2' line-pattern='^([^,]*,){11}[^,]*$' />
  </connections>
  <entities>
    <add name='File'>
      <fields>
        <add name='Line' length='max' />
      </fields>
    </add>
  </entities>
</add>";
         var logger = new ConsoleLogger(LogLevel.Debug);
         using (var outer = new ConfigurationContainer().CreateScope(xml, logger)) {
            var process = outer.Resolve<Process>();
            using (var inner = new Container(new FileHelpersModule()).CreateScope(process, logger)) {
               var controller = inner.Resolve<IProcessController>();
               controller.Execute();
               var output = process.Entities[0].Rows;
               Assert.AreEqual(7, output.Count);
               Assert.AreEqual(@"1/2/09 6:17,Product1,1200,Mastercard,carolina,Basildon,England,United Kingdom,1/2/09 6:00,1/2/09 6:08,51.5,-1.1166667", output[0]["Line"]);
               Assert.AreEqual(@"1/2/09 4:53,Product1,1200,Visa,Betina,Parkville,MO,United States,1/2/09 4:42,1/2/09 7:49,39.195,-94.68194", output[1]["Line"]);
               Assert.AreEqual(@"1/4/09 14:11,Product1,1200,Visa,Aidan,Chatou,Ile- de- France,France,6/3/08 4:22,1/5/09 1:17,48.8833333,2.15", output[4]["Line"]);
               Assert.AreEqual(@"1/5/09 5:39,Product1,1200,Amex,Heidi,Eindhoven,Noord-Brabant,Netherlands,1/5/09 4:55,1/5/09 8:15,51.45,5.4666667", output[6]["Line"]);

            }
         }
      }
   }
}

