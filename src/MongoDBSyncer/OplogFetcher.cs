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

using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Etherna.MongoDBSyncer
{
    public class OplogFetcher
    {
        // Constructor.
        public OplogFetcher(
            IMongoDatabase database,
            BsonTimestamp? lastOplogTimestamp)
        {
            Database = database;
            LastOplogTimestamp = lastOplogTimestamp;
        }

        // Properties.
        public IMongoDatabase Database { get; }
        public ConcurrentQueue<ChangeStreamDocument<BsonDocument>> OplogBuffer { get; } = new();
        public BsonTimestamp? LastOplogTimestamp { get; }

        // Methods.
        public async Task StartReceive()
        {
            var cursor = Database.Watch(new ChangeStreamOptions
            {
                StartAtOperationTime = LastOplogTimestamp
            });

            //keep cycling
            while (true)
            {
                //keep calling MoveNext until we've read the first batch
                while (await cursor.MoveNextAsync() && !cursor.Current.Any())
                { }

                //enqueue documents
                foreach (var changeDoc in cursor.Current)
                    OplogBuffer.Enqueue(changeDoc);
            }
        }
    }
}
