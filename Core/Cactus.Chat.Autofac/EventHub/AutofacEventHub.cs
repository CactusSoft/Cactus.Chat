using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Cactus.Chat.External;
using Microsoft.Extensions.Logging;

namespace Cactus.Chat.Autofac
{
    public class AutofacEventHub : IEventHub
    {
        private readonly IComponentContext _container;
        private readonly ILogger<AutofacEventHub> _log;

        public AutofacEventHub(IComponentContext container, ILogger<AutofacEventHub> log)
        {
            this._container = container;
            _log = log;
        }

        public async Task FireEvent<T>(T msg)
        {
            var handlerExist = false;
            var handlers = _container.Resolve<IEnumerable<IEventHandler<T>>>();
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
                        _log.LogError("Fail to execute event {event} in handler {handler}: {exception}", typeof(T).Name, handler.GetType(), e);
                    }
                    handlerExist = true;
                }
            }

            if (!handlerExist)
            {
                _log.LogWarning("No handler found for {event}", typeof(T));
            }
        }
    }
}
