using System;
using System.IO;
using System.Reflection;

namespace XUnitRemote.Remote.Isolation
{
    /// <summary>
    /// Abstract base class for creating callback in a child app domain and getting
    /// a result back. The child domain is created and then destroyed immediately after
    /// the call is complete.
    /// </summary>
    /// <typeparam name="T">The return type of the call. Must be serializable</typeparam>
    [Serializable]
    public abstract class IsolateBase<T>
    {
        private readonly string _DomainName;

        protected IsolateBase(string domainName)
        {
            _DomainName = domainName;
        }

        private static AppDomain CloneDomain(string name)
        {
            // TODO also resolve assemblies in AppDomain.CurrentDomain.BaseDirectory

            var path = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            var dir = Path.GetDirectoryName(path);
            var domaininfo = AppDomain.CurrentDomain.SetupInformation;
            domaininfo.ApplicationBase = dir;
            var domain = AppDomain.CreateDomain(name, AppDomain.CurrentDomain.Evidence, domaininfo);
            return domain;
        }

        public T Run()
        {
            const string resultKey = "Result";
            var domain = CloneDomain(_DomainName);
            domain.DoCallBack(() => AppDomain.CurrentDomain.SetData(resultKey, RunImpl()));
            var rr = (T)domain.GetData(resultKey);
            AppDomain.Unload(domain);
            GC.Collect();
            return rr;
        }

        protected abstract T RunImpl();
    }
}