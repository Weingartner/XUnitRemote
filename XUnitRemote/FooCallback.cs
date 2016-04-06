using System;
using XUnitRemote.Remoting.Service;

namespace XUnitRemote
{
    public class FooCallback
    {
        public static void Callback(string assemblyPath, string typeName, string methodName)
        {
            var service = new TestService();
            var r = service.RunTest(assemblyPath, typeName, methodName);
            AppDomain.CurrentDomain.SetData("Result", r);
        }
    }
}