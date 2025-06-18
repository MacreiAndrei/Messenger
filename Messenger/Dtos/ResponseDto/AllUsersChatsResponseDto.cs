using Messenger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Dtos.ResponseDto
{
    public class AllUsersChatsResponseDto
    {
        public List<Chat> chats {  get; set; }
    }
}
