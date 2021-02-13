using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etherna.HackathonDemo.Commands
{
    class AddCommandRunner
    {
        // Fields.
        private readonly MongoClient client;
        private readonly IMongoDatabase database;
        private readonly Random random = new();

        // Constructor.
        public AddCommandRunner(string mongoString, string databaseName)
        {
            // Initialize MongoDB driver.
            client = new MongoClient(mongoString);
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
            return "rand";
        }
    }
}
