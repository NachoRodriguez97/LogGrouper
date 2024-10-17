using System.Collections.Generic;

namespace LogGrouper.Models.Business
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public Client Client { get; set; }
        public List<Printer> Printers { get; set; }
        public bool IsLogged { get; set; }
    }
}
