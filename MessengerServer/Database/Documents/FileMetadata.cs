using MongoDB.Bson.Serialization.Attributes;

namespace MessengerServer.Database.Documents
{
    public class FileMetadata : Entity
    {
        [BsonRequired]
        public string RelativePath { get; set; }

        [BsonRequired]
        public string FileName { get; set; }

        [BsonRequired]
        public DateTime UpdateTime { get; set; }

        [BsonConstructor]
        private FileMetadata() { }

        public FileMetadata(string relativePath, string fileName)
        {
            RelativePath = relativePath;
            FileName = fileName;
            UpdateTime = DateTime.UtcNow;
        }
    }
}
