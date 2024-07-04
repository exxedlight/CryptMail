using CryptMail.Additional;
using CryptMail.Models;
using CryptMail.MSSQLServer;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NuGet.Protocol.Plugins;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CryptMail.Controllers
{
    public partial class MailController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> sendNewMessageChat(NewMessageModel messageModel)
        {   //  відправка нового повідомлення

            messageModel.userToId = await DBHelper.getGuidFromLogin(messageModel.recieverLogin, this);
            string partner = messageModel.PartnerLogin;
            if (messageModel.userToId == null)
            {
                //  користувача не знайдено у базі

                messageModel.onFormMessageColor = "red";
                messageModel.onFormMessage = "No receiver with such login!";
                Response.StatusCode = 201;  //  щоб поля на формі не очищалися
                return PartialView("NewMessageFormPartial", messageModel);
            }
            else
            {
                //  користувач є
                //  почати відправку

                string? senderLogin = Request.Cookies["cryptMailUser"];
                senderLogin = await DBHelper.getLoginFromGuid(new Guid(senderLogin), this);

                await DBHelper.ExecuteWithConnectionAsync(connection =>
                {   //  для БД - додати повідомлення у таблицю

                    if (messageModel.alg == 1 && !string.IsNullOrWhiteSpace(messageModel.key))
                        messageModel.Message = CoderHelper.Encoder1(messageModel.Message, messageModel.key);

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

            return RedirectToAction("OpenChat", "Mail", new { login = partner });
        }

        [HttpPost]
        public async Task<IActionResult> DecodeMessages(ChatModel chatModel)
        {
            return await RunWithCookie(async (cookie) =>
            {
                if (string.IsNullOrWhiteSpace(chatModel.newMessage.key)) return Json(new { chatModel });

                foreach (MessageModel message in chatModel.messages)
                {
                    if (chatModel.newMessage.alg == 1)
                    {
                        message.Message = CoderHelper.Decoder1(message.Message, chatModel.newMessage.key);
                    }
                }

                Response.StatusCode = 200;
                return Json(new { chatModel });
            });
        }

        [HttpPost]
        public async Task<IActionResult> getNextPage(ChatModel chatModel)
        {
            return await RunWithCookie(async (cookie) =>
            {
                bool err = false;
                chatModel.currentPage++;
                await DBHelper.ExecuteWithConnectionAsync(connection =>
                {
                    try
                    {
                        SqlCommand command = new SqlCommand("exec GetChatMessages @userId, @partnerLogin, @pageNumber;", connection);
                        command.Parameters.AddWithValue("@userId", cookie);
                        command.Parameters.AddWithValue("@partnerLogin", chatModel.newMessage.PartnerLogin);
                        command.Parameters.AddWithValue("@pageNumber", chatModel.currentPage);

                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        DataSet set = new DataSet();
                        adapter.Fill(set);

                        foreach (DataRow row in set.Tables[0].Rows)
                        {
                            chatModel.messages.Add(new MessageModel
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
                return Json(new { chatModel });
            });
        }
    }
}
