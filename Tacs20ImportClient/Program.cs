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
            client.GetCompleteImport().Wait();

            Console.ReadLine();
        }
    }
}
