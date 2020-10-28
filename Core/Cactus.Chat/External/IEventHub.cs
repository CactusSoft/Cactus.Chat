using System.Threading.Tasks;

namespace Cactus.Chat.External
{
    public interface IEventHub
    {
        Task FireEvent<T>(T msg);
    }
}
