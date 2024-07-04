using CryptMail.Models;
using CryptMail.MSSQLServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Net;

namespace CryptMail.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public PartialViewResult getSignUpForm()
        {
            return PartialView("SignUpPartial");
        }

        [HttpGet]
        public PartialViewResult getSignInForm()
        {
            return PartialView("SignInPartial");
        }

        #region COOKIE
        public IActionResult createCookie(string id)
        {
            Response.Cookies.Append("cryptMailUser", id,
                new CookieOptions 
                { 
                    Secure = true,
                    Expires = DateTime.Now.AddMonths(1),
                }
                );
            return RedirectToAction("checkCookie", "Auth");
        }

        public async Task<IActionResult> checkCookie()
        {
            string? myCookie = Request.Cookies["cryptMailUser"];

            if(myCookie == null)
                return RedirectToAction("Wellcome", "Home");

            UserModel? userModel = new UserModel("", "");
            await DBHelper.ExecuteWithConnectionAsync(connection =>
            {
                SqlCommand command = new SqlCommand($"select id, login from Users where id=@UserId", connection);

                if (!Guid.TryParse(myCookie, out Guid userId))
                {
                    userModel = null;
                    return;
                }

                command.Parameters.AddWithValue("UserId", userId);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet dSet = new DataSet();
                adapter.Fill(dSet);

                if (dSet.Tables[0].Rows.Count == 0)
                    userModel = null;
                else 
                { 
                    userModel = new UserModel(
                        dSet.Tables[0].Rows[0][0].ToString(),
                        dSet.Tables[0].Rows[0][1].ToString()
                        );

                    string userModelJson = JsonSerializer.Serialize(userModel);
                    byte[] userModelBytes = Encoding.UTF8.GetBytes(userModelJson);

                    HttpContext.Session.Set("UserModel", userModelBytes);
                }
            }, this);

            if (userModel != null) return RedirectToAction("Index", "Mail");

            return RedirectToAction("Wellcome", "Home");
        }

        public IActionResult deleteUserCookie()
        {
            Response.Cookies.Append("cryptMailUser", "", new CookieOptions { Expires = DateTime.Now.AddDays(-1) });
            return RedirectToAction("Index");
        }
        #endregion



        [HttpPost]
        public async Task<IActionResult> SignIn(string login, string password)
        {
            bool userLogged = false;
            string? userId = "";
            string? userLogin = "";

            await DBHelper.ExecuteWithConnectionAsync(connection =>
            {
                SqlCommand command = new SqlCommand($"select id, login, password from Users where login='{login}' and password='{password}'", connection);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet dSet = new DataSet();
                adapter.Fill(dSet);

                if (dSet.Tables[0].Rows.Count > 0) 
                {
                    userLogged = true;
                    userId = dSet.Tables[0].Rows[0][0].ToString();
                    userLogin = dSet.Tables[0].Rows[0][1].ToString();
                }
            }, this);

            if (userLogged)
            {
                return RedirectToAction("createCookie", "Auth", new { id = userId});
            }
            
            TempData["AuthResult"] = "User not found";
            return RedirectToAction("Index", "Auth");
        }

        [HttpPost]
        public async Task<PartialViewResult> CreateUser(string username, string pass1, string pass2)
        {
            if (pass1 != pass2)
            {
                TempData["SignUpErr"] = "Passwords are not equal!";

                return getSignUpForm();
                //return RedirectToAction("Index", "Auth");
            }


            bool UserExistFlag = false;
            await DBHelper.ExecuteWithConnectionAsync(connection =>
            {

                SqlCommand command = new SqlCommand($"select login from Users where login='{username}'", connection);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet);

                if (dataSet.Tables[0].Rows.Count > 0)
                {
                    UserExistFlag = true;
                }
            }, this);

            if (UserExistFlag)
            {
                TempData["SignUpErr"] = "Login already exist!";
                return getSignUpForm();
                //return RedirectToAction("Index", "Auth");
            }

            await DBHelper.ExecuteWithConnectionAsync(connection =>
            {
                SqlCommand command = new SqlCommand("INSERT INTO Users (login, password) VALUES (@Username, @Password)", connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", pass1);

                command.ExecuteNonQuery();
            }, this);

            return getSignInForm();
            //return RedirectToAction("Index", "Auth");
        }

    }
}