using System;
using System.IO;
using System.Reflection;
using XUnitRemote.Remoting.Result;

namespace XUnitRemote
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
        private string _ResultKey = "Result";

        protected IsolateBase(string domainName)
        {
            _DomainName = domainName;
        }

        private static AppDomain CloneDomain(string name)
        {
            string path = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
            var dir = Path.GetDirectoryName(path);
            var domaininfo = AppDomain.CurrentDomain.SetupInformation;
            domaininfo.ApplicationBase = dir;
            var domain = AppDomain.CreateDomain(name, AppDomain.CurrentDomain.Evidence, domaininfo);
            return domain;
        }

        public T Run()
        {
            var domain = CloneDomain(_DomainName);
            domain.DoCallBack(() => AppDomain.CurrentDomain.SetData(_ResultKey, RunImpl()));
            var rr = (T)domain.GetData(_ResultKey);
            AppDomain.Unload(domain);
            GC.Collect();
            return rr;
        }

        protected abstract T RunImpl();
    }
}