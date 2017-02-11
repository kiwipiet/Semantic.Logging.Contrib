using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SemanticLogging.Contrib.Tests;

namespace SemanticLogging.Contrib.Generator
{
    class Program
    {
        static void Main(string[] args)
        {

            while (true)
            {
                Console.WriteLine(DateTime.Now);
                MyCompanyContribEventSource.Log.Failure(new Exception().ToString());
                Thread.Sleep(1000);
            }
        }
    }
}
