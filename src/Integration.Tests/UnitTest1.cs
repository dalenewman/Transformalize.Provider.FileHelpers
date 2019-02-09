using System.Linq;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Configuration;
using Transformalize.Containers.Autofac;
using Transformalize.Contracts;
using Transformalize.Provider.FileHelpers.Autofac;
using Transformalize.Providers.Bogus.Autofac;
using Transformalize.Providers.Console;

namespace Integration.Tests {
    [TestClass]
    public class UnitTest1 {

        [TestMethod]
        public void Write() {
            const string xml = @"<add name='file' mode='init'>
  <parameters>
    <add name='Size' type='int' value='1000' />
  </parameters>
  <connections>
    <add name='input' provider='bogus' seed='1' />
    <add name='output' provider='file' delimiter=',' file='c:\temp\bogus.csv' />
  </connections>
  <entities>
    <add name='Contact' size='@[Size]'>
      <fields>
        <add name='Identity' type='int' />
        <add name='FirstName' />
        <add name='LastName' />
        <add name='Stars' type='byte' min='1' max='5' />
        <add name='Reviewers' type='int' min='0' max='500' />
      </fields>
    </add>
  </entities>
</add>";
            using (var outer = new ConfigurationContainer().CreateScope(xml)) {
                using (var inner = new TestContainer(new BogusModule(), new FileHelpersModule()).CreateScope(outer, new ConsoleLogger(LogLevel.Debug))) {
                    var process = inner.Resolve<Process>();
                    var controller = inner.Resolve<IProcessController>();
                    controller.Execute();

                    Assert.AreEqual((uint)1000, process.Entities.First().Inserts);
                }
            }
        }

        [TestMethod]
        public void WriteWithSomeLineBreaks() {
            const string xml = @"<add name='file' mode='init'>
  <connections>
    <add name='input' provider='internal' />
    <add name='output' provider='file' delimiter=',' file='c:\temp\data-with-line-breaks-and-commas.csv' text-qualifier='""' />
  </connections>
  <entities>
    <add name='Contact'>
      <rows>
        <add Identity='1' FirstName='Dale' LastName='Newman' Stars='1' Reviewers='1' />
        <add Identity='2' FirstName='Dale
 Jr' LastName='Newman,s' Stars='2' Reviewers='2' />
      </rows>
      <fields>
        <add name='Identity' type='int' />
        <add name='FirstName' />
        <add name='LastName' />
        <add name='Stars' type='byte' />
        <add name='Reviewers' type='int' />
      </fields>
    </add>
  </entities>
</add>";
            using (var outer = new ConfigurationContainer().CreateScope(xml)) {
                using (var inner = new TestContainer(new BogusModule(), new FileHelpersModule()).CreateScope(outer, new ConsoleLogger(LogLevel.Debug))) {
                    //var process = inner.Resolve<Process>();
                    var controller = inner.Resolve<IProcessController>();
                    controller.Execute();
                    
                }
            }
        }

        [TestMethod]
        public void ReadWithSomeLineBreaks() {
            const string xml = @"<add name='file' mode='init'>
  <connections>
    <add name='input' provider='file' delimiter=',' file='c:\temp\data-with-line-breaks-and-commas.csv' text-qualifier='""' />
    <add name='output' provider='internal' />  
  </connections>
  <entities>
    <add name='Contact'>
      <fields>
        <add name='Identity' type='int' />
        <add name='FirstName' />
        <add name='LastName' />
        <add name='Stars' type='byte' />
        <add name='Reviewers' type='int' />
      </fields>
    </add>
  </entities>
</add>";
            using (var outer = new ConfigurationContainer().CreateScope(xml)) {
                using (var inner = new TestContainer(new BogusModule(), new FileHelpersModule()).CreateScope(outer, new ConsoleLogger(LogLevel.Debug))) {
                    var process = inner.Resolve<Process>();
                    var controller = inner.Resolve<IProcessController>();
                    controller.Execute();
                    Assert.AreEqual(2, process.Entities.First().Rows.Count);

                }
            }
        }

        [TestMethod]
        public void Read() {
            const string xml = @"<add name='file'>
  <connections>
    <add name='input' provider='file' delimiter=',' file='c:\temp\bogus.csv' start='2' />
    <add name='output' provider='internal' />
  </connections>
  <entities>
    <add name='BogusStar' alias='Contact' page='1' size='10'>
      <fields>
        <add name='Identity' type='int' />
        <add name='FirstName' />
        <add name='LastName' />
        <add name='Stars' type='byte' />
        <add name='Reviewers' type='int' />
      </fields>
    </add>
  </entities>
</add>";
            using (var outer = new ConfigurationContainer().CreateScope(xml)) {
                using (var inner = new TestContainer(new FileHelpersModule()).CreateScope(outer, new ConsoleLogger(LogLevel.Debug))) {

                    var process = inner.Resolve<Process>();

                    var controller = inner.Resolve<IProcessController>();
                    controller.Execute();
                    var rows = process.Entities.First().Rows;

                    Assert.AreEqual(10, rows.Count);

                }
            }
        }

        [TestMethod]
        public void ReadSchema() {
            const string xml = @"<add name='file'>
  <connections>
    <add name='input' provider='file' file='c:\temp\bogus.csv'>
        <types>
            <add type='byte' />
            <add type='int' />
            <add type='string' />
        </types>
    </add>
    <add name='output' provider='internal' />
  </connections>
  <entities>
    <add name='BogusStar' alias='Contact' />
  </entities>
</add>";
            using (var outer = new ConfigurationContainer().CreateScope(xml)) {
                using (var inner = new TestContainer(new FileHelpersModule()).CreateScope(outer, new ConsoleLogger(LogLevel.Debug))) {

                    var process = inner.Resolve<Process>();

                    var schemaReader = inner.ResolveNamed<ISchemaReader>(process.Connections.First().Key);
                    var schema = schemaReader.Read();

                    var entity = schema.Entities.First();

                    Assert.AreEqual(5, entity.Fields.Count);
                    Assert.AreEqual("int", entity.Fields[0].Type);
                    Assert.AreEqual("string", entity.Fields[1].Type);
                    Assert.AreEqual("string", entity.Fields[2].Type);
                    Assert.AreEqual("byte", entity.Fields[3].Type);
                    Assert.AreEqual("int", entity.Fields[4].Type);


                }
            }
        }
    }
}
