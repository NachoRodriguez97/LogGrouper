using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Business
{
    [Serializable]
    public class cEtiqueta
    {
        public long IdEtiqueta { get; set; }

        public string Etiqueta { get; set; }

        public string Usuario { get; set; }

        public string Impresora { get; set; }
    }
}
