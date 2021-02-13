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
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.HackathonDemo.Commands
{
    class InsertCommandRunner
    {
        // Fields.
        private readonly IMongoDatabase database;
        private readonly Random random = new();

        // Constructor.
        public InsertCommandRunner(string mongoString, string databaseName)
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

            // Generate documents.
            List<BsonDocument> documents = new();
            for (int i = 0; i < totDocuments; i++)
                documents.Add(GenerateRandomDocument());

            // Insert documents.
            var collection = database.GetCollection<BsonDocument>(collectioName);
            await collection.InsertManyAsync(documents);

            Console.WriteLine($"{totDocuments} documents inserted into {collectioName}");
        }

        // Helpers.
        private BsonDocument GenerateRandomDocument()
        {
            var document = new BsonDocument();

            for (int i = 0; i < random.Next(1, 10); i++)
                document.Add(new BsonElement(
                    GenerateRandomString(random.Next(4, 10)),
                    new BsonString(GenerateRandomString(random.Next(4, 10)))));

            return document;
        }

        private string GenerateRandomString(int length)
        {
            var strBuilder = new StringBuilder();
            for (int i = 0; i < length; i++)
                strBuilder.Append((char)('a' + random.Next(26)));
            return strBuilder.ToString();
        }
    }
}
