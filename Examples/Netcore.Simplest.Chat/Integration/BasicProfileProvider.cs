using System;
using System.Threading.Tasks;
using Cactus.Chat.External;
using Cactus.Chat.Model.Base;
using Netcore.Simplest.Chat.Models;

namespace Netcore.Simplest.Chat.Integration
{
    public class BasicProfileProvider : IUserProfileProvider<CustomProfile>
    {
        public Task<IContextUser<CustomProfile>> Get(IAuthContext authContext)
        {
            return Get(authContext.GetUserId());
        }

        public Task<IContextUser<CustomProfile>> Get(string userId)
        {
            return Task.FromResult<IContextUser<CustomProfile>>(new User
            {
                Id = userId,
                Profile = new CustomProfile()
                {
                    FirstName = "Mr " + userId,
                    Thumbnail = new Uri("http://thecatapi.com/api/images/get?format=src&type=png")
                }
            });
        }
    }
}