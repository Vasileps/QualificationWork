using MessengerServer.Database.Interfaces;
using MessengerServer.DataModels;
using MessengerServer.Services.Interfaces;
using MongoDB.Driver;
using IOFile = System.IO.File;

namespace MessengerServer.Controllers
{
    [ApiController]
    [Route(nameof(Files))]
    public class Files : Controller
    {
        private readonly IContactListRepository contactsRep;
        private readonly IChatRepository chatRep;
        private readonly IFileMetadataRepository metadataRep;
        private readonly INotificationService notifications;
        private readonly IFilesProvider filesProvider;

        public Files(IFileMetadataRepository metadataRepository, IChatRepository chatRepository,
            IContactListRepository contactListRepository, INotificationService notificationService, IFilesProvider filesProvider)
        {
            this.chatRep = chatRepository;
            this.contactsRep = contactListRepository;
            this.metadataRep = metadataRepository;
            this.notifications = notificationService;
            this.filesProvider = filesProvider;
        }

        [HttpPost]
        [Route(nameof(UpdateProfileImage))]
        [AuthRequired]
        public async Task<ActionResult> UpdateProfileImage(IFormCollection content)
        {
            var imageStream = content.Files["Image"];
            if (imageStream is null) return BadRequest("ImageNull");

            var image = Image.Load(imageStream.OpenReadStream());
            image.Mutate(x => x.Resize(256, 256));

            var userID = (string)HttpContext.Items["UserID"]!;
            var relativePath = Path.Combine("UserImages", userID);

            var stream = new MemoryStream();
            image.SaveAsJpeg(stream);
            stream.Flush();
            image.Dispose();

            await filesProvider.UpdateFileAsync(stream, relativePath);
            stream.Dispose();

            var metadata = await metadataRep.GetByPathAsync(relativePath);

            if (metadata is null) await metadataRep.AddAsync(new(relativePath, $"{userID}.jpg"));
            else
            {
                metadata.UpdateTime = DateTime.UtcNow;
                await metadataRep.UpdateAsync(metadata);
            }

            await notifications.UserImageUpdated(userID);

            return Ok();
        }

        [HttpGet]
        [Route(nameof(GetUserProfileImage))]
        [AuthRequired]
        public async Task<ActionResult> GetUserProfileImage([FromBody] GetUserProfileImageSchema schema)
        {
            if (string.IsNullOrEmpty(schema.UserID)) return BadRequest("UserIDNullOrEmpty");

            var relativePath = Path.Combine("UserImages", schema.UserID);

            var metadata = await metadataRep.GetByPathAsync(relativePath);
            var stream = await filesProvider.GetFileAsync(relativePath);

            if (stream is null) return BadRequest("UserDoesntHaveImage");

            return File(stream, "application/octet-stream", metadata?.FileName);
        }

        [HttpGet]
        [Route(nameof(GetChatImage))]
        [AuthRequired]
        public async Task<ActionResult> GetChatImage([FromBody] GetChatImageSchema schema)
        {
            if (string.IsNullOrEmpty(schema.ChatID)) return BadRequest("ChatIDNullOrEmpty");

            var chat = await chatRep.GetByIDAsync(schema.ChatID);
            if (chat is null) return BadRequest("ChatDoesntExist");

            var userID = (string)HttpContext.Items["UserID"]!;
            if (!chat.MembersIDs.Contains(userID)) return BadRequest("UserNotMemberOfChat");


            if (chat.Type == ChatType.Personal)
            {
                var contactID = chat.MembersIDs.Single(x => x != userID);
                var relativePath = Path.Combine("UserImages", contactID);

                var metadata = await metadataRep.GetByPathAsync(relativePath);
                var stream = await filesProvider.GetFileAsync(relativePath);

                if (stream is null) return BadRequest("UserDoesntHaveImage");
                return File(stream, "application/octet-stream", metadata?.FileName);
            }

            return StatusCode(500);
        }
    }
}
