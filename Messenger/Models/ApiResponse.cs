using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Models
{
    public class ApiResponse<T>
    {
        public string status { get; set; }
        public T data { get; set; }
        public string error { get; set; }
    }
}
