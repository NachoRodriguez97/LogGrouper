using LogGrouper.Models.Business;
using LogGrouper.Models.Global;
using LogGrouper.Models.Request;
using LogGrouper.Models.Response;
using LogGrouper.Runtime.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace LogGrouper.Runtime.Business
{
    public class Farmacity : CustomerBase
    {
        private readonly Filter _filter = new();

        public Farmacity(Filter filter)
        {
            _filter = filter;
        }

        #region Pallet Handler
        public override Response CreatePallet(RequestGrouping requestGrouping, User user)
        {
            dynamic order = OrderExist(requestGrouping.QrInfo);
            PalletValidation(order);

            Pallet log = GetDropId(user.Username, order[0].type, user.Username, order[0].orderkey);

            PalletDetail detail = new();

            //If the drop id isn't null, the pallet exists.
            if (!string.IsNullOrEmpty(log.DropId))
            {
                detail.DropId = log.DropId;
                detail.Transport = string.IsNullOrEmpty(order[0].transportDescription) ? "0" : order[0].transportDescription;
                detail.DropId_PickDetail = order[0].dropid;
                detail.OrderId = order[0].order;
                detail.ExternOrderkey = order[0].externorderkey;
                detail.Orderkey = order[0].orderkey;
                detail.Sucursal = order[0].sucursal;
                detail.Type = order[0].type;

                return new Response { IsSuccess = true, Token = requestGrouping.Token, Result = JsonSerializer.Serialize(detail), Message = $"Pallet existente: {log.DropId}" };
            }
            else if (!string.IsNullOrEmpty(log.NewDropId))
            {
                detail.NewDropId = log.NewDropId;
                detail.TransportDescription = string.IsNullOrEmpty(log.TransportDescription) ? "0" : log.TransportDescription;
                detail.DropId_PickDetail = order[0].dropid;
                detail.OrderId = order[0].order;
                detail.ExternOrderkey = order[0].externorderkey;
                detail.Orderkey = order[0].orderkey;
                detail.Sucursal = order[0].sucursal;
                detail.Type = order[0].type;

                Printer lblPrinter = user.Printers.Find(x => x.Type.ToUpper().Trim() == "PRINTLBL");
                PrintLabel(detail.NewDropId, detail.TransportDescription, "", lblPrinter.Name, _filter.Storerkey);

                return new Response { IsSuccess = true, Token = requestGrouping.Token, Result = JsonSerializer.Serialize(detail), Message = log.NewDropId };
            }
            else
            {
                return new Response { IsSuccess = false, Token = requestGrouping.Token, Message = "Error al crear el pallet." };
            }
        }

        public Pallet GetDropId(string username, string transport, string user, string orderkey)
        {
            DataTable data = null;

            try
            {
                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("CreatePallet", "GroupingCity")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{User}", username);
                    template.Replace("{Storerkey}", _filter.Storerkey);
                    template.Replace("{Transport}", transport);
                    template.Replace("{User}", user);
                    template.Replace("{Order_Id}", orderkey);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }
            }
            catch (Exception ex)
            {
                throw ex;
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

        public override Response CreatePalletDetail(RequestGrouping requestGrouping, User user)
        {

            string dropid = string.IsNullOrEmpty(requestGrouping.Pallet.DropId) ? requestGrouping.Pallet.NewDropId : requestGrouping.Pallet.DropId;

            if (CityExist(requestGrouping.QrInfo))
                throw new Exception("Las ordenes ya se encuentran agrupadas en el pallet.");

            ValidatePalletDetail(dropid, requestGrouping.QrInfo);

            AddPackage(user.Username, dropid, requestGrouping.QrInfo);

            return new Response { IsSuccess = true, Token = requestGrouping.Token, Result = JsonSerializer.Serialize(requestGrouping.Pallet), Message = "Pedido agregado correctamente." };

        }

        private bool CityExist(string lpn)
        {
            DataTable data = null;

            using (ConnectionDB connection = new(_filter.ConnectionString))
            {
                StringBuilder template = new(
                    SharedFunctions.ReadDocument("CityExist", "GroupingCity")
                    );

                template.Replace("{Scheme}", _filter.Warehouse);
                template.Replace("{Lpn}", lpn);

                data = connection.GetCustomSelectQuery(template.ToString());
            }

            if (data.Rows.Count <= 0)
                return false;

            return true;
        }

        private dynamic GetOrderInfo(string lpn)
        {
            DataTable data = null;

            using (ConnectionDB connection = new(_filter.ConnectionString))
            {
                StringBuilder template = new(
                    SharedFunctions.ReadDocument("GetOrderInfo", "GroupingCity")
                    );

                template.Replace("{Scheme}", _filter.Warehouse);
                template.Replace("{Lpn}", lpn);

                data = connection.GetCustomSelectQuery(template.ToString());
            }

            if (data.Rows.Count <= 0)
                throw new Exception("Error al validar.");

            List<dynamic> orders = new();

            foreach (DataRow r in data.Rows)
            {
                var orderObj = new
                {
                    orderkey = r["ORDERKEY"].ToString(),
                    group = r["GROUP"].ToString(),
                    type = r["TYPE"].ToString(),
                    externorderkey = r["EXTERNORDERKEY"].ToString(),
                    referencedocument = r["REFERENCEDOCUMENT"].ToString(),
                };

                orders.Add(orderObj);
            }

            return orders;
        }


        //If the package doesn´t exist add it
        public void AddPackage(string username, string dropid, string lpn)
        {
            try
            {
                DataTable data = null;

                dynamic orders = GetOrderInfo(lpn);

                foreach (dynamic o in orders)
                {
                    using (ConnectionDB connection = new(_filter.ConnectionString))
                    {
                        StringBuilder template = new(
                            SharedFunctions.ReadDocument("CreatePalletDetail", "GroupingCity")
                            );

                        template.Replace("{Scheme}", _filter.Warehouse);
                        template.Replace("{Order_Id}", o.orderkey);
                        template.Replace("{Drop_Id}", dropid);
                        template.Replace("{ExternOderkey}", o.externorderkey);
                        template.Replace("{Id_PickDetail}", lpn);
                        //template.Replace("{Sucursal}", pallet.Sucursal);
                        template.Replace("{ReferenceDocument}", o.referencedocument);
                        template.Replace("{User}", username);

                        data = connection.GetCustomSelectQuery(template.ToString());
                    }
                }
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
                            SharedFunctions.ReadDocument("ClosePallet", "GroupingCity")
                            );

                        template.Replace("{Scheme}", _filter.Warehouse);
                        template.Replace("{Drop_Id}", pallet.DropId);

                        data = connection.GetCustomSelectQuery(template.ToString());
                    }

                    //PrintPackingList(pallet.DropId, username, printerPack);
                    //PrintRotulo(pallet.DropId, username, printerPack);
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

        #endregion


        public override Response GetPacking(string dropid)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetPacking", "GroupingCity")
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

        public override Response PackageQty(string dropid)
        {
            throw new NotImplementedException();
        }

        public override Response PalletByUser(string username)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetPalletByUser", "GroupingCity")
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
                    PalletDetail pallet = new()
                    {
                        DropId = r["DROPID"]?.ToString(),
                        Group = r["GRUPO"]?.ToString(),
                        AddWho = r["USERNAME"].ToString(),
                        Transport = string.IsNullOrEmpty(r["TRANSPORT"].ToString()) ? "0" : r["TRANSPORT"].ToString(),
                        Package = r["QTY"]?.ToString()
                    };

                    pallets.Add(pallet);
                }

                return new Response { IsSuccess = true, Result = JsonSerializer.Serialize(pallets) };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override Response PalletToAudit()
        {
            DataTable data = null;

            using (ConnectionDB connection = new(_filter.ConnectionString))
            {
                StringBuilder template = new(
                    SharedFunctions.ReadDocument("GetPalletToAudit", "Grouping")
                    );

                template.Replace("{Scheme}", _filter.Warehouse);

                data = connection.GetCustomSelectQuery(template.ToString());
            }

            if (data.Rows.Count <= 0)
                throw new Exception("No existen pallets por auditar.");

            List<PalletDetail> pallets = new();

            foreach (DataRow r in data.Rows)
            {
                PalletDetail pallet = new() { DropId = r["DROPID"]?.ToString(), Group = r["GRUPO"]?.ToString(), Transport = r["TRANSPORT"].ToString() };
                pallets.Add(pallet);
            }

            return new Response { IsSuccess = true, Result = JsonSerializer.Serialize(pallets) };
        }

        public override Response AuditPallet(RequestAudit requestAudit)
        {
            bool isSuccess = ValidatePalletAudit(requestAudit.DropId, requestAudit.Qty);

            if (!isSuccess)
                throw new Exception("La cantidad auditada no es la correcta.");

            using (ConnectionDB connection = new(_filter.ConnectionString))
            {
                StringBuilder template = new(
                    SharedFunctions.ReadDocument("AuditPallet", "Grouping")
                    );

                template.Replace("{Scheme}", _filter.Warehouse);
                template.Replace("{DropId}", requestAudit.DropId);

                connection.GetCustomSelectQuery(template.ToString());
            }

            return new Response() { IsSuccess = true, Message = "Pallet auditado correctamente." };
        }

        public bool ValidatePalletAudit(string dropId, int qty)
        {
            DataTable data = null;

            using (ConnectionDB connection = new(_filter.ConnectionString))
            {
                StringBuilder template = new(
                    SharedFunctions.ReadDocument("GetPackageQty", "Grouping")
                    );

                template.Replace("{Scheme}", _filter.Warehouse);
                template.Replace("{Drop_Id}", dropId);

                data = connection.GetCustomSelectQuery(template.ToString());
            }

            if (data.Rows.Count != qty)
                return false;

            return true;
        }

        public override Response RemovePackage(RequestGrouping requestGrouping)
        {
            try
            {
                string lpn = requestGrouping.QrInfo;

                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("RemovePackage", "GroupingCity")
                        );

                    template.Replace("{Scheme}", _filter.Warehouse);
                    template.Replace("{Lpn}", lpn);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                string deleted = data.Rows[0]["ELIMINADO"].ToString();

                if (Convert.ToInt16(deleted) == 0)
                    throw new Exception("Error al eliminar el pedido.");

                return new Response { IsSuccess = true, Message = $"Bulto: {lpn}" };
            }
            catch
            {
                throw;
            }
        }

        #region Validations

        private List<dynamic> OrderExist(string lpn)
        {
            DataTable data = null;

            using (ConnectionDB connection = new(_filter.ConnectionString))
            {
                StringBuilder template = new(
                    SharedFunctions.ReadDocument("OrderExist", "GroupingCity")
                    );

                template.Replace("{Scheme}", _filter.Warehouse);
                template.Replace("{Lpn}", lpn);
                template.Replace("{Storerkey}", _filter.Storerkey);

                data = connection.GetCustomSelectQuery(template.ToString());
            }

            if (data.Rows.Count <= 0)
                throw new Exception("Error al obtener el pedido");

            List<dynamic> orders = new();

            foreach (DataRow r in data.Rows)
            {
                var orderObj = new
                {
                    order = r["ORDER"].ToString(),
                    transportDescription = r.IsNull("SUSR4") ? null : r["SUSR4"].ToString(),
                    dropid = lpn,
                    externorderkey = r["EXTERNORDERKEY"].ToString(),
                    orderkey = r["ORDERKEY"].ToString(),
                    sucursal = r["C_ADDRESS4"].ToString(),
                    type = r["TYPE"].ToString()
                };

                orders.Add(orderObj);
            }

            return orders;
        }

        public void PalletValidation(dynamic orderId)
        {
            DataTable data = null;
            string dropId = null;

            string orders = "";
            int count = orderId.Count;
            for (int i = 0; i < count; i++)
            {
                orders += $"'{orderId[i].orderkey}'";
                if (i < count - 1)
                {
                    orders += ",";
                }
            }

            using (ConnectionDB connection = new(_filter.ConnectionString))
            {
                StringBuilder template = new(
                    SharedFunctions.ReadDocument("PalletExist", "GroupingCity")
                    );

                template.Replace("{Scheme}", _filter.Warehouse);
                template.Replace("{Order_Id}", orders);

                data = connection.GetCustomSelectQuery(template.ToString());
            }

            List<PalletDetail> pallets = new();

            foreach (DataRow r in data.Rows)
            {
                PalletDetail pallet = new()
                {
                    DropId = Convert.ToString(r["DROPID"]),
                    AddWho = Convert.ToString(r["USUARIO"]),
                    OrderId = Convert.ToString(r["NUMEROPEDIDO"]),
                    IsClosed = Convert.ToString(r["ISCLOSED"]),
                    OrderStatus = Convert.ToString(r["STATUS"]), //Estado del pedido en la tabla ORDERS
                    Group = Convert.ToString(r["ORDERGROUP"]), //Campo ORDERGROUP de la tabla ORDERS
                    DropId_PickDetail = Convert.ToString(r["LPN"])
                };

                pallets.Add(pallet);
            }

            var found = pallets.Find(x => x.DropId.ToUpper() != null);
            dropId = found?.DropId ?? ""; //Vacio si el paquete no esta en pallet si no seteo el pallet

            var expedida = pallets.FirstOrDefault(x => x.OrderStatus == "95");
            if (expedida != null)
                throw new Exception($"El pedido {expedida.OrderId} ya fue expedido en el {expedida.Group}.");


            var statusOk = pallets.FirstOrDefault(x => Convert.ToInt16(x.OrderStatus) < 55);
            if (statusOk != null)
                throw new Exception($"El pedido {statusOk.OrderId} esta en un estado incorrecto.");


        }

        public void ValidatePalletDetail(string dropId, string lpn)
        {

            Pallet pallet = PalletInfo(dropId);
            DataTable data = null;

            using (ConnectionDB connection = new(_filter.ConnectionString))
            {
                StringBuilder template = new(
                    SharedFunctions.ReadDocument("GetOrderInfo", "GroupingCity")
                    );

                template.Replace("{Scheme}", _filter.Warehouse);
                template.Replace("{Lpn}", lpn);

                data = connection.GetCustomSelectQuery(template.ToString());
            }

            if (data.Rows.Count <= 0)
                throw new Exception("Error al validar.");

            foreach (DataRow r in data.Rows)
            {
                var orderObj = new
                {
                    orderkey = r["ORDERKEY"].ToString(),
                    group = r["GROUP"].ToString(),
                    type = r["TYPE"].ToString()
                };
                if (r["GROUP"].ToString().ToUpper() != pallet.Group.ToUpper())
                    throw new Exception($"La orden {orderObj.orderkey} corresponde al grupo {orderObj.group}");

                if (r["TYPE"].ToString().ToUpper() != pallet.Transport.ToUpper())
                    throw new Exception($"La orden {orderObj.orderkey} corresponde al transporte {orderObj.type}");
            }
        }

        private Pallet PalletInfo(string dropId)
        {
            DataTable data = null;

            using (ConnectionDB connection = new(_filter.ConnectionString))
            {
                StringBuilder template = new(
                    SharedFunctions.ReadDocument("GetPalletInfo", "GroupingCity")
                    );

                template.Replace("{Scheme}", _filter.Warehouse);
                template.Replace("{DropId}", dropId);

                data = connection.GetCustomSelectQuery(template.ToString());
            }

            if (data.Rows.Count <= 0)
                throw new Exception("Erro al obtener el pallet.");

            Pallet pallet = new() { Group = data.Rows[0]["GROUP"]?.ToString(), Transport = data.Rows[0]["TRANSPORT"].ToString() };
            return pallet;
        }

        public override List<PalletDetail> ValidateClosePallet(string dropId)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new(_filter.ConnectionString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("ValidateClosePallet", "GroupingCity")
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

        public override Response ValidateGrouping(RequestGrouping requestGrouping)
        {
            throw new NotImplementedException();
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
        #endregion

        #region Labels

        public override void PrintPackingList(string dropid, string username, string printer)
        {
            throw new NotImplementedException();
        }

        public override void PrintRotulo(string dropid, string username, string printer)
        {
            throw new NotImplementedException();
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

                if (string.IsNullOrEmpty(transport) && string.IsNullOrEmpty(group))
                {
                    Pallet pallet = GetGroupAndTransport(dropId);
                    group = pallet.Group;
                    transport = pallet.Transport;
                }

                label = FillLabelData(label, dropId, transport, group, orders.Count.ToString());

                StringBuilder sb = new();
                sb.Append(label);
                ZebraPrinter.ZebraPrinter.SendToPrint(sb, printer);

            }
            catch (Exception)
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
                        SharedFunctions.ReadDocument("GetPalletDetail", "GroupingCity")
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
                        SharedFunctions.ReadDocument("GetGroupAndTransport", "GroupingCity")
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

        public override string PalletValidation(string orderId, string packageId, string username, string isEvent)
        {
            throw new NotImplementedException();
        }

        public override bool ValidatePalletDetail(string lbl)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
