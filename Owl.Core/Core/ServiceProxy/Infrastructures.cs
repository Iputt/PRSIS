using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Threading;
using System.ServiceModel.Description;
using Owl.Util;
using Owl.Feature;

namespace Owl
{
    internal class ContextSendInspector : IClientMessageInspector
    {

        #region IClientMessageInspector 成员

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {

        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            request.Headers.Add(new MessageHeader<string>(Cache.SessionId).GetUntypedHeader(Client.SessionIdName,OwlContext.ContextHeaderNamespace));
            string.IsNullOrEmpty(OwlContext.Current.UserName);
            request.Headers.Add(new MessageHeader<OwlContext>(OwlContext.Current).GetUntypedHeader(OwlContext.ContextHeaderlocalName, OwlContext.ContextHeaderNamespace));
            return null;
        }

        #endregion
    }
    internal class ContextReceivalCallContextInitializer : ICallContextInitializer
    {

        #region ICallContextInitializer 成员

        public void AfterInvoke(object correlationState)
        {
            OwlContext.Current.Clear();
        }

        public object BeforeInvoke(InstanceContext instanceContext, IClientChannel channel, Message message)
        {
            Cache.SessionId = message.Headers.GetHeader<string>(Client.SessionIdName, OwlContext.ContextHeaderNamespace);
            OwlContext.Current.CopyFrom(message.Headers.GetHeader<OwlContext>(OwlContext.ContextHeaderlocalName, OwlContext.ContextHeaderNamespace));
            return null;
        }

        #endregion
    }
    public class ContextPropagationBehaviorAttribute : Attribute, IServiceBehavior, IEndpointBehavior
    {

        #region IServiceBehavior 成员

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {

        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers)
            {
                foreach (EndpointDispatcher endpointdispatcher in channelDispatcher.Endpoints)
                {
                    foreach (DispatchOperation operation in endpointdispatcher.DispatchRuntime.Operations)
                    {
                        operation.CallContextInitializers.Add(new ContextReceivalCallContextInitializer());
                    }
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {

        }

        #endregion

        #region IEndpointBehavior 成员

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {

        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new ContextSendInspector());
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            foreach (DispatchOperation operation in endpointDispatcher.DispatchRuntime.Operations)
            {
                operation.CallContextInitializers.Add(new ContextReceivalCallContextInitializer());
            }
        }

        public void Validate(ServiceEndpoint endpoint)
        {

        }

        #endregion
    }
}
