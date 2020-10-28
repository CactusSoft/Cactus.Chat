using System.Collections.Generic;

namespace Cactus.Chat.Transport.Models.Input
{
    public class AddParticipantsCommand
    {
        public IList<string> Ids { get; set; }
    }
}
