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
    public class Datos
    {
        private string _connectionString = "";

        public void SetConnectionString(string connectionString) => this._connectionString = connectionString;

        public DataTable GetDataTable(string spName, SqlParameter[] parameters, CommandType cType)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection sqlConnection = new SqlConnection(this._connectionString))
            {
                using (SqlCommand selectCommand = new SqlCommand())
                {
                    selectCommand.Connection = sqlConnection;
                    selectCommand.CommandText = spName;
                    selectCommand.CommandType = cType;
                    selectCommand.CommandTimeout = 300;
                    if (parameters != null)
                        selectCommand.Parameters.AddRange(parameters);
                    using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand))
                    {
                        sqlConnection.Open();
                        sqlDataAdapter.Fill(dataTable);
                        sqlConnection.Close();
                    }
                    selectCommand.Parameters.Clear();
                }
            }
            return dataTable;
        }

        public void Execute(string spName, SqlParameter[] parameters, CommandType cType) => this.GetDataTable(spName, parameters, cType);

        public void GrabarEtiquetas(List<cEtiqueta> Etiquetas)
        {
            using (SqlConnection sqlConnection = new SqlConnection(this._connectionString))
            {
                sqlConnection.Open();
                SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
                SqlCommand sqlCommand = new SqlCommand();
                sqlCommand.Transaction = sqlTransaction;
                sqlCommand.Connection = sqlConnection;
                sqlCommand.CommandText = "sp_GENERAL_INSERT_Grabar_Etiqueta";
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.CommandTimeout = 300;
                try
                {
                    foreach (cEtiqueta etiqueta in Etiquetas)
                    {
                        SqlParameter[] values = new SqlParameter[3]
                        {
              new SqlParameter("@ETIQUETA", (object) etiqueta.Etiqueta),
              new SqlParameter("@USUARIO", (object) etiqueta.Usuario),
              new SqlParameter("@IMPRESORA", (object) etiqueta.Impresora)
                        };
                        sqlCommand.Parameters.AddRange(values);
                        sqlCommand.ExecuteNonQuery();
                        sqlCommand.Parameters.Clear();
                    }
                    sqlTransaction.Commit();
                }
                catch (Exception ex)
                {
                    sqlTransaction.Rollback();
                    sqlConnection.Close();
                    throw ex;
                }
                sqlConnection.Close();
                sqlCommand.Parameters.Clear();
            }
        }

        public void GrabarEtiquetasTest(List<cEtiqueta> Etiquetas)
        {
            using (SqlConnection sqlConnection = new SqlConnection(this._connectionString))
            {
                sqlConnection.Open();
                SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
                SqlCommand sqlCommand = new SqlCommand();
                sqlCommand.Transaction = sqlTransaction;
                sqlCommand.Connection = sqlConnection;
                sqlCommand.CommandText = "sp_GENERAL_INSERT_Grabar_Etiqueta_Test";
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.CommandTimeout = 300;
                try
                {
                    foreach (cEtiqueta etiqueta in Etiquetas)
                    {
                        SqlParameter[] values = new SqlParameter[3]
                        {
              new SqlParameter("@ETIQUETA", (object) etiqueta.Etiqueta),
              new SqlParameter("@USUARIO", (object) etiqueta.Usuario),
              new SqlParameter("@IMPRESORA", (object) etiqueta.Impresora)
                        };
                        sqlCommand.Parameters.AddRange(values);
                        sqlCommand.ExecuteNonQuery();
                        sqlCommand.Parameters.Clear();
                    }
                    sqlTransaction.Commit();
                }
                catch (Exception ex)
                {
                    sqlTransaction.Rollback();
                    sqlConnection.Close();
                    throw ex;
                }
                sqlConnection.Close();
                sqlCommand.Parameters.Clear();
            }
        }
    }
}
