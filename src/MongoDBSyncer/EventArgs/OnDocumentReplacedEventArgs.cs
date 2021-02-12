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
            BsonDocument newDocument,
            long opLogNumber)
        {
            DocId = docId;
            NewDocument = newDocument;
            OpLogNumber = opLogNumber;
        }

        public BsonElement DocId { get; }
        public BsonDocument NewDocument { get; }
        public long OpLogNumber { get; }
    }
}
