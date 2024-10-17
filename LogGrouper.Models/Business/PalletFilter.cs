using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Business
{
    public class PalletFilter
    {
        public List<string> Groups { get; set; }
        public List<string> Transports { get; set; }
        public List<string> Description{ get; set; }
    }
}
