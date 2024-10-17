using LogGrouper.Models.Business;
using LogGrouper.Models.Global;
using LogGrouper.Models.Request;
using LogGrouper.Models.Response;
using LogGrouper.Runtime.Common;
using Loginter.Common.Tools.Cryptography;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using System.Globalization;


namespace LogGrouper.Runtime.Business
{
    public class Farmaecom : CustomerBase
    {
        private readonly Filter _filter = new();

        public Farmaecom(Filter filter)
        {
            _filter = filter;
        }

        public string GetEventStatus(string storerkey)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetEventStatus", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("No hay evento cargado.");

                string isEvent = data.Rows[0]["ACTIVE"].ToString();

                return isEvent;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public override Response ValidateGrouping(RequestGrouping requestGrouping)
        {
            try
            {
                if (!ValidatePalletDetail(requestGrouping.QrInfo))
                    throw new Exception("Debe escanear el QR.");


                PalletDetail info = QrInfo(requestGrouping);

                dynamic order = OrderExist(info.OrderId);

                if (string.IsNullOrEmpty(order.order))
                    throw new Exception("No existe Pedido");

                string log = PackageExist(order.order, info.Package);

                if (string.IsNullOrEmpty(log))
                {
                    string[] stringArray = order.group != null ? order.group.Split('-') : "";
                    var group = stringArray.ToList().Last();

                    if (order.transportDescription != requestGrouping.Pallet.TransportDescription || requestGrouping.Pallet.Group.ToUpper().Contains(group))
                        throw new Exception("El pedido no corresponde a este grupo o transporte.");

                    info.Group = group;
                    info.DropId = requestGrouping.Pallet.DropId;

                    return new Response { IsSuccess = true, Result = JsonSerializer.Serialize(info) };
                }
                else
                {
                    return new Response { IsSuccess = false, Message = $"El pedido ya se encuentra agrupado en: {log}" };
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public PalletDetail QrInfo(RequestGrouping requestGrouping)
        {
            try
            {
                string qrInfo = Regex.Replace(requestGrouping.QrInfo.ToUpper(), "[()]", "");

                Regex regex1 = new(@"ID_PEDIDO:([^,]+).*ID_NROBULTO:([^,]+)");
                Regex regex2 = new(@"ID_PUP:([^,]+).*ID_ET:([^,]+)");

                Match match1 = regex1.Match(qrInfo);
                Match match2 = regex2.Match(qrInfo);

                PalletDetail info = new();
                if (match1.Success && match2.Success)
                {
                    string orderId = match1.Groups[1].Value.Trim();
                    info.OrderId = orderId.Contains(" - ") ? orderId.Split(" - ")[0].Trim() : orderId;
                    info.Package = match1.Groups[2].Value.Trim();

                    info.Transport = match2.Groups[1].Value.Trim();
                    info.Barcode = match2.Groups[2].Value.Trim();
                }
                else
                {
                    throw new Exception("Error al obtener el número de pedido y/o bulto");
                }

                return info;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override Response CreatePallet(RequestGrouping requestGrouping, User user)

        {
            try
            {
                PalletDetail info = QrInfo(requestGrouping);

                dynamic order = OrderExist(info.OrderId);

                if (string.IsNullOrEmpty(order.order))
                    throw new Exception("No existe Pedido");

                string isEvent = GetEventStatus(_filter.Storerkey);

                string dropId = PalletValidation(info.OrderId, info.Package, user.Username, isEvent);

                Pallet log = GetDropId(dropId, info.OrderId, info.Package, user.Username, isEvent);

                //If the drop id isn't null, the pallet exists.
                if (!string.IsNullOrEmpty(log.DropId))
                {
                    info.DropId = log.DropId;

                    return new Response { IsSuccess = true, Token = requestGrouping.Token, Result = JsonSerializer.Serialize(info), Message = $"Pallet existente: {log.DropId}" };
                }
                else if (!string.IsNullOrEmpty(log.NewDropId))
                {
                    info.NewDropId = log.NewDropId;
                    info.TransportDescription = log.TransportDescription;

                    Printer lblPrinter = user.Printers.Find(x => x.Type.ToUpper().Trim() == "PRINTLBL");
                    string[] stringArray = order.group != null ? order.group.Split('-') : "";
                    var group = stringArray.ToList().Last();
                    PrintLabel(info.NewDropId, info.TransportDescription, group, lblPrinter.Name, _filter.Storerkey);

                    return new Response { IsSuccess = true, Token = requestGrouping.Token, Result = JsonSerializer.Serialize(info), Message = log.NewDropId };
                }
                else
                {
                    return new Response { IsSuccess = false, Token = requestGrouping.Token, Message = "Error al crear el pallet." };
                }
            }
            catch
            {
                throw;
            }
        }

        public override Response CreatePalletDetail(RequestGrouping requestGrouping, User user)
        {
            try
            {
                //if the label scanned was the qr, it's wrong
                if (ValidatePalletDetail(requestGrouping.QrInfo))
                    throw new Exception("Debe escanear la etiqueta de pallet.");

                string dropid = string.IsNullOrEmpty(requestGrouping.Pallet.DropId) ? requestGrouping.Pallet.NewDropId : requestGrouping.Pallet.DropId;

                if (requestGrouping.QrInfo.ToUpper().Trim() != dropid.ToUpper().Trim())
                    throw new Exception("El pedido no corresponde al pallet.");

                AddPackage(user.Username, requestGrouping.Pallet);

                return new Response { IsSuccess = true, Token = requestGrouping.Token, Result = JsonSerializer.Serialize(requestGrouping.Pallet), Message = "Pedido agregado correctamente." };
            }
            catch
            {
                throw;
            }
        }

        public override bool ValidatePalletDetail(string qrInfo)
        {
            try
            {
                bool isSucces = false;

                string qrInfoRegex = Regex.Replace(qrInfo.ToUpper(), "[()]", "");

                Regex regex1 = new(@"ID_PEDIDO:([^,]+).*ID_NROBULTO:([^,]+)");
                Match match1 = regex1.Match(qrInfoRegex);

                if (match1.Success)
                    isSucces = true;

                return isSucces;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public object OrderExist(string orderId)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("OrderExist", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Order_Id}", orderId);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("Error al obtener el pedido");

                var orderObj = new
                {
                    order = data.Rows[0]["ORDER"].ToString(),
                    group = data.Rows[0].IsNull("SUSR3") ? null : data.Rows[0]["SUSR3"].ToString(),
                    transportDescription = data.Rows[0].IsNull("SUSR4") ? null : data.Rows[0]["SUSR4"].ToString()
                };

                return orderObj;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public override string PalletValidation(string orderId, string packageId, string username, string isEvent)
        {
            try
            {
                DataTable data = null;
                string dropId = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("PalletExist", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Order_Id}", orderId);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    return ""; //Retorono vacio porque no existe pallet para ese pedido

                List<PalletDetail> pallets = new();

                foreach (DataRow r in data.Rows)
                {
                    PalletDetail pallet = new()
                    {
                        DropId = Convert.ToString(r["DROPID"]),
                        Package = Convert.ToString(r["BULTOS"]),
                        AddWho = Convert.ToString(r["USUARIO"]),
                        OrderId = Convert.ToString(r["NUMEROPEDIDO"]),
                        IsClosed = Convert.ToString(r["ISCLOSED"]),
                        OrderStatus = Convert.ToString(r["STATUS"]), //Estado del pedido en la tabla ORDERS
                        Group = Convert.ToString(r["ORDERGROUP"]), //Campo ORDERGROUP de la tabla ORDERS
                        HostProcesssRequired = Convert.ToString(r["ISCANCELLED"]) //Marca de cancelado en la tabla ORDERS
                    };

                    pallets.Add(pallet);
                }

                var found = pallets.Find(x => x.Package.ToUpper() == packageId.ToUpper());
                dropId = found?.DropId ?? ""; //Vacio si el paquete no esta en pallet si no seteo el pallet

                if (found == null)
                    found = pallets.First();

                if (found.OrderStatus == "95" && !string.IsNullOrEmpty(found.Group))
                    throw new Exception($"El pedido ya fue expedido en el {found.Group}.");

                if (found.OrderStatus == "75" && !string.IsNullOrEmpty(found.Group)) //Agrupado en un pallet cerrado
                    throw new Exception($"El pedido ya se encuentra agrupado en el {found.Group}.");

                if (found.HostProcesssRequired == "1")
                    throw new Exception("Pedido cancelado.");

                if (found.OrderStatus != "75" && found.OrderStatus != "95")
                    throw new Exception($"Orden en estado incorrecto.");

                if (!string.IsNullOrEmpty(dropId)) //Agrupado en un pallet en curso
                    throw new Exception($"El pedido ya se encuentra agrupado en el {dropId}");

                if (isEvent == "1" && !string.IsNullOrEmpty(pallets.FirstOrDefault()?.DropId)) //Modo evento
                {
                    bool isMine = pallets.Exists(x => x.AddWho.ToUpper() == username.ToUpper());
                    if (!isMine)
                        throw new Exception($"El pedido corresponde al {pallets.FirstOrDefault()?.DropId}");

                    dropId = pallets.FirstOrDefault()?.DropId;
                }

                return dropId;
            }
            catch (Exception)
            {
                throw;
            }
        }

        //Return existing DropId if the pallet exists or creates a new one.
        public Pallet GetDropId(string dropId, string orderId, string packageId, string username, string isEvent)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("CreatePallet", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Drop_Id}", dropId);
                    template.Replace("{Order_Id}", orderId);
                    template.Replace("{Package_Id}", packageId);
                    template.Replace("{User}", username);
                    template.Replace("{Storerkey}", _filter.Storerkey);
                    template.Replace("{IsEvent}", isEvent);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("Error crear el pallet");

                Pallet pallet = new();

                foreach (DataRow r in data.Rows)
                {

                    if (data.Columns.Contains("DropId"))
                        pallet.DropId = r["DropId"]?.ToString();

                    if (data.Columns.Contains("NewDropId"))
                        pallet.NewDropId = r["NewDropId"] != DBNull.Value ? r["NewDropId"].ToString() : null;

                    if (data.Columns.Contains("TransportDescription"))
                        pallet.TransportDescription = r["TransportDescription"] != DBNull.Value ? r["TransportDescription"].ToString() : null;
                }

                if (!string.IsNullOrEmpty(pallet.DropId) && pallet.DropId.Contains("ERROR"))
                {
                    var msg = pallet.DropId.Split(':');
                    throw new Exception(msg[1]);
                }

                return pallet;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //If the package doesn´t exist add it
        public PalletDetail AddPackage(string username, PalletDetail pallet)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("CreatePalletDetail", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Order_Id}", pallet.OrderId);
                    template.Replace("{Package_Id}", pallet.Package);
                    template.Replace("{User}", username);
                    template.Replace("{Storerkey}", _filter.Storerkey);
                    template.Replace("{Label_Id}", pallet.Barcode);
                    template.Replace("{IsEvent}", GetEventStatus(_filter.Storerkey));

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("Error al agregar el pedido");

                foreach (DataRow r in data.Rows)
                {
                    pallet.OrderId = r["OrderId"]?.ToString();
                }

                if (pallet.OrderId.Contains("ERROR"))
                    throw new Exception(pallet.OrderId);

                return pallet;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Validate if the order and package exist on the pallet
        public string PackageExist(string orderkey, string packageId)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("PackageExist", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Orderkey}", orderkey);
                    template.Replace("{Package_Id}", packageId);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count > 0)
                    return data.Rows[0]["DROPID"]?.ToString();

                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override Response ClosePallet(Pallet pallet, string username, string printerPack, string printerLabel)
        {
            try
            {
                List<PalletDetail> list = ValidateClosePallet(pallet.DropId);
                Response response = new();

                if (list.Count == 0)
                {
                    DataTable data = null;

                    using (ConnectionDB connection = new(_filter.ConnectionString))
                    {
                        StringBuilder template = new(
                            SharedFunctions.ReadDocument("ClosePallet", "Grouping")
                            );

                        template.Replace("{Scheme}", _filter.Warehouse);
                        template.Replace("{Drop_Id}", pallet.DropId);

                        data = connection.GetCustomSelectQuery(template.ToString());
                    }

                    PrintPackingList(pallet.DropId, username, printerPack);
                    PrintRotulo(pallet.DropId, username, printerPack);
                    PrintFinalLabel(pallet.DropId, pallet.Transport, pallet.Group, printerLabel);

                    response = new() { IsSuccess = true, Message = $"Pallet: {pallet.DropId} - {pallet.Group} cerrado correctamente." };
                }
                else
                {
                    response = new() { IsSuccess = false, Message = "Error Pallet con diferencia", Result = JsonSerializer.Serialize(list) };
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override List<PalletDetail> ValidateClosePallet(string dropId)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("ValidateClosePallet", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Drop_Id}", dropId);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                List<PalletDetail> list = new();
                if (data.Rows.Count <= 0)
                    return list;

                foreach (DataRow r in data.Rows)
                {
                    PalletDetail pack = new();
                    pack.OrderId = r["OrderId"]?.ToString();
                    pack.DropId = r["DropId"]?.ToString();

                    list.Add(pack);
                }

                return list;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override Response GetDiference(string dropId)
        {
            try
            {
                return new Response { IsSuccess = true, Result = JsonSerializer.Serialize(ValidateClosePallet(dropId)) };
            }
            catch
            {
                throw;
            }
        }

        //Get all the pallets opened by user
        public override Response PalletByUser(string username)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetPalletByUser", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{User}", username);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("El usuario no tienen ningun pallet en curso.");

                List<PalletDetail> pallets = new();

                foreach (DataRow r in data.Rows)
                {
                    PalletDetail pallet = new() { DropId = r["DROPID"]?.ToString(), Group = r["GRUPO"]?.ToString(), AddWho = r["USERNAME"].ToString(), Transport = r["TRANSPORT"].ToString(), Package = r["QTY"]?.ToString() };
                    pallets.Add(pallet);
                }

                return new Response { IsSuccess = true, Result = JsonSerializer.Serialize(pallets) };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override Response PackageQty(string dropid)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetPackageQty", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Drop_Id}", dropid);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                //if (data.Rows.Count <= 0)
                //    throw new Exception("No se puede cerrar un pallet sin pedidos agrupados.");

                return new Response { IsSuccess = true, Result = data.Rows.Count.ToString() };
            }
            catch
            {
                throw;
            }
        }

        public override Response GetPacking(string dropid)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetPacking", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Drop_Id}", dropid);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("Error al buscar los paquetes del pallet.");

                List<PalletDetail> packages = new();

                foreach (DataRow r in data.Rows)
                {
                    PalletDetail pack = new();
                    pack.OrderId = r["OrderId"]?.ToString();
                    pack.Package = r["Qty"]?.ToString();

                    packages.Add(pack);
                }

                return new Response { IsSuccess = true, Result = JsonSerializer.Serialize(packages) };
            }
            catch
            {
                throw;
            }
        }

        public override Response RemovePackage(RequestGrouping requestGrouping)
        {
            try
            {
                PalletDetail info = QrInfo(requestGrouping);

                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("RemovePackage", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{OrderId}", info.OrderId);
                    template.Replace("{PackageId}", info.Package);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                string deleted = data.Rows[0]["ELIMINADO"].ToString();

                if (Convert.ToInt16(deleted) == 0)
                    throw new Exception("Error al eliminar el pedido.");

                return new Response { IsSuccess = true, Message = $"Bulto: {info.Package}, Pedido: {info.OrderId}" };
            }
            catch
            {
                throw;
            }
        }

        public override void ReprintLabel(string dropid, string printer)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                string label = base.GetZpl("zpl_pallet_generico", _filter.Storerkey);

                DataTable dt = GetPalletDetail(dropid);

                base.PrintLabel(dt.Rows[0]["GROUPID"].ToString(), dt.Rows[0]["DEST"].ToString(), "", printer, _filter.Storerkey);
            }
            catch
            {
                throw;
            }
        }

        public override void PrintPackingList(string dropid, string username, string printer)
        {
            try
            {
                List<PalletDetail> palletList = new();

                DataTable dt = GetPalletDetail(dropid);

                string template = base.GetZpl("epl_FARMA_pallet_control", _filter.Storerkey);

                string html = FillPackingListData(dt, template);
                byte[] pdfBytes = SharedFunctions.ConvertHtmlToPdf(html);

                string fileName = $"{Guid.NewGuid()}.pdf";

                /*GUARDO EN CARPETA TEMPORAL PARA VER EL ARCHIVO*/
                string tempFolderPath = Path.GetTempPath();
                string tempPdfPath = Path.Combine(tempFolderPath, fileName);
                File.WriteAllBytes(tempPdfPath, pdfBytes);
                /**/

                RequestPrintPacking json = new()
                {
                    Token = AppSettings.PrinterApiToken,
                    Impresora = printer,
                    Nombre = fileName,
                    Contenido = pdfBytes,
                    Servidor = AppSettings.PrintersSrv
                };

                for (int i = 0; i < 3; i++)
                {
                    using ApiHelper api = new();
                    var response = api.PrintPacking(AppSettings.PrinterApi, JsonSerializer.Serialize(json));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override void PrintRotulo(string dropid, string username, string printer)
        {
            try
            {
                List<PalletDetail> palletList = new();

                DataTable dt = GetPalletDetail(dropid);
                Pallet pallet = GetGroupAndTransport(dropid);

                string templateRotulo = base.GetZpl("epl_FARMA_pallet_Rotulo", _filter.Storerkey);

                string html = FillRotuloData(dt, pallet.Group, pallet.Transport, templateRotulo, username);
                byte[] pdfBytes = SharedFunctions.ConvertHtmlToPdf(html);

                string fileName = $"{Guid.NewGuid()}.pdf";

                /*GUARDO EN CARPETA TEMPORAL PARA VER EL ARCHIVO*/
                string tempFolderPath = Path.GetTempPath();
                string tempPdfPath = Path.Combine(tempFolderPath, fileName);
                File.WriteAllBytes(tempPdfPath, pdfBytes);

                RequestPrintPacking json = new()
                {
                    Token = AppSettings.PrinterApiToken,
                    Impresora = printer,
                    Nombre = fileName,
                    Contenido = pdfBytes,
                    Servidor = AppSettings.PrintersSrv
                };

                for (int i = 0; i < 2; i++)
                {
                    using ApiHelper api = new();
                    var response = api.PrintPacking(AppSettings.PrinterApi, JsonSerializer.Serialize(json));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override void PrintFinalLabel(string dropId, string transport, string group, string printer)
        {
            try
            {
                DataTable dt = GetPalletDetail(dropId);
                string label = base.GetZpl("zpl_pallet_generico", _filter.Storerkey);

                var packages = dt.AsEnumerable()
                               .Where(row => row["ORDERKEY"] != DBNull.Value)
                               .Select(row => row["ORDERKEY"])
                               .ToList();

                var orders = packages.Distinct().ToList();

                string lpn = GetLpn(dropId);

                if (string.IsNullOrEmpty(transport) && string.IsNullOrEmpty(group))
                {
                    Pallet pallet = GetGroupAndTransport(dropId);
                    group = pallet.Group;
                    transport = pallet.Transport;
                }

                label = FillLabelData(label, dropId, transport, group, orders.Count.ToString(), packages.Count.ToString(), lpn);

                StringBuilder sb = new();
                sb.Append(label);
                ZebraPrinter.ZebraPrinter.SendToPrint(sb, printer);

            }
            catch (Exception)
            {

                throw;
            }
        }

        public string FillRotuloData(DataTable dt, string group, string transport, string template, string username)
        {
            try
            {

                template = template.Replace("{PUP}", "");
                string dropid = dt.Rows[0]["GROUPID"].ToString();
                template = template.Replace("{LOG}", dropid);
                string barcode = GetLpn(dropid);
                template = template.Replace("{BARCODE}", string.IsNullOrEmpty(barcode) ? "0" : barcode);
                template = template.Replace("{Grupo}", transport.ToUpper().Equals("LOGINTER") ? "FARMACITY" : transport);
                template = template.Replace("{Date}", DateTime.Now.ToShortDateString());
                template = template.Replace("{Hour}", DateTime.Now.ToString("HH:mm"));
                template = template.Replace("{OPERARIO}", username);

                if (!string.IsNullOrEmpty(group))
                {
                    if (group.Contains("A") || group.Contains("B"))
                    {
                        template = template.Replace("{FREC}",
                            @"<tr><td colspan='2'  style = 'font-size: 100px;text-align: center'>" + group.ToUpper()) + "</ td> </ tr>";
                    }
                    else
                    {
                        template = template.Replace("{FREC}", "");
                    }
                }
                else
                {
                    template = template.Replace("{FREC}", "");
                }

                string result = GetHour(dt.Rows[0]["GROUPID"].ToString());
                int deliveryHour = string.IsNullOrEmpty(result) ? 0 : Convert.ToInt32(result);
                CultureInfo ci = new CultureInfo("Es-Es");
                if (group.Contains("A") || group.Contains("B"))
                {
                    if (DateTime.Now.Hour > 3 && DateTime.Now.Hour <= 6)
                    {
                        template = template.Replace("{Despachar}", ci.DateTimeFormat.GetDayName(DateTime.Now.DayOfWeek));
                        template = template.Replace("{Dia}", DateTime.Now.ToShortDateString());
                    }
                    else
                    {
                        var fecha = DateTime.Now.AddDays(1);
                        template = template.Replace("{Despachar}", ci.DateTimeFormat.GetDayName(fecha.DayOfWeek));
                        template = template.Replace("{Dia}", fecha.ToShortDateString());
                    }
                }
                else if (deliveryHour > DateTime.Now.Hour)
                {
                    template = template.Replace("{Despachar}", ci.DateTimeFormat.GetDayName(DateTime.Now.DayOfWeek));
                    template = template.Replace("{Dia}", DateTime.Now.ToShortDateString());
                }
                else
                {
                    var fecha = DateTime.Now.AddDays(1);
                    template = template.Replace("{Despachar}", ci.DateTimeFormat.GetDayName(fecha.DayOfWeek));
                    template = template.Replace("{Dia}", fecha.ToShortDateString());
                }

                var codebar = SharedFunctions.GenerateCodeBar(barcode);
                using (var image = SKImage.FromBitmap(SKBitmap.FromImage(codebar)))
                using (var ms = new MemoryStream())
                {
                    image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(ms);
                    var imageBytes = ms.ToArray();
                    var qrBase64String = Convert.ToBase64String(imageBytes);
                    template = template.Replace("{IMG}", "data:image/png;base64," + qrBase64String);
                }

                return template;
            }
            catch
            {
                throw;
            }
        }

        public string GetLpn(string dropid)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetLpn", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Drop_Id}", dropid);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("Pallet en estado incorrecto.");

                return data.Rows[0]["Lpn"].ToString();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string FillPackingListData(DataTable dt, string template)
        {
            try
            {
                string pdfText = "";

                string htmldetails = "<tr><td>{PALLET}</td><td>{ORDER}</td><td>{EXTERNORDER}</td><td>{BULTO}</td><td>{RTO}</td><td>{FRECUENCY}</td></tr>";

                string fulldetails = "";

                var lstOrder = new List<string>();
                var lstPacks = new List<string>();

                string headPallet = "";
                string headDestiny = "";

                int countlines = 0;

                string tableHeader = "<table width='100%' border=1 cellpadding=2><tr><th>PALLET</th><th>ORDER</th><th>EXTERNORDER</th><th>BULTO</th><th>RTO</th><th>FREC</th></tr>";

                fulldetails = tableHeader;

                int packageQty = 0;

                foreach (DataRow dr in dt.Rows)
                {
                    var details = htmldetails;

                    string order = dr["ORDERKEY"].ToString();
                    string pack = dr["PACKAGE"].ToString();
                    //string originalpack = dr["ORIGINALPACKAGE"].ToString();

                    headPallet = dr["GROUPID"].ToString();
                    headDestiny = dr["DEST"].ToString().ToUpper().Equals("LOGINTER") ? "FARMACITY" : dr["DEST"].ToString();

                    details = details.Replace("{PALLET}", headPallet);
                    details = details.Replace("{ORDER}", order);
                    details = details.Replace("{EXTERNORDER}", dr["EXTERNORDERKEY"].ToString());
                    details = details.Replace("{BULTO}", pack);
                    details = details.Replace("{RTO}", dr["EXTERNALORDERKEY2"].ToString());
                    details = details.Replace("{FRECUENCY}", dr["FRECUENCIA"].ToString());

                    fulldetails += details;

                    if (countlines >= 35)
                    {
                        fulldetails += "</table>";
                        fulldetails += "<div style=\"page-break-after:always\"></div>";
                        fulldetails += tableHeader;
                        countlines = 0;
                    }

                    if (!lstOrder.Exists(x => x == order))
                    { lstOrder.Add(order); }

                    if (!lstPacks.Exists(x => x == pack))
                    { lstPacks.Add(pack); }


                    countlines++;
                }

                template = template.Replace("{PALLET}", headPallet);
                template = template.Replace("{DEST}", headDestiny);
                var fecha = DateTime.Now.ToString();
                template = template.Replace("{FECHA}", fecha);

                fulldetails += "</table>";

                fulldetails += "<h3><b>TOTAL ORDENES.</b> " + lstOrder.Count.ToString() + "</h3>";
                fulldetails += "<h3><b>TOTAL BULTOS.</b> " + lstPacks.Count.ToString() + "</h3>";

                template = template.Replace("{DETAILS}", fulldetails);

                //var qrImageBytes = SharedFunctions.GenerarQR(headPallet);
                //using (var ms = new MemoryStream())
                //{
                //    bitmap.Save(ms, ImageFormat.Jpeg);
                //    var SigBase64 = Convert.ToBase64String(ms.ToArray());
                //    template = template.Replace("{IMG}", "data:image/gif;base64," + SigBase64);
                //}

                SKImage qrImage = SharedFunctions.GenerateCodeBar(headPallet);
                using (var image = SKImage.FromBitmap(SKBitmap.FromImage(qrImage)))
                using (var ms = new MemoryStream())
                {
                    image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(ms);
                    var imageBytes = ms.ToArray();
                    var qrBase64String = Convert.ToBase64String(imageBytes);
                    template = template.Replace("{IMG}", "data:image/png;base64," + qrBase64String);
                }

                //var qrBase64String = Convert.ToBase64String(qrImageBytes);
                //template = template.Replace("{IMG}", "data:image/png;base64," + qrImage);


                pdfText += template;

                return pdfText;
            }
            catch
            {
                throw;
            }

        }

        public DataTable GetPalletDetail(string dropid)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetPalletDetail", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Drop_Id}", dropid);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("Error al obtener la información del pallet.");

                return data;
            }
            catch
            {
                throw;
            }
        }

        public Pallet GetGroupAndTransport(string dropid)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetGroupAndTransport", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Drop_Id}", dropid);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                Pallet pallet = new();

                if (data.Rows.Count <= 0)
                    return pallet;

                pallet.Group = data.Rows[0]["GROUP"].ToString();
                pallet.Transport = data.Rows[0]["TRANSPORT"].ToString();

                return pallet;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public string GetHour(string dropid)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetHour", "Grouping")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Drop_Id}", dropid);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    return "";

                string hour = data.Rows[0]["HOUR"] != DBNull.Value ? data.Rows[0]["HOUR"].ToString() : "";

                return hour;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public override Response PalletToAudit()
        {
            throw new NotImplementedException();
        }

        public override Response AuditPallet(RequestAudit requestAudit)
        {
            throw new NotImplementedException();
    }
}
}
