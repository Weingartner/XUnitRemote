using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace XUnitRemote
{
    public static class Common
    {
        public static NetNamedPipeBinding CreateBinding()
        {
            return new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { SendTimeout = TimeSpan.FromMinutes(5) };
        }

        public static async Task ExecuteWithChannel<TChannel>(ChannelFactory<TChannel> channelFactory, Func<TChannel, Task> action)
        {
            var service = channelFactory.CreateChannel();
            var channel = (IServiceChannel) service;
            var success = false;
            try
            {
                await action(service);
                channel.Close();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    channel.Abort();
                }
            }
        }
    }
}