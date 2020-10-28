using System.Threading.Tasks;
using Cactus.Chat.Model.Base;

namespace Cactus.Chat.External
{
    public interface IUserProfileProvider<T>
        where T : IChatProfile
    {
        Task<IContextUser<T>> Get(IAuthContext authContext);

        Task<IContextUser<T>> Get(string userId);
    }
}
