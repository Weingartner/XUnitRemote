using System;
using Xunit.Abstractions;
using Xunit.Sdk;
using FactAttribute = Xunit.FactAttribute;

namespace XUnitRemote.Test
{

    /// <summary>
    /// This is the custom attribute you add to tests you wish to run under the control of SampleProcess.
    /// For example your unit test will look like
    /// <![CDATA[
    /// [SampleProcessFact]
    /// public void TestShouldWork(){
    ///    Assert.Equal("SampleProcess",Process.GetCurrentProcess().ProcessName)
    ///    Assert.IsTrue(1==2);
    /// }
    /// ]]>
    /// and it will be executed within "SampleProcess" not the visual studio process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [XunitTestCaseDiscoverer("XUnitRemote.Test.SampleProcessFactDiscoverer", "XUnitRemote.Test")]
    public class SampleProcessFactAttribute : FactAttribute { }


    /// <summary>
    /// This is the xunit fact discoverer that xunit uses to replace the standard xunit runner
    /// with our runner. Anything that is tagged with the above attribute will use this discoverer. 
    /// </summary>
    public class SampleProcessFactDiscoverer : XUnitRemoteFactDiscovererBase
    {
        protected override string Id { get; } = SampleProcess.Program.Id;
        protected override string ExePath { get; } = @"..\..\..\SampleProcess\bin\Debug\SampleProcess.exe";

        public SampleProcessFactDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
        }
    }
}
