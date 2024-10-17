using LogGrouper.Models.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Request
{
    public class RequestPrinter
    {
        public List<Printer> Printers { get; set; }
        public string Token { get; set; }
    }
}
