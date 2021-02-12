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
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Etherna.MongoDBSyncer
{
    public class MongoDBSyncProcessor
    {
        // Enums.
        public enum SyncState { Initializing, Syncing }

        // Fields.
        private readonly MongoClient client;
        private readonly IMongoDatabase database;
        private readonly ConcurrentQueue<ChangeStreamDocument<BsonDocument>> opLogBuffer = new();
        private readonly OpLogReceiver opLogReceiver;
        private Task? opLogReceiverTask;

        // Constructors.
        public MongoDBSyncProcessor(string connectionString, string databaseName, BsonTimestamp? lastOpLogTimestamp)
        {
            ConnectionString = connectionString;
            DatabaseName = databaseName;
            LastOpLogTimestamp = lastOpLogTimestamp;

            // Initialize MongoDB driver.
            client = new MongoClient(connectionString);
            database = client.GetDatabase(databaseName);

            // Init.
            opLogReceiver = new OpLogReceiver(database, opLogBuffer, lastOpLogTimestamp);
        }

        // Properties.
        public string DatabaseName { get; }
        public BsonTimestamp? LastOpLogTimestamp { get; private set; }
        public string ConnectionString { get; }

        // Events.
        public event EventHandler<OnDocumentInsertedEventArgs>? OnDocumentInserted;
        public event EventHandler<OnDocumentRemovedEventArgs>? OnDocumentRemoved;
        public event EventHandler<OnDocumentReplacedEventArgs>? OnDocumentReplaced;
        public event EventHandler<OnRebuildPodEventArgs>? OnRebuildPod;

        // Methods.
        public void StartSync()
        {
            // Start receive oplogs.
            opLogReceiverTask = opLogReceiver.StartReceive();

            // Verify if need initial sync.
            var oldestAvailableOpLog = new BsonTimestamp(42); //TODO. Get from db
            if (LastOpLogTimestamp < 0 || LastOpLogTimestamp < oldestAvailableOpLog)
            {
                // Sync from scratch.
                OnRebuildPod?.Invoke(this, new OnRebuildPodEventArgs(DatabaseName));

                
            }

            // Process oplogs.
            while (true)
            {
                while (opLogBuffer.TryDequeue(out var document))
                {
                    switch (document.OperationType)
                    {
                        case ChangeStreamOperationType.Insert:
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

                Task.Delay(100);
            }
        }
    }
}
