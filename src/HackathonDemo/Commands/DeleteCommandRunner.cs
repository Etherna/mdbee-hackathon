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
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Etherna.HackathonDemo.Commands
{
    class DeleteCommandRunner
    {
        // Fields.
        private readonly IMongoDatabase database;

        // Constructor.
        public DeleteCommandRunner(string mongoString, string databaseName)
        {
            // Initialize MongoDB driver.
            var client = new MongoClient(mongoString);
            database = client.GetDatabase(databaseName);
        }

        // Methods.
        public async Task RunAsync(string[] commandSegments)
        {
            // Read parameters.
            if (commandSegments.Length != 3)
            {
                Console.WriteLine("Invalid command composition");
                return;
            }
            if (!int.TryParse(commandSegments[1], out int totDocuments))
            {
                Console.WriteLine("Invalid number of documents");
                return;
            }
            if (string.IsNullOrEmpty(commandSegments[2]))
            {
                Console.WriteLine("Invalid collection name");
                return;
            }
            var collectioName = commandSegments[2];

            // Find documents to remove.
            var collection = database.GetCollection<BsonDocument>(collectioName);
            var documentIds = collection.AsQueryable()
                .Take(totDocuments)
                .ToArray()
                .Select(doc => doc.GetElement("_id").Value);

            // Delete documents.
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.AnyIn("_id", documentIds));

            Console.WriteLine($"{documentIds.Count()} documents deleted from {collectioName}");
        }
    }
}
