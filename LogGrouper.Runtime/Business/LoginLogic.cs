using LogGrouper.Models.Business;
using LogGrouper.Models.Global;
using LogGrouper.Models.Request;
using LogGrouper.Models.Response;
using LogGrouper.Runtime.Common;
using Loginter.Common.Tools.Cryptography;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LogGrouper.Runtime.Business
{
    public class LoginLogic : IDisposable
    {
        public LoginLogic() { Encrypter.SetOptionChain(ChainOption.ChainOptionOne); }
        public void Dispose() { GC.SuppressFinalize(this); }

        public string InforAuthenticate(ReqAuthenticate req)
        {
            try
            {

                DataTable data = null;

                using (ConnectionDB connection = new ConnectionDB(AppSettings.MainConnString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetClientDataByName", "Client")
                        );

                    template.Replace("{Client}", req.Client);
                    template.Replace("{Scheme}", AppSettings.Schema);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("Error al obtener la información del cliente seleccionado.");

                Client client = new()
                {
                    Id = Int32.Parse(data.Rows[0]["Id"].ToString()),
                    Name = data.Rows[0]["Named"].ToString(),
                    ConnString = Encrypter.Decrypt(data.Rows[0]["ConnectionString"]?.ToString()),
                    Warehouse = data.Rows[0]["Warehouse"]?.ToString(),
                    StorerName = data.Rows[0]["StorerName"]?.ToString(),
                    LoginUrl = Encrypter.Decrypt(data.Rows[0]["LoginUrl"]?.ToString()),
                    LabelPrefix = data.Rows[0]["LabelPrefix"]?.ToString(),
                    GetInfoQuery = data.Rows[0]["GetInfoQuery"]?.ToString(),
                    ApiLoginPassword = Encrypter.Decrypt(data.Rows[0]["LoginPassword"]?.ToString()),
                    ApiLoginUsername = Encrypter.Decrypt(data.Rows[0]["LoginUsername"]?.ToString())
                };

                using (ApiHelper api = new ApiHelper())
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    headers.Add("Tenant", "INFOR");
                    headers.Add("Username", req.Username);
                    headers.Add("Password", req.Password);

                    var response = api.fetch(
                       client.LoginUrl,
                       null,
                       "GET",
                       headers,
                       null,
                       275,
                       null
                       );
                }

                return Encrypter.Encrypt(JsonSerializer.Serialize(new User() { Username = req.Username, Password = req.Password, Client = client }));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public string GetClients()
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new ConnectionDB(AppSettings.MainConnString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetClients", "Client")
                        );

                    template.Replace("{Scheme}", AppSettings.Schema);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("Error al obtener clientes.");

                List<Client> clients = new();
                foreach (DataRow c in data.Rows)
                {
                    Client client = new() { Name = c["Named"].ToString(), Id = Convert.ToInt32(c["Id"]) };
                    clients.Add(client);
                }

                return JsonSerializer.Serialize(clients);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public User GetLoggedUser(string username, string client)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new ConnectionDB(AppSettings.MainConnString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetLoggedUser", "Client")
                        );

                    template.Replace("{Username}", username);
                    template.Replace("{Client}", client);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    return null;

                User user = new() { Username = data.Rows[0]["Username"].ToString(), IsLogged = Convert.ToBoolean(data.Rows[0]["IsLogged"]) };

                return user;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Response ValidateLoggedUser(string username, string password, string client)
        {
            try
            {
                User loggedUser = GetLoggedUser(username, client);

                if (loggedUser == null || !loggedUser.IsLogged)
                {
                    if (loggedUser == null)
                    {
                        InsertLoggedUser(username, client);
                    }
                    else
                    {
                        UpdateLoggedUser(username, client, 1);
                    }

                    return new Response { IsSuccess = true };
                }
                else
                {
                    if (password.Equals("1-QR12345678"))
                    {
                        UpdateLoggedUser(username, client, 0);
                        return new Response { IsSuccess = false, Message = "Cuenta Desbloqueada" };
                    }

                    return new Response { IsSuccess = false, Message = "Este usuario ya tiene una sesion iniciada." };
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Response Logout(string username, string client)
        {
            try
            {
                UpdateLoggedUser(username, client, 0);
                return new Response { IsSuccess = true };
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void UpdateLoggedUser(string username, string client, int isLogged)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new ConnectionDB(AppSettings.MainConnString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("UpdateLoggedUser", "Client")
                        );

                    template.Replace("{Username}", username);
                    template.Replace("{Client}", client);
                    template.Replace("{IsLogged}", isLogged.ToString());

                    data = connection.GetCustomSelectQuery(template.ToString());
                }
            }
            catch
            {
                throw;
            }
        }

        private void InsertLoggedUser(string username, string client)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new ConnectionDB(AppSettings.MainConnString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("InsertLoggedUser", "Client")
                        );

                    template.Replace("{Username}", username);
                    template.Replace("{Client}", client);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }
            }
            catch
            {
                throw;
            }
        }

        public List<ClientSetting> GetClientSettings(string client)
        {
            try
            {
                DataTable data = null;

                using (ConnectionDB connection = new ConnectionDB(AppSettings.MainConnString))
                {
                    StringBuilder template = new(
                        SharedFunctions.ReadDocument("GetClientSettings", "Client")
                        );

                    template.Replace("{Client}", client);

                    data = connection.GetCustomSelectQuery(template.ToString());
                }

                if (data.Rows.Count <= 0)
                    throw new Exception("Error al cargar las configuraciones del cliente.");

                List<ClientSetting> settings = new();
                foreach (DataRow s in data.Rows)
                {
                    ClientSetting setting = new() {
                        Module = s["Module"].ToString(),
                        Functionality = s["Functionality"].ToString(),
                        Name = s["Name"].ToString(),
                        IsActive = Convert.ToBoolean(Convert.ToInt16(s["IsActive"]))
                    };

                    settings.Add(setting);
                }

                return settings;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
