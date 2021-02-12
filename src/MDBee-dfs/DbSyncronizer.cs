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
    public class DbSyncronizer
    {
        // Consts.
        private const string DbInfoTableName = "db_info";
        private const string LastOpLogKeyName = "last_oplog";

        // Fields.
        private readonly CookieContainer cookieContainer = new();
        private readonly DfsClient dfsClient;
        private readonly string dfsUrl;
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
            // Login user with Dfs.
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
        private void OnDocumentInserted(object? sender, OnDocumentInsertedEventArgs e) => Task.Run(async () =>
        {
            // Add document.
            //***TO-DO

            // Update sync state.
            await UpdateOpLogNumber(e.OpLogNumber);
        }).Wait();

        private void OnDocumentRemoved(object? sender, OnDocumentRemovedEventArgs e) => Task.Run(async () =>
        {
            // Remove document.
            //***TO-DO

            // Update sync state.
            await UpdateOpLogNumber(e.OpLogNumber);
        }).Wait();

        private void OnDocumentReplaced(object? sender, OnDocumentReplacedEventArgs e) => Task.Run(async () =>
        {
            // Replace document.
            //***TO-DO

            // Update sync state.
            await UpdateOpLogNumber(e.OpLogNumber);
        }).Wait();

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

        private async Task UpdateOpLogNumber(long opLogNumber)
        {
            await dfsClient.KvOpenAsync(DbInfoTableName);
            await dfsClient.KvEntryPutAsync(DbInfoTableName, LastOpLogKeyName, opLogNumber.ToString());
        }
    }
}
