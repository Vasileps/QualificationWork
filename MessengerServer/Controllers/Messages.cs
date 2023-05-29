﻿using MessengerServer.Database.Interfaces;
using MessengerServer.DataModels;
using MessengerServer.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MessengerServer.Controllers
{
    [ApiController]
    [Route(nameof(Messages))]
    public class Messages : Controller
    {
        private static TimeSpan mistake = TimeSpan.FromMinutes(1);

        private readonly IMessageRepository messageRep;
        private readonly IChatRepository chatRep;
        private readonly INotificationService notifications;

        public Messages(IMessageRepository messageRepository, IChatRepository chatRepository,
            INotificationService notificationService)
        {
            this.chatRep = chatRepository;
            this.messageRep = messageRepository;
            this.notifications = notificationService;
        }

        [HttpGet]
        [Route(nameof(GetMessagesDescending))]
        [AuthRequired]
        public async Task<ActionResult> GetMessagesDescending([FromBody] GetMessagesSchema schema)
        {
            if (schema.Count <= 0) return BadRequest("CountShouldBePositive");

            var chat = await chatRep.GetByIDAsync(schema.ChatID);
            if (chat is null) return BadRequest("ChatDoesntExist");

            var userID = (string)HttpContext.Items["UserID"]!;

            if (!chat.MembersIDs.Contains(userID)) return BadRequest("NotMemberOfChat");

            var messages = await messageRep.GetMessagesDescendingAsync(schema.ChatID, schema.Count, schema.SkipWhileID);

            return Json(messages);
        }

        [HttpGet]
        [Route(nameof(GetMessagesAscending))]
        [AuthRequired]
        public async Task<ActionResult> GetMessagesAscending([FromBody] GetMessagesSchema schema)
        {
            if (schema.Count <= 0) return BadRequest("CountShouldBePositive");

            var chat = await chatRep.GetByIDAsync(schema.ChatID);
            if (chat is null) return BadRequest("ChatDoesntExist");

            var userID = (string)HttpContext.Items["UserID"]!;

            if (!chat.MembersIDs.Contains(userID)) return BadRequest("NotMemberOfChat");

            var messages = await messageRep.GetMessagesAscendingAsync(schema.ChatID, schema.Count, schema.SkipWhileID);

            return Json(messages);
        }

        [HttpPost]
        [Route(nameof(AddTextMessage))]
        [AuthRequired]
        public async Task<ActionResult> AddTextMessage([FromBody] AddTextMessageSchema schema)
        {
            if (string.IsNullOrWhiteSpace(schema.Text)) return BadRequest("TextNullOrEmpty");

            var chat = await chatRep.GetByIDAsync(schema.ChatID);
            if (chat is null) return BadRequest("ChatDoesntExist");

            var userID = (string)HttpContext.Items["UserID"]!;
            if (!chat.MembersIDs.Contains(userID)) return BadRequest("NotMemberOfChat");

            var actualSendTime = DateTime.Now - schema.SendTime > mistake ? DateTime.Now : schema.SendTime;

            var message = Message.TextMessage(schema.ChatID, userID, actualSendTime, schema.Text);

            await messageRep.AddAsync(message);

            await notifications.MessageRecieved(message, chat.MembersIDs);

            return Ok();
        }

        [HttpPost]
        [Route(nameof(AddFileMessage))]
        [AuthRequired]
        public ActionResult AddFileMessage()
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route(nameof(EditTextMessage))]
        [AuthRequired]
        public async Task<ActionResult> EditTextMessage([FromBody] EditTextMessageSchema schema)
        {
            if (string.IsNullOrWhiteSpace(schema.Text)) return BadRequest("TextNullOrEmpty");

            var userID = (string)HttpContext.Items["UserID"]!;

            var message = await messageRep.GetByIDAsync(schema.MessageID);
            if (message is null) return BadRequest("MessageDoesntExists");

            var chat = await chatRep.GetByIDAsync(message.ChatID);
            if (chat is null) return StatusCode(500);

            if (message.SendBy != userID) return BadRequest("OnlySenderCanEditMessage");
            if (message.Type != MessageType.Text) return BadRequest("MessageTypeIsNotText");

            message.Data = schema.Text;
            await messageRep.UpdateAsync(message);

            await notifications.MessageUpdated(message, chat.MembersIDs);

            return Ok();
        }

        [HttpPost]
        [Route(nameof(DeleteMessage))]
        [AuthRequired]
        public async Task<ActionResult> DeleteMessage([FromBody] DeleteMessageSchema schema)
        {
            var userID = (string)HttpContext.Items["UserID"]!;

            var message = await messageRep.GetByIDAsync(schema.MessageID);
            if (message is null) return BadRequest("MessageDoesntExists");

            if (message.SendBy != userID) return BadRequest("OnlySenderCanDeleteMessage");

            var chat = await chatRep.GetByIDAsync(message.ChatID);
            if (chat is null) return StatusCode(500);

            message.IsDeleted = true;
            await messageRep.UpdateAsync(message);

            await notifications.MessageDeleted(message, chat.MembersIDs);

            return Ok();
        }
    }
}
