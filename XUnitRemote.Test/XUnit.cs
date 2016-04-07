using System;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using XUnitRemote.Local;
using FactAttribute = Xunit.FactAttribute;

namespace XUnitRemote.Test
{

    /// <summary>
    /// This is the custom attribute you add to tests you wish to run under the control of SampleProcess.
    /// For example your unit test can look like
    /// <![CDATA[
    /// [SampleProcessFact]
    /// public void TestShouldWork(){
    ///     Assert.Equal("SampleProcess", GetProcess.GetCurrentProcess().ProcessName)
    /// }
    /// ]]>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [XunitTestCaseDiscoverer("XUnitRemote.Test.SampleProcessFactDiscoverer", "XUnitRemote.Test")]
    public class SampleProcessFactAttribute : FactAttribute { }

    [AttributeUsage(AttributeTargets.Method)]
    [XunitTestCaseDiscoverer("XUnitRemote.Test.SampleProcessTheoryDiscoverer", "XUnitRemote.Test")]
    public class SampleProcessTheoryAttribute : TheoryAttribute { }

    internal static class Common
    {
        public const string SampleProcessPath = @"..\..\..\XUnitRemote.Test.SampleProcess\bin\Debug\XUnitRemote.Test.SampleProcess.exe";
    }

    /// <summary>
    /// This is the xunit fact discoverer that xunit uses to replace the standard xunit runner
    /// with our runner. Anything that is tagged with the above attribute will use this discoverer. 
    /// </summary>
    public class SampleProcessFactDiscoverer : XUnitRemoteFactDiscoverer
    {
        public SampleProcessFactDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink, SampleProcess.Program.Id, Common.SampleProcessPath)
        {
        }
    }

    public class SampleProcessTheoryDiscoverer : XUnitRemoteTheoryDiscoverer
    {
        public SampleProcessTheoryDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink, SampleProcess.Program.Id, Common.SampleProcessPath)
        {
        }
    }
}
