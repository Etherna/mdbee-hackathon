using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Etherna.MongoDBSyncer.EventArgs
{
    public class OnDocumentRemovedEventArgs
    {
        public OnDocumentRemovedEventArgs(BsonElement docId, long opLogNumber)
        {
            DocId = docId;
            OpLogNumber = opLogNumber;
        }

        public BsonElement DocId { get; }
        public long OpLogNumber { get; }
    }
}
