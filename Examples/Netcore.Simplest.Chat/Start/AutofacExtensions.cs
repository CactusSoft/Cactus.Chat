using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Features.Scanning;
using Microsoft.AspNetCore.SignalR;

namespace Netcore.Simplest.Chat.Start
{
    public static class AutofacExtensions
    {
        public static IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> RegisterHubs(this ContainerBuilder builder, params Assembly[] assemblies)
        {
            // typeof(Hub), not typeof(IHub)
            return builder.RegisterAssemblyTypes(assemblies)
                .Where(t => typeof(Hub).IsAssignableFrom(t))
                .ExternallyOwned();
        }
    }
}
