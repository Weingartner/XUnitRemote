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
        private readonly Func<IXunitTestCase, IXunitTestCase> _TestCaseConverter;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="diagnosticMessageSink"></param>
        /// <param name="id">Unique identifier of the communication channel between the two processes</param>
        /// <param name="exePath">Location of the remote process</param>
        /// <param name="defaultTestCaseDiscoverer"></param>
        /// <param name="testCaseConverter"></param>
        protected XUnitRemoteTestCaseDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath, IXunitTestCaseDiscoverer defaultTestCaseDiscoverer, Func<IXunitTestCase, IXunitTestCase> testCaseConverter)
        {
            _DiagnosticMessageSink = diagnosticMessageSink;
            _Id = id;
            _ExePath = exePath;
            _DefaultTestCaseDiscoverer = defaultTestCaseDiscoverer;
            _TestCaseConverter = testCaseConverter;
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
                    yield return _TestCaseConverter(testCase);
                }
            }
        }
    }

    public abstract class XUnitRemoteFactDiscoverer : XUnitRemoteTestCaseDiscoverer
    {
        protected XUnitRemoteFactDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath)
            : this(diagnosticMessageSink, id, exePath, p => p)
        {
        }

        protected XUnitRemoteFactDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath, Func<IXunitTestCase, IXunitTestCase> testCaseConverter)
            : base(diagnosticMessageSink, id, exePath, new FactDiscoverer(diagnosticMessageSink), testCaseConverter)
        {
        }
    }

    public abstract class XUnitRemoteTheoryDiscoverer : XUnitRemoteTestCaseDiscoverer
    {
        protected XUnitRemoteTheoryDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath)
            : this(diagnosticMessageSink, id, exePath, p => p)
        {
        }

        protected XUnitRemoteTheoryDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath, Func<IXunitTestCase, IXunitTestCase> testCaseConverter)
            : base(diagnosticMessageSink, id, exePath, new TheoryDiscoverer(diagnosticMessageSink), testCaseConverter)
        {
        }
    }
}