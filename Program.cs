using System;
using DittoSDK;
using System.Collections.Generic;
using System.Linq;

namespace Tasks
{
    class Program
    {
        static Ditto ditto;
        static bool isAskedToExit = false;
        static List<Task> tasks = new List<Task>();

        public static async System.Threading.Tasks.Task Main(params string[] args)
        {
            ditto = new Ditto(identity: DittoIdentity.OnlinePlayground("REPLACE_ME_WITH_YOUR_APP_ID", "REPLACE_ME_WITH_YOUR_PLAYGROUND_TOKEN"));

            try
            {
                ditto.StartSync();
            }
            catch (DittoException ex)
            {
                Console.WriteLine("There was an error starting Ditto.");
                Console.WriteLine("Here's the following error");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Ditto cannot start sync but don't worry.");
                Console.WriteLine("Ditto will still work as a local database.");
            }

            Console.WriteLine("Welcome to Ditto's Task App\n");

            string query = "SELECT * FROM tasks WHERE isDeleted == false";

            ditto.Sync.RegisterSubscription(query);

            ditto.Store.RegisterObserver(query, (result) =>
            {
                tasks = result.Items.ConvertAll(item => Task.JsonToTask(item.JsonString()));
            });

            await ditto.Store.ExecuteAsync("EVICT FROM tasks WHERE isDeleted == true");

            ListCommands();


            while (!isAskedToExit)
            {

                Console.Write("\nYour command: ");
                string command = Console.ReadLine();

                switch (command)
                {
                    case string s when command.StartsWith("--insert"):
                        string taskBody = s.Replace("--insert ", "");
                        var task = new Task(taskBody, false).ToDictionary();
                        await ditto.Store.ExecuteAsync("INSERT INTO tasks DOCUMENTS (:task)", new Dictionary<string, object> { { "task", task } });
                        break;

                    case string s when command.StartsWith("--toggle"):
                        string _idToToggle = s.Replace("--toggle ", "");
                        var isCompleted = tasks.First(t => t._id == _idToToggle).isCompleted;
                        await ditto.Store.ExecuteAsync(
                            "UPDATE tasks " +
                            "SET isCompleted = :newValue " +
                            "WHERE _id == :id",
                            new Dictionary<string, object> {{ "newValue", !isCompleted }, { "id", _idToToggle }});
                        break;

                    case string s when command.StartsWith("--delete"):
                        string _idToDelete = s.Replace("--delete ", "");
                        var isDeleted = tasks.First(t => t._id == _idToDelete).isDeleted;
                        await ditto.Store.ExecuteAsync(
                            "UPDATE tasks " +
                            "SET isDeleted = :newValue " +
                            "WHERE _id == :id",
                            new Dictionary<string, object> {{ "newValue", !isDeleted }, { "id", _idToDelete }});
                        break;

                    case { } when command.StartsWith("--list"):
                        tasks.ForEach(task =>
                        {
                            Console.WriteLine(task.ToString());
                        });
                        break;

                    case { } when command.StartsWith("--exit"):
                        Console.WriteLine("Good bye!");
                        isAskedToExit = true;
                        break;

                    default:
                        Console.WriteLine("Unknown command");
                        ListCommands();
                        break;

                }
            }
        }

        public static void ListCommands()
        {
            Console.WriteLine("************* Available Commands *************");
            Console.WriteLine("--insert my new task");
            Console.WriteLine("   insert a task");
            Console.WriteLine("   Example: \"--insert Get Milk\"");
            Console.WriteLine("--toggle myTaskTd");
            Console.WriteLine("   Toggles the isComplete property to the opposite value");
            Console.WriteLine("   Example: \"--toggle 1234abc\"");
            Console.WriteLine("--delete myTaskTd");
            Console.WriteLine("   Deletes a task");
            Console.WriteLine("   Example: \"--delete 1234abc\"");
            Console.WriteLine("--list");
            Console.WriteLine("   List the current tasks");
            Console.WriteLine("--exit");
            Console.WriteLine("   Exits the program");
            Console.WriteLine("************* Commands *************");
        }
    }
}
