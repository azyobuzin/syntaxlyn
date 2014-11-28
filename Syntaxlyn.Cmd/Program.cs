using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Syntaxlyn.Core;

namespace Syntaxlyn.Cmd
{
    static class Program
    {
        static void Main(string[] args)
        {
            try
            {
                File.Copy(
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "style.css"),
                    Path.Combine(outDir.FullName, "style.css"),
                    true
                );
                BuildContext.BuildAsync(args, CreateTextWriter, CreateLinkUri).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static readonly DirectoryInfo outDir = Directory.CreateDirectory("out");
        private const string HtmlExtension = ".html";

        static string GetOutDirPath(Document doc)
        {
            return string.Concat(Enumerable.Repeat("../", doc.Folders.Count + 1));
        }

        static async Task CreateTextWriter(Document doc, Func<TextWriter, Task> write)
        {
            Console.WriteLine(string.Join("/", doc.Folders.Concat(new[] { doc.Name })));

            var dir = outDir.CreateSubdirectory(doc.Project.Name);
            if (doc.Folders.Count > 0)
                dir = dir.CreateSubdirectory(string.Join(Path.DirectorySeparatorChar.ToString(), doc.Folders));

            using (var writer = new StreamWriter(Path.Combine(dir.FullName, doc.Name + HtmlExtension)))
            {
                await writer.WriteAsync(string.Format(@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<title>{0}</title>
<link rel=""stylesheet"" href=""{1}style.css"">
</head>
<body>
<pre>", WebUtility.HtmlEncode(doc.Name), GetOutDirPath(doc)));
                await write(writer);
                await writer.WriteAsync(@"</pre>
</body>
</html>");
            }
        }

        static async Task<string> CreateLinkUri(Document workingDoc, SyntaxReference syntaxRef, Document doc)
        {
            return "\{GetOutDirPath(workingDoc)}\{doc.Project.Name}/\{string.Join("/", doc.Folders.Concat(new[] { doc.Name }))}\{HtmlExtension}#\{(await syntaxRef.GetSyntaxAsync()).GetHashCode()}";
        }
    }
}
