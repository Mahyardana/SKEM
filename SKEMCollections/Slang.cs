using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKEMCollections
{
    public class Slang
    {
        public ObjectId id { get; set; }
        public string acronym { get; set; }
        public string expansion { get; set; }
    }
}
