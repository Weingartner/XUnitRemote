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
        public Guid? CollectionId { get; set; }
        private readonly IMessageSink _DiagnosticMessageSink;
        private readonly string _Id;
        private readonly string _ExePath;
        private readonly IXunitTestCaseDiscoverer _DefaultTestCaseDiscoverer;
        private readonly Func<IXunitTestCase, IXunitTestCase> _TestCaseConverter;
        private static readonly Guid _UniqueId = Guid.NewGuid();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="diagnosticMessageSink"></param>
        /// <param name="id">Unique identifier of the communication channel between the two processes</param>
        /// <param name="exePath">Location of the remote process</param>
        /// <param name="defaultTestCaseDiscoverer"></param>
        /// <param name="testCaseConverter"></param>
        protected XUnitRemoteTestCaseDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath, IXunitTestCaseDiscoverer defaultTestCaseDiscoverer, Func<IXunitTestCase, IXunitTestCase> testCaseConverter, Guid? collectionId)
        {
            CollectionId = collectionId;
            _DiagnosticMessageSink = diagnosticMessageSink;
            _Id = id;
            _ExePath = exePath;
            _DefaultTestCaseDiscoverer = defaultTestCaseDiscoverer;
            _TestCaseConverter = testCaseConverter;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            if(CollectionId.HasValue)
                testMethod = WrapTestMethod(testMethod, CollectionId.Value);

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

        private ITestMethod WrapTestMethod(ITestMethod testMethod, Guid uniqueId)
        {
            var testClass = testMethod.TestClass;
            var testCollection = testClass.TestCollection;
            testCollection = new TestCollection
                (testCollection.TestAssembly, testCollection.CollectionDefinition, testCollection.DisplayName)
            {
                UniqueID = uniqueId
            };
            testClass = new TestClass(testCollection, testClass.Class);
            testMethod = new TestMethod(testClass, testMethod.Method);
            return testMethod;
        }
    }

    public abstract class XUnitRemoteFactDiscoverer : XUnitRemoteTestCaseDiscoverer
    {
        /// <summary>
        /// </summary>
        /// <param name="uniqueId">Set this to a non null GUID to force all tests tagged with associated fact to be run sequentially</param>
        protected XUnitRemoteFactDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath, Guid? uniqueId)
            : this(diagnosticMessageSink, id, exePath, p => p, uniqueId)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="uniqueId">Set this to a non null GUID to force all tests tagged with associated fact to be run sequentially</param>
        protected XUnitRemoteFactDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath, Func<IXunitTestCase, IXunitTestCase> testCaseConverter, Guid? uniqueId)
            : base(diagnosticMessageSink, id, exePath, new FactDiscoverer(diagnosticMessageSink), testCaseConverter, uniqueId)
        {
        }
    }

    public abstract class XUnitRemoteTheoryDiscoverer : XUnitRemoteTestCaseDiscoverer
    {
        /// <summary>
        /// </summary>
        /// <param name="uniqueId">Set this to a non null GUID to force all tests tagged with associated fact to be run sequentially</param>
        protected XUnitRemoteTheoryDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath, Guid? uniqueId)
            : this(diagnosticMessageSink, id, exePath, p => p, uniqueId)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="uniqueId">Set this to a non null GUID to force all tests tagged with associated fact to be run sequentially</param>
        protected XUnitRemoteTheoryDiscoverer(IMessageSink diagnosticMessageSink, string id, string exePath, Func<IXunitTestCase, IXunitTestCase> testCaseConverter, Guid? uniqueId)
            : base(diagnosticMessageSink, id, exePath, new TheoryDiscoverer(diagnosticMessageSink), testCaseConverter, uniqueId)
        {
        }
    }
}