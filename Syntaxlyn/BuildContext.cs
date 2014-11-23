using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Syntaxlyn
{
    class BuildContext
    {
        public BuildContext(string solutionFile)
        {
            this.solutionFile = solutionFile;
        }

        private readonly MSBuildWorkspace workspace = MSBuildWorkspace.Create();
        private readonly string solutionFile;
        public Solution Solution { get; private set; }

        public async Task Build()
        {
            this.Solution = await workspace.OpenSolutionAsync(this.solutionFile).ConfigureAwait(false);

            var outDir = Directory.CreateDirectory("out");
            await WriteCss(outDir);

            foreach (var proj in this.Solution.Projects)
            {
                var projDir = outDir.CreateSubdirectory(proj.Id.Id.ToString());
                foreach (var doc in proj.Documents)
                {
                    Console.WriteLine(doc.Name);
                    var semanticModel = await doc.GetSemanticModelAsync().ConfigureAwait(false);
                    using (var writer = new StreamWriter(Path.Combine(projDir.FullName, doc.Id.Id.ToString() + ".html")))
                    {
                        await writer.WriteAsync(@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<title>" + WebUtility.HtmlEncode(doc.Name) + @"</title>
<link rel=""stylesheet"" href=""../style.css"">
</head>
<body>
<pre>").ConfigureAwait(false);
                        new CSharpHtmlWalker(this, semanticModel, writer)
                            .Visit(await semanticModel.SyntaxTree.GetRootAsync().ConfigureAwait(false));
                        await writer.WriteAsync(@"</pre>
</body>
</html>").ConfigureAwait(false);
                    }
                }
            }
        }

        private static async Task WriteCss(DirectoryInfo dir)
        {
            using (var writer = new StreamWriter(Path.Combine(dir.FullName, "style.css")))
            {
                await writer.WriteAsync(@"pre {
    font-family: Consolas, メイリオ, monospace;
    font-size: 12pt;
    color: black;
}
a {
    color: black;
}
.keyword {
    color: blue;
}
.string {
    color: maroon;
}
.type, .type a {
    color: #2b91af !important;
}
.comment {
    color: green;
}
.disabled {
    color: gray;
}
").ConfigureAwait(false);
            }
        }
    }
}
