using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Etherna.MongoDBSyncer.EventArgs
{
    public class OnDocumentInsertedEventArgs
    {
        public OnDocumentInsertedEventArgs(BsonDocument newDocument, long opLogNumber)
        {
            NewDocument = newDocument;
            OpLogNumber = opLogNumber;
        }

        public BsonDocument NewDocument { get; }
        public long OpLogNumber { get; }
    }
}
