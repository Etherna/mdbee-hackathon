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
    class Program
    {
        // Consts.
        private const string DbInfoTableName = "db_info";
        private const string HelpText =
            "MDBee-dfs help:\n\n" +
            "-u\tDfs username\n" +
            "-p\tDfs password\n" +
            "-m\tMongoDB URL\n" +
            "-f\tFairOS-dfs server URL\n" +
            "-d\tDatabase name\n" +
            "\n" +
            "-h\tPrint help\n";
        private const string LastOpLogKeyName = "last_oplog";

        // Methods.
        static async Task Main(string[] args)
        {
            // Parse arguments.
            string? databaseName = null;
            string? dfsUrl = null;
            string? mongoUrl = null;
            string? password = null;
            string? username = null;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-d": databaseName = args[++i]; break;
                    case "-f": dfsUrl = args[++i]; break;
                    case "-m": mongoUrl = args[++i]; break;
                    case "-p": password = args[++i]; break;
                    case "-u": username = args[++i]; break;
                    case "-h": Console.Write(HelpText); return;
                    default: throw new ArgumentException(args[i] + " is not a valid argument");
                }
            }

            // Request Dfs credentials.
            Console.WriteLine("Dfs username:");
            username = ReadStringIfEmpty(username);

            Console.WriteLine();
            Console.WriteLine("Dfs password:");
            if (password is null)
            {
                var strBuilder = new StringBuilder();
                while (password is null)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        if (strBuilder.Length == 0)
                            Console.WriteLine("*Invalid empty password*");
                        else
                        {
                            password = strBuilder.ToString();
                            Console.WriteLine("*Ok*");
                        }
                    }
                    else
                        strBuilder.Append(key.KeyChar);
                }
            }
            else Console.WriteLine("*****");

            // Request connection urls.
            Console.WriteLine();
            Console.WriteLine("MongoDB connection url:");
            mongoUrl = ReadStringIfEmpty(mongoUrl);

            Console.WriteLine();
            Console.WriteLine("FairOS-dfs server url:");
            dfsUrl = ReadStringIfEmpty(dfsUrl);

            // Request sync config.
            Console.WriteLine();
            Console.WriteLine("Syncing database name:");
            databaseName = ReadStringIfEmpty(databaseName);

            // Create Dfs client.
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookieContainer } ;
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri(dfsUrl) } ;

            var dfsClient = new DfsClient(httpClient);

            // Login user with Dfs.
            if ((await dfsClient.UserPresentAsync(username)).Result.Present)
            {
                // Login.
                var response = await dfsClient.UserLoginAsync(username, password);
                TrySetCookies(dfsUrl, cookieContainer, response.Headers.ToDictionary(p => p.Key, p => p.Value));

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
                TrySetCookies(dfsUrl, cookieContainer, response.Headers.ToDictionary(p => p.Key, p => p.Value));
                var result = response.Result;

                Console.WriteLine();
                Console.WriteLine($"New user {username} created with address {result.Address}.");
                Console.WriteLine("Please store the following 12 words safely");
                Console.WriteLine("=============== Mnemonic ==========================");
                Console.WriteLine(result.Mnemonic);
                Console.WriteLine("=============== Mnemonic ==========================");
            }

            // Create pod for db, if doesn't exist, and open.
            var existingPods = (await dfsClient.PodLsAsync()).Result;
            if (!existingPods.Pod_name.Contains(databaseName))
            {
                var newPodResponse = await dfsClient.PodNewAsync(databaseName, password);

                Console.WriteLine();
                Console.WriteLine($"Created pod {databaseName}");
            }
            var podOpenResponse = await dfsClient.PodOpenAsync(databaseName, password);

            // Create "db_info" table, if doesn't exist.
            var existingTables = (await dfsClient.KvLsAsync()).Result;
            if (existingTables.Tables is null ||
                !existingTables.Tables.Any(t => t.Name == DbInfoTableName))
            {
                var newTableResponse = await dfsClient.KvNewAsync(DbInfoTableName, IndexType.String);

                Console.WriteLine();
                Console.WriteLine($"Created table {DbInfoTableName}");
            }
            var kvOpenResponse = await dfsClient.KvOpenAsync(DbInfoTableName);

            // Get current sync state.
            var lastOpLog = -1;
            try
            {
                var lastOpLogResponse = await dfsClient.KvEntryGetAsync(DbInfoTableName, LastOpLogKeyName);
                lastOpLog = int.Parse(Base64Decode(lastOpLogResponse.Result.Values));
            }
            catch { }

            Console.WriteLine();
            Console.WriteLine(lastOpLog >= 0 ?
                $"Last synced oplog: {lastOpLog}" :
                "No oplog found, start synchronization from scratch");

            // Start sync process.
            var syncProcessor = new MongoDBSyncProcessor(mongoUrl, databaseName, lastOpLog);
            syncProcessor.OnDocumentInserted += OnDocumentInserted;
            syncProcessor.OnDocumentRemoved += OnDocumentRemoved;
            syncProcessor.OnDocumentReplaced += OnDocumentReplaced;

            syncProcessor.StartSync();
        }

        // Event handlers.
        private static void OnDocumentInserted(object? sender, OnDocumentInsertedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnDocumentRemoved(object? sender, OnDocumentRemovedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void OnDocumentReplaced(object? sender, OnDocumentReplacedEventArgs e)
        {
            throw new NotImplementedException();
        }

        // Private helpers.
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string Base64Encode(string plainText)
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

        private static string ReadStringIfEmpty(string? strValue)
        {
            if (string.IsNullOrWhiteSpace(strValue))
            {
                while (string.IsNullOrWhiteSpace(strValue))
                {
                    strValue = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(strValue))
                        Console.WriteLine("*Empty string not allowed*");
                }
            }
            else Console.WriteLine(strValue);

            return strValue;
        }

        private static void TrySetCookies(
            string host,
            CookieContainer cookieContainer,
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
                    cookieContainer.SetCookies(new Uri(host), fixedCookie);
                }
        }
    }
}
