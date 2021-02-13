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
            "add <n.documents> <collection>\t- add n. new random documents\n" +
            "status\t\t\t\t- print current sync status of mongo and dfs\n" +
            "exit\t\t\t\t- exit the application\n" +
            "help\t\t\t\t- print this help\n";

        // Fields.
        private string username;
        private string password;
        private string dfsUrl;
        private string mongoString;
        private string databaseName;
        private readonly AddCommandRunner addCommandRunner;

        // Constructor.
        public DataConsole(string username, string password, string dfsUrl, string mongoString, string databaseName)
        {
            this.username = username;
            this.password = password;
            this.dfsUrl = dfsUrl;
            this.mongoString = mongoString;
            this.databaseName = databaseName;

            addCommandRunner = new AddCommandRunner(mongoString, databaseName);
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
                    case "add": await AddCommandRunner.RunAsync(commandSegments, mongoString, databaseName); break;
                    case "status": await StatusCommandRunner.RunAsync(username, password, dfsUrl, mongoString, databaseName); break;
                    case "exit": exit = true; break;
                    case "help": Console.Write(HelpText); break;
                    case "": break;
                    default: Console.WriteLine("Invalid command"); break;
                }
            }
        }
    }
}
