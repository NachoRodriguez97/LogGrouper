using LogGrouper.Models.Business;
using LogGrouper.Models.Global;
using LogGrouper.Runtime.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Runtime.ZebraPrinter
{
    public class ZebraPrinter
    {
        public static void SendToPrint(StringBuilder sb, string printer, int buffersize = 5)
        {
            string jobid = Guid.NewGuid().ToString().Split('-').First();

            int buffer = buffersize;

            string convertedString = ReplaceDiacritics(sb.ToString());
            string filtredString = EraseInvalidValidCharacters(convertedString);

            string content = filtredString.Replace("^XZ", "ª");


            string[] traces = content.Split('ª');

            List<cEtiqueta> listaEtiquetas = new List<cEtiqueta>();

            foreach (string trace in traces)
            {
                if (!string.IsNullOrEmpty(trace))
                {
                    cEtiqueta etiqueta = new cEtiqueta();
                    etiqueta.Etiqueta = trace + "^XZ";
                    etiqueta.Impresora = printer;
                    etiqueta.Usuario = "Farmaecom Agrupador";

                    listaEtiquetas.Add(etiqueta);
                }
            }



            Grabacion.GrabarMultiplesEtiquetas(AppSettings.ZebraPrinter, listaEtiquetas);

        }


        public static string ReplaceDiacritics(string source)
        {
            //source = source.Replace("º", "");
            //source = source.Replace("°", "");
            ////source = source.Replace(".", "");
            //source = source.Replace("¥", "Ñ");
            //source = source.Replace("ñ", "n");//  149954 §
            //source = source.Replace("§", "");
            //source = source.Replace("Ñ", "N");
            //


            string sourceInFormD = source.Normalize(NormalizationForm.FormD);

            var output = new StringBuilder();
            foreach (char c in sourceInFormD)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    output.Append(c);
            }

            return (output.ToString().Normalize(NormalizationForm.FormC));
        }


        private static string EraseInvalidValidCharacters(string source)
        {
            StringBuilder sb = new StringBuilder();

            string allowableLetters = @" ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789^.,;:-)(][|/>\_{";
            //string allowableLetters = ConfigurationManager.AppSettings["CHARSET"];

            foreach (char c in source)
            {
                string character = c.ToString();
                // This is using String.Contains for .NET 2 compat.,
                //   hence the requirement for ToString()
                if (allowableLetters.Contains(character))
                    sb.Append(character); // return false;
            }

            return sb.ToString();
        }
    }
}
