//   Copyright 2021 Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Etherna.MongoDBSyncer.EventArgs;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Etherna.MongoDBSyncer
{
    public class SyncProcessor
    {
        // Enums.
        public enum SyncState { Initializing, Syncing }

        // Fields.
        private readonly MongoClient client;
        private readonly IMongoDatabase database;
        private readonly OplogFetcher oplogFetcher;
        private Task? oplogReceiverTask;

        // Constructors.
        public SyncProcessor(string connectionString, string databaseName, BsonTimestamp? lastOplogTimestamp)
        {
            ConnectionString = connectionString;
            DatabaseName = databaseName;
            LastOplogTimestamp = lastOplogTimestamp;

            // Initialize MongoDB driver.
            client = new MongoClient(connectionString);
            database = client.GetDatabase(databaseName);

            // Init.
            oplogFetcher = new OplogFetcher(database, lastOplogTimestamp);
        }

        // Properties.
        public string DatabaseName { get; }
        public BsonTimestamp? LastOplogTimestamp { get; private set; }
        public string ConnectionString { get; }

        // Events.
        public event EventHandler<OnDocumentInsertedEventArgs>? OnDocumentInserted;
        public event EventHandler<OnDocumentRemovedEventArgs>? OnDocumentRemoved;
        public event EventHandler<OnDocumentReplacedEventArgs>? OnDocumentReplaced;
        public event EventHandler<OnRebuildPodEventArgs>? OnRebuildPod;

        // Methods.
        public async Task StartAsync()
        {
            // Start receive oplogs.
            oplogReceiverTask = oplogFetcher.StartReceive();

            // Verify if need initial sync.
            /*
             * An initial sync should be performed if it's the first time that the db is syncing,
             * or if it's impossibile to recover a state from last oplog timestamp. Because this is
             * an exceptional case, not suitable for the hackathon, and because I've problems
             * deleting pods, this case is ignored here.
             */
            //var oldestAvailableOplog = new BsonTimestamp(42); //TODO. Get from db
            if (LastOplogTimestamp is null/* || LastOplogTimestamp < oldestAvailableOplog*/)
            {
                long docCounter = 0;

                // Rebuild destination pod.
                OnRebuildPod?.Invoke(this, new OnRebuildPodEventArgs(DatabaseName));

                // Enumerate and clone collections.
                var collectionNames = await database.ListCollectionNamesAsync();
                while (await collectionNames.MoveNextAsync())
                    foreach (var collectionName in collectionNames.Current)
                    {
                        var collection = database.GetCollection<BsonDocument>(collectionName);
                        await collection.Find(FilterDefinition<BsonDocument>.Empty, new FindOptions { NoCursorTimeout = true })
                            .ForEachAsync(document =>
                            {
                                //try to get key
                                var element = document.GetElement("_id");

                                OnDocumentInserted?.Invoke(this,
                                    new OnDocumentInsertedEventArgs(DatabaseName, collectionName, element, document, null));
                                
                                docCounter++;
                            });
                    }

                Console.WriteLine($"First sync: imported {docCounter} documents");
            }

            // Process oplogs.
            while (true)
            {
                while (oplogFetcher.OplogBuffer.TryDequeue(out var oplog))
                {
                    switch (oplog.OperationType)
                    {
                        case ChangeStreamOperationType.Insert:
                            OnDocumentInserted?.Invoke(this,
                                new OnDocumentInsertedEventArgs(
                                    DatabaseName,
                                    oplog.CollectionNamespace.CollectionName,
                                    oplog.DocumentKey.Elements.First(),
                                    oplog.FullDocument,
                                    oplog.ClusterTime));
                            break;
                        case ChangeStreamOperationType.Update:
                            break;
                        case ChangeStreamOperationType.Replace:
                            break;
                        case ChangeStreamOperationType.Delete:
                            break;
                        case ChangeStreamOperationType.Invalidate:
                            break;
                        case ChangeStreamOperationType.Rename:
                            break;
                        case ChangeStreamOperationType.Drop:
                            break;
                        default:
                            break;
                    }
                }

                await Task.Delay(100);
            }
        }
    }
}
