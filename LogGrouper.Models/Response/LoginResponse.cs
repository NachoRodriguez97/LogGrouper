using LogGrouper.Models.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Response
{
    public class LoginResponse : Response
    {
        public string Settings { get; set; }
    }
}
