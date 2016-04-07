using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XUnitRemote.Local
{
    public abstract class XUnitRemoteTestCaseDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _DiagnosticMessageSink;
        private readonly string _Id;
        private readonly string _ExePath;
        private readonly IXunitTestCaseDiscoverer _DefaultTestCaseDiscoverer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="diagnosticMessageSink"></param>
        /// <param name="id">Unique identifier of the communication channel between the two processes</param>
        /// <param name="exePath">Location of the remote process</param>
        /// <param name="defaultTestCaseDiscoverer"></param>
        protected XUnitRemoteTestCaseDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath, IXunitTestCaseDiscoverer defaultTestCaseDiscoverer)
        {
            _DiagnosticMessageSink = diagnosticMessageSink;
            _Id = id;
            _ExePath = exePath;
            _DefaultTestCaseDiscoverer = defaultTestCaseDiscoverer;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            if (!Process.GetCurrentProcess().ProcessName.Equals(Path.GetFileNameWithoutExtension(_ExePath), StringComparison.OrdinalIgnoreCase))
            {
                yield return new XUnitRemoteTestCase(_DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, _Id, _ExePath);
            }
            else
            {
                foreach (var testCase in _DefaultTestCaseDiscoverer.Discover(discoveryOptions, testMethod, factAttribute))
                {
                    yield return testCase;
                }
            }
        }
    }

    public abstract class XUnitRemoteFactDiscoverer : XUnitRemoteTestCaseDiscoverer
    {
        protected XUnitRemoteFactDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath)
            : base(diagnosticMessageSink, id, exePath, new FactDiscoverer(diagnosticMessageSink))
        {
        }
    }

    public abstract class XUnitRemoteTheoryDiscoverer : XUnitRemoteTestCaseDiscoverer
    {
        protected XUnitRemoteTheoryDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath)
            : base(diagnosticMessageSink, id, exePath, new TheoryDiscoverer(diagnosticMessageSink))
        {
        }
    }
}