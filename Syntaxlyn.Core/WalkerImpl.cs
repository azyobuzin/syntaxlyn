using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Syntaxlyn.Core
{
    class WalkerImpl
    {
        internal WalkerImpl(BuildContext ctx, Document doc, SemanticModel semanticModel, TextWriter writer)
        {
            this.Context = ctx;
            this.Document = doc;
            this.SemanticModel = semanticModel;
            this.Writer = writer;
        }

        internal BuildContext Context { get; private set; }
        internal Document Document { get; private set; }
        internal SemanticModel SemanticModel { get; private set; }
        internal TextWriter Writer { get; private set; }

        private string startIdentifier;
        private string endIdentifier;

        internal async Task VisitIdentifierName(SyntaxNode node)
        {
            var symbol = this.SemanticModel.GetSymbolInfo(node).Symbol;
            if (symbol == null) return;

            var isVar = false;
            if (this.SemanticModel.Language == "C#")
            {
                var idNode = node as Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
                isVar = idNode != null && idNode.IsVar;
            }

            this.startIdentifier = string.Format(
                "<span class=\"{0}\" title=\"{1}\">",
                isVar ? "keyword"
                    : symbol.Kind == SymbolKind.NamedType || symbol.Kind == SymbolKind.TypeParameter || symbol.MetadataName == ".ctor"
                        ? "type"
                        : "identifier",
                WebUtility.HtmlEncode(
                    symbol.Kind == SymbolKind.NamedType
                        ? symbol.ToDisplayString()
                        : symbol.ToMinimalDisplayString(this.SemanticModel, node.SpanStart)
                )
            );
            this.endIdentifier = "</span>";

            if (symbol.Kind != SymbolKind.Namespace)
            {
                var syntaxRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
                var doc = this.Document.Project.Solution.GetDocument(syntaxRef?.SyntaxTree);
                if (doc != null)
                {
                    this.startIdentifier += "<a href=\"\{WebUtility.HtmlEncode(await this.Context.uriFactory(this.Document, syntaxRef, doc).ConfigureAwait(false))}\">";
                    this.endIdentifier = "</a>" + this.endIdentifier;
                }
            }
        }

        internal Task WriteRaw(string txt)
        {
            return this.Writer.WriteAsync(txt);
        }

        internal Task Write(string txt)
        {
            return this.WriteRaw(WebUtility.HtmlEncode(txt));
        }

        internal Task Write(SyntaxToken token)
        {
            return this.Write(token.Text);
        }

        internal Task Write(SyntaxTrivia trivia)
        {
            return this.Write(trivia.ToFullString());
        }

        private Task WriteEndSpan()
        {
            return this.WriteRaw("</span>");
        }

        internal Task WriteKeyword(string txt)
        {
            return this.WriteRaw("<span class=\"keyword\">\{WebUtility.HtmlEncode(txt)}</span>");
        }

        internal Task WriteKeyword(SyntaxToken token)
        {
            return this.WriteKeyword(token.Text);
        }

        internal Task WriteString(string txt)
        {
            return this.WriteRaw("<span class=\"string\">\{WebUtility.HtmlEncode(txt)}</span>");
        }

        internal Task WriteString(SyntaxToken token)
        {
            return this.WriteString(token.Text);
        }

        internal async Task WriteIdentifierToken(SyntaxToken token)
        {
            var idTag = this.startIdentifier != null;
            if (idTag) await this.WriteRaw(this.startIdentifier).ConfigureAwait(false);
            await this.Write(token.Text).ConfigureAwait(false);
            if (idTag)
            {
                await this.WriteRaw(this.endIdentifier).ConfigureAwait(false);
                this.startIdentifier = null;
                this.endIdentifier = null;
            }
        }

        internal Task WriteComment(string txt)
        {
            return this.WriteRaw("<span class=\"comment\">\{WebUtility.HtmlEncode(txt)}</span>");
        }

        internal Task WriteComment(SyntaxToken token)
        {
            return this.WriteComment(token.Text);
        }

        internal Task WriteComment(SyntaxTrivia trivia)
        {
            return this.WriteComment(trivia.ToFullString());
        }

        internal Task WriteDisabledText(string txt)
        {
            return this.WriteRaw("<span class=\"disabled\">\{WebUtility.HtmlEncode(txt)}</span>");
        }

        internal Task WriteDisabledText(SyntaxTrivia trivia)
        {
            return this.WriteDisabledText(trivia.ToFullString());
        }

        internal Task WriteDeclarationId(SyntaxNode node)
        {
            return this.WriteRaw("<span id=\"\{node.GetHashCode()}\">");
        }

        internal Task VisitTypeDeclaration(SyntaxNode node)
        {
            this.startIdentifier = @"<span class=""type"">";
            this.endIdentifier = "</span>";

            return this.WriteDeclarationId(node);
        }

        internal Task WriteEndDeclaration()
        {
            return this.WriteEndSpan();
        }

        internal Task WriteStartXmlComment()
        {
            return this.WriteRaw(@"<span class=""xmlcomment"">");
        }

        internal Task WriteEndXmlComment()
        {
            return this.WriteEndSpan();
        }

        internal Task WriteStartXmlIdentifier()
        {
            return this.WriteRaw(@"<span class=""identifier"">");
        }

        internal Task WriteEndXmlIdentifier()
        {
            return this.WriteEndSpan();
        }
    }
}
