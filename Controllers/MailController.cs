using CryptMail.Models;
using CryptMail.MSSQLServer;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace CryptMail.Controllers
{
    public partial class MailController : Controller
    {
        #region DATA WRAPPER
        public async Task<IActionResult> RunWithCookie(Func<string, Task<IActionResult>> action)
        {
            string key = "cryptMailUser";
            string? cookie = Request.Cookies[key];

            if (cookie == null)
            {
                return RedirectToAction("Index", "Auth");
            }

            bool userExist = await checkUserInDataBaseAsync(cookie);
            if (!userExist)
            {
                return RedirectToAction("Index", "Auth");
            }

            try
            {
                return await action(cookie);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public async Task<bool> checkUserInDataBaseAsync(string userId)
        {
            bool userExist = false;
            await DBHelper.ExecuteWithConnectionAsync(connection =>
            {
                if (!Guid.TryParse(userId, out Guid userIdGuid)) return;
                
                SqlCommand command = new SqlCommand($"select Id from Users where Id=@userId", connection);
                command.Parameters.AddWithValue("userId", userIdGuid);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet dSet = new DataSet();
                adapter.Fill(dSet);

                if (dSet.Tables[0].Rows.Count > 0) userExist = true;
            }, this);

            return userExist;
        }
        #endregion


        #region CONTROLLER METHODS

        public IActionResult LogOut()
        {   //  вийти - очищення кукі
            return RedirectToAction("deleteUserCookie", "Auth");
        }


        public async Task<IActionResult> OpenChat(string login)
        {
            return await RunWithCookie(async (cookie) =>
            {
                ChatModel userChat = new ChatModel();

                userChat.newMessage.PartnerLogin = login;
                bool err = false;

                await DBHelper.ExecuteWithConnectionAsync(connection =>
                {
                    try
                    {
                        SqlCommand command = new SqlCommand("exec GetChatMessages @userId, @partnerLogin, @pageNumber;", connection);
                        command.Parameters.AddWithValue("@userId", cookie);
                        command.Parameters.AddWithValue("@partnerLogin", login);
                        command.Parameters.AddWithValue("@pageNumber", userChat.currentPage);

                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        DataSet set = new DataSet();
                        adapter.Fill(set);

                        foreach (DataRow row in set.Tables[0].Rows)
                        {
                            userChat.messages.Add(new MessageModel
                            {
                                senderLogin = row[0].ToString(),
                                Message = row[1].ToString(),
                                messageSendTime = DateTime.Parse(row[2].ToString()),
                                Theme = row[3].ToString(),
                                isImportantForReciever = bool.Parse(row[4].ToString()),
                                isReadedByReciever = bool.Parse(row[5].ToString())
                            });
                        }
                    }
                    catch
                    {
                        err = true;
                    }

                }, this);

                //exec GetChatMessages '5e8f16ab-6a65-4b81-8c14-2bf8bf6d6244', '1', 3;

                if (err) return View("Index");
                return View("Chat", userChat);
            });
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            byte[]? userModelWithErr = HttpContext.Session.Get("MessageWithError");
            if (userModelWithErr != null)
            {
                UserMessageModel? umModel = JsonSerializer.Deserialize<UserMessageModel>
                    (
                    Encoding.UTF8.GetString(userModelWithErr)
                    );
                umModel.newMessageFormOpened = true;

                if(umModel != null ) return View(umModel);
            }

            return await RunWithCookie(async (cookie) =>
            {
                //  звернутися до сесії користувача
                byte[] userModelBytes = HttpContext.Session.Get("UserModel");
                string userModelJson = Encoding.UTF8.GetString(userModelBytes);
                UserModel user = JsonSerializer.Deserialize<UserModel>(userModelJson);

                //  зібрати об'єкт Користувач / Повідомлення
                UserMessageModel userMessage = new UserMessageModel();
                userMessage.userModel = user;
                userMessage.newMessageFormOpened = false;

                //  відобразити сторінку з об'єктом молелі
                return View(userMessage);
            });
        }






        [HttpPost]
        public async Task<PartialViewResult> sendNewMessage(NewMessageModel messageModel)
        {   //  відправка нового повідомлення

            messageModel.userToId = await DBHelper.getGuidFromLogin(messageModel.recieverLogin, this);
            if (messageModel.userToId == null)
            {   
                //  користувача не знайдено у базі

                messageModel.onFormMessageColor = "red";
                messageModel.onFormMessage = "No receiver with such login!";
                Response.StatusCode = 201;  //  щоб поля на формі не очищалися
            }
            else
            {
                //  користувач є
                //  почати відправку

                string? senderLogin = Request.Cookies["cryptMailUser"];
                senderLogin = await DBHelper.getLoginFromGuid(new Guid(senderLogin), this);

                await DBHelper.ExecuteWithConnectionAsync(connection =>
                {   //  для БД - додати повідомлення у таблицю

                    SqlCommand command = new SqlCommand("exec AddMessage @sender, @reciever, @title, @text, @time", connection);
                    command.Parameters.AddWithValue("sender", senderLogin);
                    command.Parameters.AddWithValue("reciever", messageModel.recieverLogin);
                    command.Parameters.AddWithValue("title", messageModel.Theme);
                    command.Parameters.AddWithValue("text", messageModel.Message);
                    command.Parameters.AddWithValue("time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    command.ExecuteNonQuery();
                }, this);

                //  очистити модель, та відобразити зеленим кольором
                messageModel = new NewMessageModel();
                messageModel.onFormMessageColor = "green";
                messageModel.onFormMessage = "Message sent!";
                Response.StatusCode = 200;  //  успіх відпраки
            }

            return PartialView("NewMessageFormPartial", messageModel);
        }



        [HttpPost]
        public async Task<PartialViewResult> getChats([FromBody] UserMessageModel userMessage)
        {

            await DBHelper.ExecuteWithConnectionAsync(connection =>
            {

                SqlCommand command = new SqlCommand("exec GetChatsByLogin @thisUserLogin", connection);
                command.Parameters.AddWithValue("thisUserLogin", userMessage.userModel.login);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataSet dSet = new DataSet();
                adapter.Fill(dSet);

                if (dSet.Tables[0].Rows.Count == 0)
                {
                    userMessage.isAnyChat = false;
                    return;
                }
                else userMessage.isAnyChat = true;

                foreach(DataRow item in dSet.Tables[0].Rows)
                {
                    MessageModel message = new MessageModel();

                    message.PartnerLogin = item[0].ToString();
                    message.Theme = item[1].ToString();
                    message.Message = item[2].ToString();
                    message.messageSendTime = Convert.ToDateTime(item[3]);
                    message.isImportantForReciever = Convert.ToBoolean(item[4]);
                    message.isImportantForSender = Convert.ToBoolean(item[5]);
                    message.isReadedByReciever = Convert.ToBoolean(item[7]);

                    userMessage.messagesList.Add(message);
                }

            }, this);

            Response.StatusCode = 200;
            return PartialView("ChatsListPartial", userMessage);
        }

        #endregion
    }
}
