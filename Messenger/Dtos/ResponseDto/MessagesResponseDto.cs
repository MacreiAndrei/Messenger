using Messenger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Dtos.ResponseDto
{
    public class MessagesResponseDto
    {
        public List<Message> messages { get; set; }
    }
}
