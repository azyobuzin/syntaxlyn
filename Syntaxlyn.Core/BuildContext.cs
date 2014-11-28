using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Syntaxlyn.Core
{
    public delegate Task WriterFactory(Document doc, Func<TextWriter, Task> write);
    public delegate Task<string> UriFactory(Document workingDoc, SyntaxReference syntaxRef, Document doc);

    public class BuildContext
    {
        private BuildContext() { }

        internal MSBuildWorkspace Workspace { get; } = MSBuildWorkspace.Create();
        private IEnumerable<string> files;

        private WriterFactory writerFactory;
        internal UriFactory uriFactory;

        public static Task BuildAsync(IEnumerable<string> files, WriterFactory writerFactory, UriFactory uriFactory)
        {
            if (writerFactory == null) throw new ArgumentNullException("writerFactory");
            if (uriFactory == null) throw new ArgumentNullException("uriFactory");

            return new BuildContext() { files = files, writerFactory = writerFactory, uriFactory = uriFactory }.BuildAsync();
        }

        private async Task BuildAsync()
        {
            foreach (var file in this.files)
            {
                if (Path.GetExtension(file).ToLowerInvariant() == ".sln")
                {
                    var solution = await this.Workspace.OpenSolutionAsync(file).ConfigureAwait(false);
                    await Task.WhenAll(solution.Projects.Select(this.BuildProjectAsync)).ConfigureAwait(false);
                }
                else
                {
                    await this.BuildProjectAsync(await this.Workspace.OpenProjectAsync(file).ConfigureAwait(false));
                }
            }
        }

        private async Task BuildProjectAsync(Project proj)
        {
            var isCSharp = false;
            switch (proj.Language)
            {
                case "C#":
                    isCSharp = true;
                    break;
                case "Visual Basic":
                    break;
                default:
                    return;
            }

            await Task.WhenAll(proj.Documents
                .Where(doc => doc.SupportsSemanticModel)
                .Select(async doc =>
                {
                    var semanticModel = await doc.GetSemanticModelAsync().ConfigureAwait(false);

                    var root = await semanticModel.SyntaxTree.GetRootAsync().ConfigureAwait(false);
                    await this.writerFactory(doc, writer => isCSharp
                        ? new CSharpHtmlWalker(this, doc, semanticModel, writer).Visit(root)
                        : new VisualBasicHtmlWalker(this, doc, semanticModel, writer).Visit(root)
                    ).ConfigureAwait(false);
                })
            );
        }
    }
}
