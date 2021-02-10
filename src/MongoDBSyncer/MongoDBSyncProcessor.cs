using Etherna.MongoDBSyncer.EventArgs;
using System;

namespace Etherna.MongoDBSyncer
{
    public class MongoDBSyncProcessor
    {

        // Constructors.
        public MongoDBSyncProcessor(string mongoUrl, string databaseName)
        {
            MongoUrl = mongoUrl;
            DatabaseName = databaseName;
        }

        // Properties.
        public string DatabaseName { get; }
        public string MongoUrl { get; }

        // Events.
        public event EventHandler<OnDocumentInsertedEventArgs> OnDocumentInserted;
        public event EventHandler<OnDocumentRemovedEventArgs> OnDocumentRemoved;
        public event EventHandler<OnDocumentReplacedEventArgs> OnDocumentReplaced;

        // Methods.
        public void StartSync()
        {
        }
    }
}
