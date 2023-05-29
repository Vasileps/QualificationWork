using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MessengerServer.Database.Documents
{
    public class Message : Entity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string ChatID { get; set; }

        [BsonIgnoreIfNull]
        public string Data { get; set; }

        [BsonRequired]
        public MessageType Type { get; set; }

        [BsonRequired]
        public DateTime SendTime { get; set; }

        [BsonRequired]
        public DateTime UpdateTime { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string SendBy { get; set; }

        [BsonIgnoreIfDefault]
        [BsonDefaultValue(false)]
        public bool IsDeleted { get; set; }

        [BsonConstructor]
        private Message() { }

        private Message(string chatID, string userID, DateTime sendTime)
        {
            ChatID = chatID;
            SendTime = sendTime;
            SendBy = userID;
            UpdateTime = DateTime.Now;
            IsDeleted = false;
        }

        public static Message TextMessage(string chatID, string userID, DateTime sendTime, string text)
        {
            var message = new Message(chatID, userID, sendTime);
            message.Type = MessageType.Text;
            message.Data = text;
            return message;
        }
    }

    public enum MessageType : ushort
    {
        Text,
    }
}
