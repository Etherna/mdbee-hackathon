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

using Etherna.FairOSDfsClient;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Etherna.HackathonDemo.Commands
{
    class StatusCommandRunner
    {
        // Fields.
        private readonly CookieContainer cookieContainer = new();
        private readonly DfsClient dfsClient;
        private readonly string dfsUrl;
        private readonly string databaseName;
        private bool userLoggedIn;
        private IMongoDatabase database;

        // Constructor.
        public StatusCommandRunner(string dfsUrl, string mongoString, string databaseName)
        {
            this.dfsUrl = dfsUrl;
            this.databaseName = databaseName;

            // Create Dfs client.
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri(dfsUrl) };
            dfsClient = new DfsClient(httpClient);

            // Initialize MongoDB driver.
            var client = new MongoClient(mongoString);
            database = client.GetDatabase(databaseName);
        }

        // Methods.
        public async Task RunAsync(string username, string password)
        {
            // Login, only first time.
            if (!userLoggedIn)
            {
                await LoginUser(username, password);
                userLoggedIn = true;
            }

            // Try to create pod.
            try { await dfsClient.PodNewAsync(databaseName, password); }
            catch { }

            // Try to open pod.
            try { await dfsClient.PodOpenAsync(databaseName, password); }
            catch { }

            // Get document dbs stats from dfs.
            var dfsDocDbsStats = new Dictionary<string, long>();
            var docLsResponse = await dfsClient.DocLsAsync();
            if (docLsResponse.Result.Tables is not null)
                foreach (var docDbName in docLsResponse.Result.Tables.Select(t => t.Name))
                {
                    // Try to open document db.
                    try { await dfsClient.DocOpenAsync(docDbName); }
                    catch { }

                    // Count documents.
                    var countResponse = await dfsClient.DocCountAsync(docDbName);
                    if (!long.TryParse(countResponse.Result.Message, out var totDocuments))
                        continue;

                    // Add stats.
                    dfsDocDbsStats.Add(docDbName, totDocuments);
                }

            // Get collections stats from mongo.
            var mongoCollectionStats = new Dictionary<string, long>();
            var collectionsCursor = database.ListCollections();
            while (collectionsCursor.MoveNext())
            {
                foreach (var collectionBsonDoc in collectionsCursor.Current)
                {
                    var name = collectionBsonDoc.GetElement("name").Value.ToString()!;
                    var collection = database.GetCollection<BsonDocument>(name);
                    var totDocuments = collection.CountDocuments(Builders<BsonDocument>.Filter.Empty);

                    mongoCollectionStats.Add(name, totDocuments);
                }
            }

            // Print results.
            Console.WriteLine("Mongo collections - Tot documents");
            foreach (var stat in mongoCollectionStats.OrderBy(p => p.Key))
                Console.WriteLine($"* {stat.Key} - {stat.Value}");

            Console.WriteLine("---------------");

            Console.WriteLine("Dfs Document DBs - Tot documents");
            foreach (var stat in dfsDocDbsStats.OrderBy(p => p.Key))
                Console.WriteLine($"* {stat.Key} - {stat.Value}");
        }

        // Private helpers.
        private async Task LoginUser(string username, string password)
        {
            // Verify if user exists with Dfs.
            if ((await dfsClient.UserPresentAsync(username)).Result.Present)
            {
                // Login.
                var response = await dfsClient.UserLoginAsync(username, password);
                TrySetCookies(response.Headers.ToDictionary(p => p.Key, p => p.Value));

                Console.WriteLine();
                if (response.StatusCode == 200)
                {
                    Console.WriteLine($"User {username} loggedin");
                }
                else
                {
                    Console.WriteLine("Invalid Dfs login");
                    throw new UnauthorizedAccessException();
                }
            }
            else
            {
                // Signup.
                var response = await dfsClient.UserSignupAsync(
                    username,
                    password);
                TrySetCookies(response.Headers.ToDictionary(p => p.Key, p => p.Value));
                var result = response.Result;

                Console.WriteLine();
                Console.WriteLine($"New user {username} created with address {result.Address}.");
                Console.WriteLine("Please store the following 12 words safely");
                Console.WriteLine("=============== Mnemonic ==========================");
                Console.WriteLine(result.Mnemonic);
                Console.WriteLine("=============== Mnemonic ==========================");
            }
        }

        private void TrySetCookies(
            IDictionary<string, IEnumerable<string>> responseHeaders)
        {
            if (responseHeaders.TryGetValue("Set-Cookie", out var cookies))
                foreach (var cookie in cookies)
                {
                    // Purify cookie.
                    //from domain field. See https://github.com/fairDataSociety/fairOS-dfs/issues/52
                    var cookieParts = cookie.Split(';');
                    var cookieSafeParts = cookieParts.Where(cp => !cp.Contains("Domain="));

                    var fixedCookie = cookieSafeParts.First(); //string.Join(';', cookieSafeParts);

                    // Set cookie.
                    cookieContainer.SetCookies(new Uri(dfsUrl), fixedCookie);
                }
        }
    }
}
