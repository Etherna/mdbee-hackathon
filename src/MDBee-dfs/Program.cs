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

using System;
using System.Text;
using System.Threading.Tasks;

namespace Etherna.MDBeeDfs
{
    class Program
    {
        // Consts.
        private const string HelpText =
            "MDBee-dfs help:\n\n" +
            "-u\tDfs username\n" +
            "-p\tDfs password\n" +
            "-m\tMongoDB connection string\n" +
            "-f\tFairOS-dfs server URL\n" +
            "-d\tDatabase name\n" +
            "\n" +
            "-h\tPrint help\n";

        // Methods.
        static async Task Main(string[] args)
        {
            // Parse arguments.
            string? databaseName = null;
            string? dfsUrl = null;
            string? mongoString = null;
            string? password = null;
            string? username = null;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-d": databaseName = args[++i]; break;
                    case "-f": dfsUrl = args[++i]; break;
                    case "-m": mongoString = args[++i]; break;
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
            Console.WriteLine("FairOS-dfs server url:");
            dfsUrl = ReadStringIfEmpty(dfsUrl);

            Console.WriteLine();
            Console.WriteLine("MongoDB connection string:");
            mongoString = ReadStringIfEmpty(mongoString);

            // Request sync config.
            Console.WriteLine();
            Console.WriteLine("Syncing database name:");
            databaseName = ReadStringIfEmpty(databaseName);

            // Start job.
            var dbSyncronizer = new DbSyncronizer(username, password, dfsUrl, mongoString, databaseName);
            await dbSyncronizer.StartAsync();
        }

        // Private helpers.
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
    }
}
