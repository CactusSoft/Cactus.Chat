using Cactus.Chat.Model.Base;

namespace Netcore.Simplest.Chat.Models
{
    public class User : IContextUser<CustomProfile>
    {
        public string Id { get; set; }
        public bool IsDeleted { get; set; }

        public CustomProfile Profile { get; set; }
    }
}