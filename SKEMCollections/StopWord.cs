using MongoDB.Bson;

namespace SKEMCollections
{
    public class StopWord
    {
        public ObjectId id { get; set; }
        public string word { get; set; }
    }
}