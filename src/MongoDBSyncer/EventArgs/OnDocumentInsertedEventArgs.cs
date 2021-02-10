using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Etherna.MongoDBSyncer.EventArgs
{
    public class OnDocumentInsertedEventArgs
    {
        public OnDocumentInsertedEventArgs(BsonDocument newDocument)
        {
            NewDocument = newDocument;
        }

        public BsonDocument NewDocument { get; }
    }
}
