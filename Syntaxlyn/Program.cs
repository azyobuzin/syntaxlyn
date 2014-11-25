using System;

namespace Syntaxlyn
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new BuildContext(args).Build().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
