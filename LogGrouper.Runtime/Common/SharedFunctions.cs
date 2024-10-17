using BarcodeStandard;
using NReco.PdfGenerator;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using ThoughtWorks.QRCode.Codec;


namespace LogGrouper.Runtime.Common
{
    public class SharedFunctions
    {
        public static string ReadDocument(string filename, string entidad)
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);

            //prueba local
            string filePath = Path.Combine(assemblyPath.Replace("\\bin", "").Replace("\\Debug", "").Replace("file:\\", "").Replace("\\net5.0", "").Replace("API", "Runtime") + $"\\Queries\\{entidad}\\", filename + ".txt");

            //api prod
            //string filePath = Path.Combine(assemblyPath.Replace("file:\\", "") + $"\\Queries\\{entidad}\\", filename + ".txt");

            if (!File.Exists(filePath))
            {
                //throw new Exception("La direccion de las queries debe estar en " + filePath);
                return "";
            }

            return File.ReadAllText(filePath);
        }

        public List<T> TableToList<T>(DataTable table)
        {
            List<T> res = new List<T>();
            foreach (DataRow rw in table.Rows)
            {
                T item = Activator.CreateInstance<T>();
                foreach (DataColumn cl in table.Columns)
                {
                    PropertyInfo pi = typeof(T).GetProperty(cl.ColumnName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    if (pi != null && rw[cl] != DBNull.Value)
                        pi.SetValue(item, ChangeType(rw[cl], pi.PropertyType), new object[0]);
                }
                res.Add(item);
            }
            return res;
        }

        public static object ChangeType(object value, System.Type conversion)
        {
            var t = conversion;

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return Convert.ChangeType(value, t);
        }

        public static SKImage GenerateCodeBar(string contenido)
        {
            try
            {
                Barcode barcode = new();
                return barcode.Encode(BarcodeStandard.Type.Code128, contenido, SKColors.Black, SKColors.White, 300, 100);
            }
            catch
            {
                throw;
            }
        }

        public static byte[] ConvertHtmlToPdf(string html)
        {
            try
            {
                var htmlToPdf = new HtmlToPdfConverter();
                byte[] pdf = htmlToPdf.GeneratePdf(html);
                return pdf;
            }
            catch
            {
                throw;
            }
        }
    }
}

