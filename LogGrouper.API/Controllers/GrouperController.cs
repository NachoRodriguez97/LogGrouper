using LogGrouper.Models.Business;
using LogGrouper.Models.Global;
using LogGrouper.Models.Request;
using LogGrouper.Models.Response;
using LogGrouper.Runtime.Business;
using Loginter.Common.Tools.Cryptography;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LogGrouper.API.Controllers
{
    [EnableCors("AllowOrigin")]
    [Route("[controller]")]
    [ApiController]

    public class GrouperController : Controller
    {
        [HttpPost("GetPrinters")]
        public dynamic GetPrinters([FromBody] string token)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                return new Response { IsSuccess = true, Message = iCustomer.GetPrinters(user.Client.ConnString, Encrypter.Decrypt(user.Client.Warehouse), user.Client.StorerName) };
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = "Error: " + ex.ToString() };
            }
        }

        [HttpPost("SetPrinters")]
        public dynamic SetPrinters([FromBody] RequestPrinter request)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(request.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = user.Client.Warehouse, ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                string newToken = iCustomer.SetPrinters(user, request.Printers);

                if (string.IsNullOrEmpty(newToken))
                {
                    return new Response { IsSuccess = false, Message = "Error: La impresora no pudo ser seleccionada correctamente." };
                }

                return new Response { IsSuccess = true, Token = newToken };
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.ToString() };
            }
        }

        [HttpPost("ValidateGrouping")]
        public dynamic ValidateGrouping([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                return iCustomer.ValidateGrouping(requestGrouping);

            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("CreatePallet")]
        public dynamic CreatePallet([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                return iCustomer.CreatePallet(requestGrouping, user);

            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("CreatePalletDetail")]
        public dynamic CreatePalletDetail([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                return iCustomer.CreatePalletDetail(requestGrouping, user);
            }
            catch(Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message};
            }
        }

        [HttpPost("LogByUser")]
        public dynamic LogByUser([FromBody] string token)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                return iCustomer.PalletByUser(user.Username);
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("PalletToAudit")]
        public dynamic PalletToAudit([FromBody] string token)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                return iCustomer.PalletToAudit();
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("AuditPallet")]
        public dynamic AuditPallet([FromBody] RequestAudit requestAudit)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestAudit.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                return iCustomer.AuditPallet(requestAudit);
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("ClosePallet")]
        public dynamic ClosePallet([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                Printer lblPrinterPack = user.Printers.Find(x => x.Type.ToUpper().Trim() == "PRINTPACK");
                Printer lblPrinterLabel = user.Printers.Find(x => x.Type.ToUpper().Trim() == "PRINTLBL");
                return iCustomer.ClosePallet(requestGrouping.Pallet, user.Username, lblPrinterPack.Name, lblPrinterLabel.Name);
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("GetDiference")]
        public dynamic GetDiference([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                return iCustomer.GetDiference(requestGrouping.Pallet.DropId);
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("PackageQty")]
        public dynamic PackageQty([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                return iCustomer.PackageQty(requestGrouping.Pallet.DropId);
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("GetPacking")]
        public dynamic GetPacking([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                return iCustomer.GetPacking(requestGrouping.Pallet.DropId);
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("RemovePackage")]
        public dynamic RemovePackage([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                return iCustomer.RemovePackage(requestGrouping);
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("PrintLabel")]
        public dynamic PrintLabel([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                Printer lblPrinter = user.Printers.Find(x => x.Type.ToUpper().Trim() == "PRINTLBL");
                iCustomer.PrintLabel(requestGrouping.Pallet.DropId, requestGrouping.Pallet.Transport, requestGrouping.Pallet.Group,lblPrinter.Name, user.Client.StorerName);

                return new Response { IsSuccess = true, Message = "Documento enviado a impresión." };
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("ReprintRotulo")]
        public dynamic ReprintRotulo([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                Printer lblPrinter = user.Printers.Find(x => x.Type.ToUpper().Trim() == "PRINTPACK");
                iCustomer.PrintRotulo(requestGrouping.Pallet.DropId, user.Username, lblPrinter.Name);

                return new Response { IsSuccess = true, Message = "Documento enviado a impresión." };
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("ReprintLabel")]
        public dynamic ReprintLabel([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                Printer lblPrinter = user.Printers.Find(x => x.Type.ToUpper().Trim() == "PRINTLBL");
                iCustomer.ReprintLabel(requestGrouping.Pallet.DropId, lblPrinter.Name);

                return new Response { IsSuccess = true, Message = "Documento enviado a impresión." };
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("PrintPacking")]
        public dynamic PrintPacking([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                Printer lblPrinter = user.Printers.Find(x => x.Type.ToUpper().Trim() == "PRINTPACK");
                iCustomer.PrintPackingList(requestGrouping.Pallet.DropId, user.Username, lblPrinter.Name);

                return new Response { IsSuccess = true, Message = "Documento enviado a impresión." };
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("PrintFinalLabel")]
        public dynamic PrintFinalLabel([FromBody] RequestGrouping requestGrouping)
        {
            try
            {
                string tokenDecrypt = Encrypter.Decrypt(requestGrouping.Token);
                User user = JsonSerializer.Deserialize<User>(tokenDecrypt);

                using CustomerFactory factory = new(new Filter { Storerkey = user.Client.Name, Warehouse = Encrypter.Decrypt(user.Client.Warehouse), ConnectionString = user.Client.ConnString });
                ICustomer iCustomer = factory.GetCustomer();

                if (iCustomer == null)
                {
                    return new Response { IsSuccess = false, Message = $"Error: Cliente ({user.Client.Name}) no tiene asociada ninguna configuración." };
                }

                Printer lblPrinter = user.Printers.Find(x => x.Type.ToUpper().Trim() == "PRINTLBL");
                iCustomer.PrintFinalLabel(requestGrouping.Pallet.DropId, "", "", lblPrinter.Name);

                return new Response { IsSuccess = true, Message = "Documento enviado a impresión." };
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }
    }
}
