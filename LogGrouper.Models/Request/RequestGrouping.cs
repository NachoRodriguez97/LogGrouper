using LogGrouper.Models.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Request
{
    public class RequestGrouping
    {
        public string QrInfo { get; set; }
        public PalletDetail Pallet { get; set; }
        public string Token { get; set; }
    }
}
