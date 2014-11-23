using System.Diagnostics;

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
            catch
            {
                Debugger.Break();
            }
        }
    }
}
