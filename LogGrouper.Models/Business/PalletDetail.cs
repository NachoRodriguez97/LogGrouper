using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Models.Business
{
    public class PalletDetail : Pallet
    {
        public string OrderId { get; set; }
        public string Package { get; set; }
        public string Barcode { get; set; }
        public string ReferenceDocument { get; set; }
        public string ExternOrderkey { get; set; }
        public string DropId_PickDetail { get; set; }
        public string Sucursal { get; set; }
        public string Orderkey { get; set; }
        public string Type { get; set; }
    }
}
