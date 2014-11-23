using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Syntaxlyn
{
    public class BuildContext
    {
        public BuildContext(string[] files)
        {
            this.files = files;
        }

        public MSBuildWorkspace Workspace { get; } = MSBuildWorkspace.Create();
        private readonly string[] files;

        public async Task Build()
        {
            var outDir = Directory.CreateDirectory("out");
            await WriteCss(outDir);

            foreach (var file in files)
            {
                if (Path.GetExtension(file).ToLowerInvariant() == ".sln")
                {
                    var solution = await this.Workspace.OpenSolutionAsync(file);
                    foreach (var proj in solution.Projects)
                        await BuildProject(outDir, proj);
                }
                else
                {
                    await BuildProject(outDir, await this.Workspace.OpenProjectAsync(file));
                }
            }
        }

        private async Task BuildProject(DirectoryInfo outDir, Project proj)
        {
            var projDir = outDir.CreateSubdirectory(proj.Id.Id.ToString());
            foreach (var doc in proj.Documents)
            {
                Console.WriteLine(doc.Name);
                var semanticModel = await doc.GetSemanticModelAsync().ConfigureAwait(false);
                var root = await semanticModel.SyntaxTree.GetRootAsync().ConfigureAwait(false);
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
                    switch (proj.Language)
                    {
                        case "C#":
                            new CSharpHtmlWalker(this, semanticModel, writer).Visit(root);
                            break;
                        case "Visual Basic":
                            new VisualBasicHtmlWalker(this, semanticModel, writer).Visit(root);
                            break;
                        default:
                            throw new NotSupportedException(proj.Language);
                    }
                    await writer.WriteAsync(@"</pre>
</body>
</html>").ConfigureAwait(false);
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
