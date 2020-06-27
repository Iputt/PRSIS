using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using System.ServiceModel;
using System.Threading;
using Owl.Feature;
namespace Owl
{
    internal class ChannelProxy<TChannel> where TChannel : class
    {
        protected string EndPointConfigurationName = typeof(TChannel).Name;
        private ChannelFactory<TChannel> factory;
        ReaderWriterLockSlim readerwriterlock = Locker.Create();
        protected ChannelFactory<TChannel> Factory
        {
            get
            {
                if (factory != null && factory.State == CommunicationState.Faulted)
                {
                    factory.Abort();
                    factory = null;
                }
                if (factory != null && (factory.State == CommunicationState.Closed || factory.State == CommunicationState.Closing))
                    factory = null;
                using (Locker.LockWrite(readerwriterlock))
                {
                    if (factory == null)
                    {
                        factory = new ChannelFactory<TChannel>(EndPointConfigurationName);
                        Factory.Endpoint.Behaviors.Add(new ContextPropagationBehaviorAttribute());
                    }
                }
                return factory;
            }
        }

        public static readonly ChannelProxy<TChannel> Instance = new ChannelProxy<TChannel>();

        public TChannel GetChannel()
        {
            return Factory.CreateChannel();
        }
        public void PutChannel(TChannel channel)
        {
            ICommunicationObject comobj = channel as ICommunicationObject;
            switch (comobj.State)
            {
                case CommunicationState.Created:
                case CommunicationState.Opening:
                case CommunicationState.Opened:
                    comobj.Close();
                    break;
                case CommunicationState.Faulted:
                    comobj.Abort();
                    break;
            }
        }
    }
    internal class ServiceProxy<TChannel> : RealProxy where TChannel : class
    {
        public ServiceProxy()
            : base(typeof(TChannel))
        {

        }
        public override IMessage Invoke(IMessage msg)
        {

            IMethodCallMessage methodcall = msg as IMethodCallMessage;
            object[] paras = new object[methodcall.ArgCount];
            methodcall.Args.CopyTo(paras, 0);
            IMethodReturnMessage methodreturn = null;
            TChannel channel = ChannelProxy<TChannel>.Instance.GetChannel();
            try
            {
                object value = methodcall.MethodBase.Invoke(channel, paras);
                methodreturn = new ReturnMessage(value, paras, methodcall.ArgCount, methodcall.LogicalCallContext, methodcall);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is CommunicationException || ex.InnerException is TimeoutException)
                    (channel as ICommunicationObject).Abort();
                if (ex.InnerException != null)
                    methodreturn = new ReturnMessage(ex.InnerException, methodcall);
                else
                    methodreturn = new ReturnMessage(ex, methodcall);
            }
            finally
            {
                ChannelProxy<TChannel>.Instance.PutChannel(channel);
            }
            return methodreturn;
        }
    }
    public class ServiceProxyFactory
    {
        public static TChannel CreateChannel<TChannel>() where TChannel : class
        {
            ServiceProxy<TChannel> proxy = new ServiceProxy<TChannel>();
            return (TChannel)proxy.GetTransparentProxy();
        }
    }
}
