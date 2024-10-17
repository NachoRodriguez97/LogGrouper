using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Request
{
    public class RequestPrintPacking
    {
        public string Token { get; set; }
        public string Impresora { get; set; }
        public string Nombre { get; set; }
        public string Servidor { get; set; }
        public byte[] Contenido { get; set; }
    }
}
