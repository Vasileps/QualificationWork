using MessengerServer.Database.Interfaces;
using MessengerServer.DataModels;
using MessengerServer.Services.Interfaces;
using MongoDB.Driver;

namespace MessengerServer.Controllers
{
    [ApiController]
    [Route(nameof(Chats))]
    public class Chats : Controller
    {
        private readonly IChatRepository chatRep;
        private readonly IUserRepository userRep;
        private readonly IContactListRepository contactListRep;
        private readonly IChatListRepository chatListRep;
        private readonly INotificationService notifications;
        private readonly IConfiguration configuration;

        public Chats(IChatRepository chatRepository, IUserRepository userRepository, IChatListRepository chatListRepository,
            INotificationService notificationService, IConfiguration configuration, IContactListRepository contactListRepository)
        {
            this.chatRep = chatRepository;
            this.notifications = notificationService;
            this.configuration = configuration;
            this.userRep = userRepository;
            this.contactListRep = contactListRepository;
            this.chatListRep = chatListRepository;
        }

#nullable enable
        [HttpPost]
        [Route(nameof(CreatePersonalChat))]
        [AuthRequired]
        public async Task<ActionResult> CreatePersonalChat([FromBody] CreatePersonalChatSchema schema)
        {
            if (string.IsNullOrEmpty(schema.ContactID)) return BadRequest("SearchStringNullOrEmpty");

            var userID = (string)HttpContext.Items["UserID"]!;

            if (userID == schema.ContactID) return BadRequest("ContactIDCantBeYours");

            var contact = await userRep.GetByIDAsync(schema.ContactID);
            if (contact is null) return BadRequest("ContactDoesntExist");

            var userContacts = await contactListRep.GetByUserIDAsync(userID);
            if (userContacts is null) userContacts = await contactListRep.AddAsync(new(userID));

            if (userContacts.Contacts.Contains(schema.ContactID))
                return BadRequest("ChatAlreadyExits");

            await contactListRep.AddContactAsync(userID, schema.ContactID);
            var chat = await chatRep.AddAsync(Chat.Personal(userID, schema.ContactID));
            await chatListRep.AddChatToManyAsync(chat.ID, userID, schema.ContactID);

            await notifications.ChatCreated(chat);

            return Ok();
        }

        [HttpPost]
        [Route(nameof(CreateGroupChat))]
        [AuthRequired]
        public ActionResult CreateGroupChat()
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route(nameof(DeleteChat))]
        [AuthRequired]
        public ActionResult DeleteChat()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Route(nameof(GetChats))]
        [AuthRequired]
        public async Task<ActionResult> GetChats([FromBody] GetChatsSchema schema)
        {
            if (schema.Count <= 0) return BadRequest("CountShouldBePositive");
            var userID = (string)HttpContext.Items["UserID"]!;

            var chatList = await chatListRep.GetByUserIDAsync(userID);
            if (chatList == null)
            {
                await chatListRep.AddAsync(new(userID));
                return Json(new Chat[0]);
            }

            var chats = await chatRep.GetChatsAsync(schema.Count, schema.SkipWhileID, chatList.Chats);
            chats = chats.ToArray();

            foreach (var chat in chats)
            {
                if (chat.Type != ChatType.Personal) continue;
                var contactID = chat.MembersIDs.First(x => x != userID);
                var user = await userRep.GetByIDAsync(contactID);
                chat.Name = user is null ? "UserNotFound" : user.Username;
            }

            return Json(chats);
        }
    }
}

