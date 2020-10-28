using System.Threading.Tasks;

namespace Cactus.Chat.Autofac
{
    /// <summary>
    /// Used to get event handlers from IContainer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEventHandler<in T>
    {
        Task Handle(T msg);
    }
}