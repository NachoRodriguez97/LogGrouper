using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Business
{
    public class Printer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Ip { get; set; }
        public string Folder { get; set; }
        public string Type { get; set; }
    }
}
