using System.Diagnostics;

namespace Syntaxlyn
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new BuildContext(args[0]).Build().Wait();
            }
            catch
            {
                Debugger.Break();
            }
        }
    }
}
