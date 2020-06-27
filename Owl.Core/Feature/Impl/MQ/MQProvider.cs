using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Feature.Impl.MQ
{
    public abstract class MQProvider : Provider
    {
        public abstract void Subscrib(string message, Action<string> callback);

        public abstract void Publish(string message, string body);

    }
}
