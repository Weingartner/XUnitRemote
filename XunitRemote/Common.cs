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

        public static void ExecuteWithChannel<TChannel>(ChannelFactory<TChannel> channelFactory, Action<TChannel> action)
        {
            var service = channelFactory.CreateChannel();
            var channel = (IServiceChannel) service;
            var success = false;
            try
            {
                action(service);
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