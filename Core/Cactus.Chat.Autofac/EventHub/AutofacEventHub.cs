using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Cactus.Chat.External;
using Cactus.Chat.Logging;

namespace Cactus.Chat.Autofac
{
    public class AutofacEventHub : IEventHub
    {
        private readonly ILog log = LogProvider.GetLogger(typeof(AutofacEventHub));
        private readonly IComponentContext container;

        public AutofacEventHub(IComponentContext container)
        {
            this.container = container;
        }

        public async Task FireEvent<T>(T msg)
        {
            var handlerExist = false;
            var handlers = container.Resolve<IEnumerable<IEventHandler<T>>>();
            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        await handler.Handle(msg).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        log.ErrorFormat("Fail to execute event {0} in handler {1}: {2}", typeof(T).Name, handler.GetType(), e);
                    }
                    handlerExist = true;
                }
            }

            if (!handlerExist)
            {
                log.WarnFormat("No handler found for {0}", typeof(T));
            }
        }
    }
}
