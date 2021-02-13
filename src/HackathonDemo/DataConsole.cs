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

using Etherna.HackathonDemo.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Etherna.HackathonDemo
{
    class DataConsole
    {
        // Consts.
        private const string HelpText =
            "Demo console commands:\n" +
            "insert <n.documents> <collection>\t- add n. new random documents\n" +
            "delete <n.documents> <collection>\t- delete n. random documents\n" +
            "status\t\t\t\t\t- print current sync status of mongo and dfs\n" +
            "exit\t\t\t\t\t- exit the application\n" +
            "help\t\t\t\t\t- print this help\n";

        // Fields.
        private readonly InsertCommandRunner insertCommandRunner;
        private readonly DeleteCommandRunner deleteCommandRunner;
        private readonly StatusCommandRunner statusCommandRunner;
        private readonly string username;
        private readonly string password;

        // Constructor.
        public DataConsole(string username, string password, string dfsUrl, string mongoString, string databaseName)
        {
            insertCommandRunner = new InsertCommandRunner(mongoString, databaseName);
            deleteCommandRunner = new DeleteCommandRunner(mongoString, databaseName);
            statusCommandRunner = new StatusCommandRunner(dfsUrl, mongoString, databaseName);
            this.username = username;
            this.password = password;
        }

        // Methods.
        public async Task StartAsync()
        {
            var exit = false;

            Console.Clear();
            Console.Write(HelpText);

            while (!exit)
            {
                Console.Write("> ");
                var command = Console.ReadLine();

                var commandSegments = command!.Split(' ');
                switch (commandSegments.First().ToLowerInvariant())
                {
                    case "insert": await insertCommandRunner.RunAsync(commandSegments); break;
                    case "delete": await deleteCommandRunner.RunAsync(commandSegments); break;
                    case "status": await statusCommandRunner.RunAsync(username, password); break;
                    case "exit": exit = true; break;
                    case "help": Console.Write(HelpText); break;
                    case "": break;
                    default: Console.WriteLine("Invalid command"); break;
                }
            }
        }
    }
}
