using LogGrouper.Models.Business;
using LogGrouper.Models.Request;
using LogGrouper.Models.Response;
using LogGrouper.Runtime.Business;
using Loginter.Common.Tools.Cryptography;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LogGrouper.API.Controllers
{
    [EnableCors("AllowOrigin")]
    [Route("[controller]")]
    [ApiController]

    public class LoginController : ControllerBase
    {
        [HttpPost("Authenticate")]
        public dynamic Authenticate([FromBody] ReqAuthenticate request)
        {
            LoginResponse response = new();

            try
            {
                using LoginLogic uLogic = new();
                
                Response validateUser = uLogic.ValidateLoggedUser(request.Username, request.Password, request.Client);

                if (request.Password.Equals("1-QR12345678"))
                {
                    return validateUser;
                }

                string token = uLogic.InforAuthenticate(request);

                if (token != null && !token.Trim().Equals(""))
                {
                    if (validateUser.IsSuccess)
                    {
                        string settings = JsonConvert.SerializeObject(uLogic.GetClientSettings(request.Client), Formatting.Indented);
                        response = new() { IsSuccess = true, Message = "Usuario autentificado", Token = token, Settings = settings };
                    }
                    else
                    {
                        response = new() { IsSuccess = false, Message = validateUser.Message };
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
            catch (Exception ex)
            {
                response = new() { IsSuccess = false, Message = "Usuario y/o contraseña incorrectos.", Result = ex.ToString() };
            }

            return response;
        }

        [HttpPost("Logout")]
        public Response Logout([FromBody] ReqAuthenticate request)
        {
            try
            {
                using LoginLogic uLogic = new();

                Response response = new();

                response = uLogic.Logout(request.Username, request.Client);

                return response;
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        [HttpPost("GetClients")]
        public dynamic GetClients()
        {
            try
            {
                using LoginLogic uLogic = new();

                string customers = uLogic.GetClients();

                return new Response { IsSuccess = true, Message = customers };
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.ToString() };
            }
        }

        [HttpPost("Encrypt")]
        public dynamic Encrypt([FromBody] ReqAuthenticate text)
        {
            try
            {
                string urlEncrypt = Encrypter.Encrypt(text.Password);

                return urlEncrypt;
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.ToString() };
            }
        }

        [HttpPost("Decrypt")]
        public dynamic Decrypt([FromBody] ReqAuthenticate text)
        {
            try
            {
                string urlEncrypt = Encrypter.Decrypt(text.Password);

                return urlEncrypt;
            }
            catch (Exception ex)
            {
                return new Response { IsSuccess = false, Message = ex.ToString() };
            }
        }
    }
}
