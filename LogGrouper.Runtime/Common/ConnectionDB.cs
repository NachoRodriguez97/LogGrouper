using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace LogGrouper.Runtime.Common
{
    public class ConnectionDB : IDisposable
    {
        public void Dispose() { GC.SuppressFinalize(this); }

        private static string connectionStrings = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionDB"/> class.
        /// Logic.
        /// </summary>
        public ConnectionDB()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionDB"/> class.
        /// Logic.
        /// </summary>
        /// <param name="connectionString">connectionString.</param>
        public ConnectionDB(string connectionString)
        {
            connectionStrings = connectionString;
        }

        /// <summary>
        /// GetDataTable.
        /// </summary>
        /// <param name="connectionString">connectionString.</param>
        public void SetConnectionString(string connectionString)
        {
            ConnectionDB.connectionStrings = connectionString;
        }

        public DataTable GetCustomSelectQuery(string query)
        {
            try
            {
                DataTable dt = new DataTable();
                string connString = connectionStrings;

                using (SqlConnection con = new SqlConnection(connString))
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Connection = con;
                        cmd.CommandText = query;
                        cmd.CommandTimeout = 300;

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {

                            con.Open();
                            da.Fill(dt);
                            con.Close();
                        }
                        cmd.Parameters.Clear();
                    }
                }
                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public DataTable GetDataTable(string spname, SqlParameter[] parameters, CommandType cType)
        {
            DataTable dt = new DataTable();
            string connString = connectionStrings;

            using (SqlConnection con = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = con;
                    cmd.CommandText = spname;
                    cmd.CommandTimeout = 300;

                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        con.Open();

                        da.Fill(dt);

                        con.Close();
                    }

                    cmd.Parameters.Clear();
                }
            }

            return dt;
        }

        public DataTable GetDataTable(string spname, CommandType cType)
        {
            DataTable dt = new DataTable();
            string connString = connectionStrings;

            using (SqlConnection con = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = con;
                    cmd.CommandText = spname;
                    cmd.CommandTimeout = 300;

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        con.Open();

                        da.Fill(dt);

                        con.Close();
                    }

                    cmd.Parameters.Clear();
                }
            }

            return dt;
        }


        public List<T> GetList<T>(string spname, CommandType cType)
        {
            var dt = GetDataTable(spname, cType);

            return TableToList<T>(dt);
        }

        public List<T> TableToList<T>(DataTable table)
        {
            List<T> res = new List<T>();
            foreach (DataRow rw in table.Rows)
            {
                T item = Activator.CreateInstance<T>();
                foreach (DataColumn cl in table.Columns)
                {
                    PropertyInfo pi = typeof(T).GetProperty(cl.ColumnName);

                    if (pi != null && rw[cl] != DBNull.Value)
                        pi.SetValue(item, ChangeType(rw[cl], pi.PropertyType), new object[0]);
                }
                res.Add(item);
            }
            return res;
        }

        public static object ChangeType(object value, Type conversion)
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


        public DataTable GetCustomABMQuery(string query)
        {
            string connString = connectionStrings;
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = con;
                    cmd.CommandText = query;
                    cmd.CommandTimeout = 300;

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {

                        con.Open();
                        //cmd.ExecuteNonQuery();
                        da.Fill(dt);
                        con.Close();
                    }
                    cmd.Parameters.Clear();
                }
            }
            return dt;
        }
    }
}
