using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Etherna.MongoDBSyncer.EventArgs
{
    public class OnDocumentRemovedEventArgs
    {
        public OnDocumentRemovedEventArgs(BsonElement docId)
        {
            DocId = docId;
        }

        public BsonElement DocId { get; }
    }
}
