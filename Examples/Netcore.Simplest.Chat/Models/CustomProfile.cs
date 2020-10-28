using System;
using Cactus.Chat.Model.Base;

namespace Netcore.Simplest.Chat.Models
{
    public class CustomProfile : IChatProfile
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }
        public Uri Avatar { get; set; }
    }
}