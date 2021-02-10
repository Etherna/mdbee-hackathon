using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Etherna.MongoDBSyncer.EventArgs
{
    public class OnDocumentReplacedEventArgs
    {
        public OnDocumentReplacedEventArgs(
            BsonElement docId,
            BsonDocument newDocument)
        {
            DocId = docId;
            NewDocument = newDocument;
        }

        public BsonElement DocId { get; }
        public BsonDocument NewDocument { get; }
    }
}
