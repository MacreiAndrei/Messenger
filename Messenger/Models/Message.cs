using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Models
{
    public class Message
    {
        public int messageID { get; set; }
        public string content { get; set; }
        public DateTime timeStamp { get; set; }

        public Sender sender { get; set; }
    }
}
