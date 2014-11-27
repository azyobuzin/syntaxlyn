using System;
using Syntaxlyn.Core;

namespace Syntaxlyn.Cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var ctx = new BuildContext(args);
                ctx.StartedParsingDocument += (sender, e) => Console.WriteLine(e.Document.Name);
                ctx.Build().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
