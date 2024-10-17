using LogGrouper.Models.Business;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogGrouper.Runtime.ZebraPrinter
{
    public class Grabacion
    {
        public static void GrabarEtiqueta(string connectionString, cEtiqueta Etiqueta)
        {
            SqlParameter[] parameters = new SqlParameter[3]
            {
        new SqlParameter("@ETIQUETA", (object) Etiqueta.Etiqueta),
        new SqlParameter("@USUARIO", (object) Etiqueta.Usuario),
        new SqlParameter("@IMPRESORA", (object) Etiqueta.Impresora)
            };
            Datos datos = new Datos();
            datos.SetConnectionString(connectionString);
            datos.Execute("sp_GENERAL_INSERT_Grabar_Etiqueta", parameters, CommandType.StoredProcedure);
        }

        public static void GrabarEtiquetaTest(string connectionString, cEtiqueta Etiqueta)
        {
            SqlParameter[] parameters = new SqlParameter[3]
            {
        new SqlParameter("@ETIQUETA", (object) Etiqueta.Etiqueta),
        new SqlParameter("@USUARIO", (object) Etiqueta.Usuario),
        new SqlParameter("@IMPRESORA", (object) Etiqueta.Impresora)
            };
            Datos datos = new Datos();
            datos.SetConnectionString(connectionString);
            datos.Execute("sp_GENERAL_INSERT_Grabar_Etiqueta_Test", parameters, CommandType.StoredProcedure);
        }

        public static void GrabarMultiplesEtiquetas(string connectionString, List<cEtiqueta> Etiquetas)
        {
            Datos datos = new Datos();
            datos.SetConnectionString(connectionString);
            datos.GrabarEtiquetas(Etiquetas);
        }

        public static void GrabarMultiplesEtiquetasTest(
          string connectionString,
          List<cEtiqueta> Etiquetas)
        {
            Datos datos = new Datos();
            datos.SetConnectionString(connectionString);
            datos.GrabarEtiquetasTest(Etiquetas);
        }
    }
}
