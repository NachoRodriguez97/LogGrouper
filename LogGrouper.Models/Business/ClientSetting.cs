using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Business
{
    public class ClientSetting
    {
        public int Id { get; set; }
        public string Module { get; set; }
        public string Functionality { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime Ts { get; set; }
        public string EditBy { get; set; }
        public int Client { get; set; }
        public bool IsAdmin { get; set; }
    }
}
