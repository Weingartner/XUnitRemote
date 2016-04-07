using System;
using System.Runtime.Serialization;

namespace XUnitRemote.Local
{
    [Serializable]
    public class TestExecutionException : Exception
    {
        public TestExecutionException()
        {
        }

        public TestExecutionException(string message) : base(message)
        {
        }

        public TestExecutionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected TestExecutionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}