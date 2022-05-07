using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKEMCollections
{
    public class Tweet
    {
        public ObjectId id;
        public double sentimentScore;
        public string tweetID;
        public string authorName;
        public string authorUrl;
        public string fullText;
        public string[] words;
    }
}
