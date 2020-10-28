using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Cactus.Chat.Signalr
{
    public static class HubClientsExtension
    {
        public static async Task Clients(this IHubClients hubClients, IEnumerable<string> connectionIds, string method, object paylod)
        {
            foreach (var connectionId in connectionIds)
            {
                await hubClients.Client(connectionId).SendAsync(method, paylod);
            }
        }

    }
}