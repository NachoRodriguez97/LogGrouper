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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LogGrouper.Runtime.Business
{
    public abstract class CustomerBase : ICustomer
    {
        public string GetPrinters(string cnn, string whs, string storerkey)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(cnn))
                {
                    StringBuilder template = new(
                    SharedFunctions.ReadDocument("GetPrinters", "Client")
                        );

                    template.Replace("{Scheme}", whs);
                    template.Replace("{Storerkey}", storerkey);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("Error al obtener las impresoras.");

                List<Printer> printers = new();
                foreach (DataRow p in data.Rows)
                {
                    Printer printer = new() { Name = p["PrinterName"].ToString(), Type = p["PrinterType"].ToString() };
                    printers.Add(printer);
                }

                return JsonSerializer.Serialize(printers);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string SetPrinters(User user, List<Printer> printers)
        {
            try
            {
                if (printers.Count > 0)
                {
                    user.Printers = new();
                    foreach (Printer p in printers)
                    {
                        user.Printers.Add(p);
                    }
                }
                else
                {
                    return string.Empty;
                }

                string newToken = Encrypter.Encrypt(JsonSerializer.Serialize(user));

                return newToken;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public abstract Response ClosePallet(Pallet pallet, string username, string printerPack, string printerLabel);

        public abstract Response CreatePallet(RequestGrouping requestGrouping, User user);

        public abstract Response CreatePalletDetail(RequestGrouping requestGrouping, User user);

        public abstract Response GetDiference(string dropId);

        public abstract Response GetPacking(string dropId);

        public abstract Response PackageQty(string dropid);

        public abstract Response PalletByUser(string username);

        public abstract Response PalletToAudit();

        public abstract Response AuditPallet(RequestAudit requestAudit);

        public abstract void PrintFinalLabel(string dropId, string transport, string group, string printer);

        public void PrintLabel(string dropid, string transport, string group, string printer, string storerkey)
        {
            try
            {
                StringBuilder sb = new();

                string label = GetZpl("zpl_pallet_generico", storerkey);
                label = FillLabelData(label, dropid, transport, group);

                sb.Append(label);

                ZebraPrinter.ZebraPrinter.SendToPrint(sb, printer);
            }
            catch
            {
                throw;
            }
        }

        public string GetZpl(string name, string storerkey)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(AppSettings.MainConnString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetZpl", "Grouping")
                        );

                    template.Replace("{LabelName}", name);
                    template.Replace("{Storerkey}", storerkey);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("Error al obtener el zpl.");

                string zpl = string.Empty;
                foreach (DataRow r in data.Rows)
                {
                    zpl = r["Zpl"].ToString();
                }

                return zpl;
            }
            catch
            {
                throw;
            }
        }

        public string FillLabelData(string label, string dropid, string transport, string group, string orders = "", string packages = "", string lpn = "")
        {
            try
            {
                label = label.Replace("{DATE}", DateTime.Now.ToLongDateString());
                label = label.Replace("{LOG}", dropid);
                label = label.Replace("{TRANSPORT}", transport.ToUpper().Equals("LOGINTER") ? "[FARMACITY]," : $"[{transport}],");
                label = label.Replace("{GROUP}", $" - {group}");
                label = label.Replace("{ORDERS}", !string.IsNullOrEmpty(orders) ? $"ORDENES: ({orders}) - " : "");
                label = label.Replace("{PACKAGE}", !string.IsNullOrEmpty(packages) ? $"BULTOS: ({packages})" : "");

                return label;
            }
            catch
            {
                throw;
            }
        }

        public abstract void PrintPackingList(string dropid, string username, string printer);

        public abstract void PrintRotulo(string dropid, string username, string printer);

        public abstract Response RemovePackage(RequestGrouping requestGrouping);

        public abstract void ReprintLabel(string dropId, string printer);

        public abstract List<PalletDetail> ValidateClosePallet(string dropId);

        public abstract Response ValidateGrouping(RequestGrouping requestGrouping);

        public abstract bool ValidatePalletDetail(string lbl);

        public abstract string PalletValidation(string orderId, string packageId, string username, string isEvent);
    }
}
