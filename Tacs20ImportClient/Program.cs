using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tacs20ImportClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ApiClient client = new ApiClient();

            // Zuerst wird die Grundkonfiguration durch den tacsSuperUser erstellt.
            Console.WriteLine("--- get base configuration (full) ---" + Environment.NewLine);
            client.GetCompleteImport().Wait();
            Console.WriteLine("--- Hier erfolgt der erste Export und die Zuweisungen zu den Anstellungen ---");
            Console.WriteLine("--- Press any key to continue ---");
            Console.ReadKey();

            Console.WriteLine("--- Nun werden die Zuweisungen zu den Anstellungen importiert ---");
            client.GetEmploymentAssignments().Wait();
            Console.WriteLine("--- initial import completed ---" + Environment.NewLine);

            DateTime changesSince = new DateTime(2016, 05, 15);
            Console.WriteLine($"--- get changes since {changesSince} ---" + Environment.NewLine);
            client.GetChanges(changesSince).Wait();
            Console.WriteLine("--- import of changes completed ---" + Environment.NewLine);

            Console.ReadKey();
        }
    }
}
