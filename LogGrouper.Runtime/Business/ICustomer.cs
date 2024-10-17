using LogGrouper.Models.Business;
using LogGrouper.Models.Global;
using LogGrouper.Models.Request;
using LogGrouper.Models.Response;
using LogGrouper.Runtime.Common;
using Loginter.Common.Tools.Cryptography;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LogGrouper.Runtime.Business
{
    public interface ICustomer
    {
        public string GetPrinters(string cnn, string whs, string storerkey);
        public string SetPrinters(User user, List<Printer> printers);
        public Response ValidateGrouping(RequestGrouping requestGrouping);
        public Response CreatePallet(RequestGrouping requestGrouping, User user);
        public Response CreatePalletDetail(RequestGrouping requestGrouping, User user);
        public bool ValidatePalletDetail(string lbl);
        public List<PalletDetail> ValidateClosePallet(string dropId);
        public Response ClosePallet(Pallet pallet, string username, string printerPack, string printerLabel);
        public Response GetDiference(string dropId);
        public Response PalletByUser(string username);
        public Response PalletToAudit();
        public Response AuditPallet(RequestAudit requestAudit);
        public Response PackageQty(string dropid);
        public Response RemovePackage(RequestGrouping requestGrouping);
        public Response GetPacking(string dropId);
        public void PrintLabel(string dropid, string transport, string group, string printer, string storerkey);
        public void PrintPackingList(string dropid, string username, string printer);
        public void PrintRotulo(string dropid, string username, string printer);
        public void PrintFinalLabel(string dropId, string transport, string group, string printer);
        public string PalletValidation(string orderId, string packageId, string username, string isEvent);
        public void ReprintLabel(string dropId, string printer);
    }
}
