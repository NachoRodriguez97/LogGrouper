using LogGrouper.Models.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Request
{
    public class RequestCreatePallet
    {
        public string Token { get; set; }
        public Pallet Pallet{ get; set; }
    }
}
