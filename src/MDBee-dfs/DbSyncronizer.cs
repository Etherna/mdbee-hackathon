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
using Etherna.MongoDBSyncer;
using Etherna.MongoDBSyncer.EventArgs;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.MDBeeDfs
{
    public class DbSyncronizer
    {
        // Consts.
        private const string DbInfoTableName = "db_info";
        private const string LastOplogKeyName = "last_oplog";

        // Fields.
        private readonly CookieContainer cookieContainer = new();
        private readonly DfsClient dfsClient;
        private readonly string dfsUrl;
        private readonly List<string> existingDocumentDbs = new();
        private readonly HttpClient httpClient;
        private readonly string mongoUrl;
        private readonly string databaseName;
        private readonly string password;
        private readonly string username;

        // Constructors.
        public DbSyncronizer(
            string username,
            string password,
            string dfsUrl,
            string mongoUrl,
            string databaseName)
        {
            this.username = username;
            this.password = password;
            this.dfsUrl = dfsUrl;
            this.mongoUrl = mongoUrl;
            this.databaseName = databaseName;

            // Create Dfs client.
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            httpClient = new HttpClient(handler) { BaseAddress = new Uri(dfsUrl) };

            dfsClient = new DfsClient(httpClient);
        }

        // Methods.
        public async Task StartAsync()
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

            // Get sync state.
            var lastOplogTimestamp = await TryGetLastOplogTimestamp();

            Console.WriteLine(lastOplogTimestamp is not null ?
                $"Last synced oplog: {lastOplogTimestamp}" :
                "No oplog found, start synchronization from scratch");

            // Get existing document dbs.
            existingDocumentDbs.AddRange(await GetExistingDocumentDbNamesAsync());

            // Start sync process.
            var syncProcessor = new SyncProcessor(mongoUrl, databaseName, lastOplogTimestamp);
            syncProcessor.OnDocumentInserted += OnDocumentInserted;
            syncProcessor.OnDocumentDeleted += OnDocumentDeleted;
            syncProcessor.OnRebuildPod += OnRebuildPod;

            await syncProcessor.StartAsync();
        }

        // Event handlers.
        private void OnDocumentInserted(object? sender, OnDocumentInsertedEventArgs e) => Task.Run(async () =>
        {
            // Create document db if doesn't exist.
            if (!existingDocumentDbs.Contains(e.CollectionName))
            {
                await dfsClient.DocNewAsync(e.CollectionName);
                existingDocumentDbs.Add(e.CollectionName);

                Console.WriteLine($"Created documentDb {e.CollectionName}");
            }

            // Open document db.
            //use try-catch because API rise error if already opened
            try { await dfsClient.DocOpenAsync(e.CollectionName); }
            catch { }

            /*
             * Adjust id name.
             * MongoDB use "_id" as default id name, instead fairos-dfs use "id".
             * For simplicity here I force to rename the id from "_id" into "id". 
             */
            var fixedBsonDocument = new BsonDocument(e.NewDocument.Elements.Select(
                e => e.Name == "_id" ?
                     new BsonElement("id", e.Value) :
                     e));

            // Add document.
            var objDocument = BsonTypeMapper.MapToDotNetValue(fixedBsonDocument);
            var jsonDocument = JsonConvert.SerializeObject(objDocument);
            await dfsClient.DocEntryPutAsync(e.CollectionName, jsonDocument);

            // Update sync state.
            if (e.OplogTimestamp is not null)
                await UpdateLastOplogTimestamp(e.OplogTimestamp.Value);

            Console.WriteLine($"Inserted document with key {e.DocumentKey.Value} in {e.CollectionName}");
        }).Wait();

        private void OnDocumentDeleted(object? sender, OnDocumentDeletedEventArgs e) => Task.Run(async () =>
        {
            // Open document db.
            //use try-catch because API rise error if already opened
            try { await dfsClient.DocOpenAsync(e.CollectionName); }
            catch { }

            // Remove document.
            try { await dfsClient.DocEntryDelAsync(e.CollectionName, e.DocumentKey.Value.ToString()!); }
            catch { }

            // Update sync state.
            await UpdateLastOplogTimestamp(e.OplogTimestamp.Value);

            Console.WriteLine($"Removed document with key {e.DocumentKey.Value} from {e.CollectionName}");
        }).Wait();

        private void OnRebuildPod(object? sender, OnRebuildPodEventArgs e) => Task.Run(async () =>
        {
            // Rebuild pod.
            //delete old
            var existingPods = (await dfsClient.PodLsAsync()).Result;
            if (existingPods.Pod_name.Contains(databaseName))
            {
                //await dfsClient.PodDeleteAsync(databaseName);
                throw new NotImplementedException("PodDelete have issues");
            }

            //create new
            var newPodResponse = await dfsClient.PodNewAsync(databaseName, password);

            Console.WriteLine($"Created new pod {databaseName}");

            // Populate it.
            //create db_info table
            await dfsClient.KvNewAsync(DbInfoTableName, IndexType.String);

            //init sync state.
            await UpdateLastOplogTimestamp(0);

            Console.WriteLine($"Created and initialized table {DbInfoTableName}");
        }).Wait();

        // Private helpers.
        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        private static string GenerateMnemonic()
        {
            // Load words.
            var stream = File.OpenText(@"Resources\bip39english.txt");
            var words = new List<string>();
            while (!stream.EndOfStream)
                words.Add(stream.ReadLine()!);

            // Select 12 words.
            var selectedWords = new List<string>();
            var rand = new Random();
            for (int i = 0; i < 12; i++)
                selectedWords.Add(words[rand.Next(0, words.Count)]);

            // Result.
            var mnemonic = string.Join(' ', selectedWords);
            return mnemonic;
        }

        private async Task<IEnumerable<string>> GetExistingDocumentDbNamesAsync()
        {
            var names = new List<string>();
            try
            {
                // Try to open pod.
                await dfsClient.PodOpenAsync(databaseName, password);

                // Get names.
                var docLsResponse = await dfsClient.DocLsAsync();
                names.AddRange(docLsResponse.Result.Tables.Select(t => t.Name));
            }
            catch { }
            return names;
        }

        private async Task<BsonTimestamp?> TryGetLastOplogTimestamp()
        {
            try
            {
                // Try to open pod.
                await dfsClient.PodOpenAsync(databaseName, password);

                // Open "db_info" table.
                await dfsClient.KvOpenAsync(DbInfoTableName);

                // Get current sync state.
                BsonTimestamp? lastOplogTimestamp = null;
                var lastOplogResponse = await dfsClient.KvEntryGetAsync(DbInfoTableName, LastOplogKeyName);
                lastOplogTimestamp = new BsonTimestamp(long.Parse(Base64Decode(lastOplogResponse.Result.Values)));

                return lastOplogTimestamp;
            }
            catch { }

            return null;
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

        private async Task UpdateLastOplogTimestamp(long oplogNumber)
        {
            await dfsClient.KvOpenAsync(DbInfoTableName);
            await dfsClient.KvEntryPutAsync(DbInfoTableName, LastOplogKeyName, oplogNumber.ToString());
        }
    }
}
