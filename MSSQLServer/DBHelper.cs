

using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace CryptMail.MSSQLServer
{
    public static class DBHelper
    {
        public static string connectionStr = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""D:\zDocs\C_Sharp\ASP NET\CryptMail\MSSQLServer\CryptMailLocalDb.mdf"";Integrated Security=True;Connect Timeout=30";

        public static async Task ExecuteWithConnectionAsync(Action<SqlConnection> action, ControllerBase controller)
        {
            using (SqlConnection connection = new SqlConnection(DBHelper.connectionStr))
            {
                try
                {
                    await connection.OpenAsync();
                    await Task.Run(() => action.Invoke(connection));
                }
                catch (Exception ex)
                {
                    controller.Response.StatusCode = 500;
                    controller.Response.ContentType = "text/plain";
                    await controller.Response.WriteAsync(ex.Message);
                    return;
                }

                if (connection.State == System.Data.ConnectionState.Open)
                    await connection.CloseAsync();
            }
        }

        public static async Task<string?> getLoginFromGuid(Guid guid, ControllerBase controller)
        {
            string? login = null;
            await ExecuteWithConnectionAsync(connection =>
            {
                SqlCommand command = new SqlCommand("select login from Users where Id=@Id", connection);
                command.Parameters.AddWithValue("Id", guid);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet dSet = new DataSet();
                adapter.Fill(dSet);

                if (dSet != null && dSet.Tables[0].Rows.Count > 0)
                    login = dSet.Tables[0].Rows[0][0].ToString();

            }, controller);

            return login;
        }

        public static async Task<Guid?> getGuidFromLogin(string login, ControllerBase controller)
        {
            Guid? guid = null;
            await ExecuteWithConnectionAsync(connection =>
            {
                SqlCommand command = new SqlCommand("select Id from Users where login=@login", connection);
                command.Parameters.AddWithValue("login", login);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet dSet = new DataSet();
                adapter.Fill(dSet);

                if (dSet != null && dSet.Tables[0].Rows.Count > 0)
                    guid = Guid.Parse(dSet.Tables[0].Rows[0][0].ToString());

            }, controller);

            return guid;
        }
    }
}
