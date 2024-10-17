using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Business
{
    public class Pallet
    {
        public string DropId { get; set; }
        public string OrderStatus { get; set; }
        public string HostProcesssRequired { get; set; }
        public string OrderTransport { get; set; }
        public string TransportDescription { get; set; }
        public string Transport { get; set; }
        public string NewDropId { get; set; }
        public string Group { get; set; }
        public string AddWho { get; set; }
        public string IsClosed { get; set; }
    }
}
