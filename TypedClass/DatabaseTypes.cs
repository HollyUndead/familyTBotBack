using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace familyTBotBack.TypedClass
{
    public class User
    {
        [BsonId]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        [BsonElement("userName")]
        public string UserName { get; set; } = "";

        [BsonElement("role")]
        public string Role { get; set; } = "";

        [BsonElement("userId")]
        public int UserId { get; set; }

        [BsonElement("is_active")]
        public bool IsActive { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Profile
    {
        [BsonElement("userId")]
        public long? UserId { get; set; }
        [BsonElement("userName")]
        public string UserName { get; set; } = "";
        [BsonElement("assignedTasks")]
        public string[] AssignedTasks { get; set; } = [];
        [BsonElement("completedTasks")]
        public string[] CompletedTasks {  get; set; } = [];
        [BsonElement("overdueTasks")] 
        public string[] OverdueTasks { get; set; } = [];
        [BsonElement("starHistory")] 
        public string[] StarHistory { get; set;} = [];
        [BsonElement("authOnWeb")]
        public Boolean? AuthOnWeb { get; set; }
    }
}
