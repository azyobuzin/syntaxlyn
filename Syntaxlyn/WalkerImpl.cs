using System.IO;
using System.Linq;
using System.Net;
using Microsoft.CodeAnalysis;

namespace Syntaxlyn
{
    class WalkerImpl
    {
        internal WalkerImpl(BuildContext ctx, SemanticModel semanticModel, TextWriter writer)
        {
            this.Context = ctx;
            this.SemanticModel = semanticModel;
            this.Writer = writer;
        }

        internal BuildContext Context { get; private set; }
        internal SemanticModel SemanticModel { get; private set; }
        internal TextWriter Writer { get; private set; }

        private string startLink;

        internal void SetStartLink(SyntaxNode node)
        {
            var symbol = this.SemanticModel.GetSymbolInfo(node).Symbol;
            if (symbol != null && symbol.Kind != SymbolKind.Namespace)
            {
                var syntaxRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
                var doc = this.Context.Workspace.CurrentSolution.GetDocument(syntaxRef?.SyntaxTree);
                if (doc != null)
                {
                    this.startLink = "<a \{symbol.Kind == SymbolKind.NamedType ? "class=\"type\" " : ""}href =\"../\{doc.Id.ProjectId.Id}/\{doc.Id.Id}.html#\{syntaxRef.GetSyntax().GetHashCode()}\">";
                }
            }
        }

        internal void WriteRaw(string txt)
        {
            this.Writer.Write(txt);
        }

        internal void Write(string txt)
        {
            this.WriteRaw(WebUtility.HtmlEncode(txt));
        }

        internal void Write(SyntaxToken token)
        {
            this.Write(token.Text);
        }

        internal void Write(SyntaxTrivia trivia)
        {
            this.Write(trivia.ToFullString());
        }

        internal void WriteKeyword(string txt)
        {
            this.WriteRaw("<span class=\"keyword\">\{WebUtility.HtmlEncode(txt)}</span>");
        }

        internal void WriteKeyword(SyntaxToken token)
        {
            this.WriteKeyword(token.Text);
        }

        internal void WriteString(string txt)
        {
            this.WriteRaw("<span class=\"string\">\{WebUtility.HtmlEncode(txt)}</span>");
        }

        internal void WriteString(SyntaxToken token)
        {
            this.WriteString(token.Text);
        }

        internal void WriteIdentifierToken(SyntaxToken token)
        {
            var isLink = this.startLink != null;
            if (isLink) this.WriteRaw(this.startLink);
            this.Write(token.Text);
            if (isLink)
            {
                this.WriteRaw("</a>");
                this.startLink = null;
            }
        }

        internal void WriteComment(string txt)
        {
            this.WriteRaw("<span class=\"comment\">\{WebUtility.HtmlEncode(txt)}</span>");
        }

        internal void WriteComment(SyntaxTrivia trivia)
        {
            this.WriteComment(trivia.ToFullString());
        }

        internal void WriteDisabledText(string txt)
        {
            this.WriteRaw("<span class=\"disabled\">\{WebUtility.HtmlEncode(txt)}</span>");
        }

        internal void WriteDisabledText(SyntaxTrivia trivia)
        {
            this.WriteDisabledText(trivia.ToFullString());
        }

        internal void WriteDeclarationId(SyntaxNode node)
        {
            this.WriteRaw("<span id=\"\{node.GetHashCode()}\">");
        }

        internal void WriteEndDeclaration()
        {
            this.WriteRaw("</span>");
        }
    }
}
