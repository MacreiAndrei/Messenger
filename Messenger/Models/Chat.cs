using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Models
{
    public class Chat
    {
        public int chatID { get; set; }
        public string chatName { get; set; }
        public Message? lastMessage { get; set; }
    }
}
