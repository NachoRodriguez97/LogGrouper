using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Request
{
    public class RequestAudit
    {
        public int Qty { get; set; }
        public string DropId { get; set; }
        public string Token { get; set; }
    }
}
