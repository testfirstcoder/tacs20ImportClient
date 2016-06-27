using System;

namespace Tacs20ImportClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ApiClient client = new ApiClient();

            // Zuerst wird die Grundkonfiguration durch den tacsSuperUser erstellt.
            Console.WriteLine("--- Grundkonfiguration abholen und speichern ---" + Environment.NewLine);
            client.GetCompleteImport().Wait();
            Console.WriteLine("--- Hier erfolgt der erste Export und die Zuweisungen zu den Anstellungen ---");
            Console.WriteLine("--- Press any key to continue ---");
            Console.ReadKey();
            Console.WriteLine();

            // Nach dem ersten Export können Variablen etc. einzelnen Anstellungen zugewiesen werden.
            // Diese werden als nächstes importiert.
            Console.WriteLine("--- Nun werden die Zuweisungen zu den Anstellungen importiert ---");
            client.GetEmploymentAssignments().Wait();
            Console.WriteLine("--- Grundkonfiguration ist nun importiert ---");
            Console.WriteLine("--- Ab jetzt müssen nur noch die Änderungen importiert werden ---");
            Console.WriteLine("--- Press any key to continue ---");
            Console.ReadKey();
            Console.WriteLine();

            // Sobald die Grundkonfiguration importiert wurde, müssen nur noch die Änderungen seit dem letzten 
            // Import abgeholt werden.
            DateTime changesSince = new DateTime(2016, 05, 15);
            Console.WriteLine($"--- Import der Änderungen seit {changesSince} ---" + Environment.NewLine);
            client.GetChanges(changesSince).Wait();
            Console.WriteLine("--- Import der Änderungen beendet ---" + Environment.NewLine);

            // Das changesSince-Datum für den nächsten Import auf "heute" setzen.
            changesSince = DateTime.Today;
            Console.ReadKey();
        }
    }
}
