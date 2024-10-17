using LogGrouper.Models.Request;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace LogGrouper.Runtime.Common
{
    public class ApiHelper : IDisposable
    {
        public void Dispose() { GC.SuppressFinalize(this); }

        public dynamic fetch(string url, string json, string method, Dictionary<string, string> headers, string bearer, int timeOut, Type objectType)
        {
            object response = null;
            var responseService = "";

            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = method;
                httpWebRequest.Timeout = 1000 * timeOut;

                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> item in headers)
                    {
                        if (item.Value == null)
                            throw new Exception($"el header {item.Key} se encuentra vacio");
                        httpWebRequest.Headers[item.Key] = item.Value;
                    }
                }

                if (!string.IsNullOrEmpty(bearer))
                {
                    httpWebRequest.Headers["Authorization"] = $"Bearer {bearer}";
                }

                if (json != null)
                {
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(json);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                var streamReader = new StreamReader(httpResponse.GetResponseStream());
                responseService = streamReader.ToString();

                var streamResponse = streamReader.ReadToEnd();

                if (objectType != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    };

                    response = JsonSerializer.Deserialize(streamResponse, objectType, options);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al llamar a la api {url} " + ex.Message);
            }

            return response;
        }

        public dynamic PrintPacking(string url, string json)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "http://app-web-01:5012/api/ApiDPS/Imprimir");
                request.Headers.Add("accept", "*/*");
                var content = new StringContent(json, null, "application/json");
                request.Content = content;
                var response = client.Send(request);
                response.EnsureSuccessStatusCode();
                //Console.WriteLine(response.Content.ReadAsStringAsync());
                return response.Content.ReadAsStringAsync();
            }
            catch(Exception ex)
            {
                throw new Exception("Error al imprimir el packing: " + ex.Message);
            }
        }

    }
}
