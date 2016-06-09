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
            Console.WriteLine("--- get complete import ---" + Environment.NewLine);
            client.GetCompleteImport().Wait();
            Console.WriteLine("--- initial import completed ---" + Environment.NewLine);

            DateTime changesSince = new DateTime(2016, 05, 15);
            Console.WriteLine($"--- get changes since {changesSince} ---" + Environment.NewLine);
            client.GetChanges(changesSince).Wait();
            Console.WriteLine("--- import of changes completed ---" + Environment.NewLine);

            Console.ReadKey();
        }
    }
}
