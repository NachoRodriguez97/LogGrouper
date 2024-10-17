using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Response
{
    public class Response
    {
        public string Message { get; set; }
        public string Result { get; set; }
        public bool IsSuccess { get; set; }
        public string Token { get; set; }
    }
}
