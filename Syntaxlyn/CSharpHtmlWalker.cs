using System.IO;
using System.Linq;
using System.Net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Syntaxlyn
{
    class CSharpHtmlWalker : CSharpSyntaxWalker
    {
        public CSharpHtmlWalker(BuildContext ctx, SemanticModel semanticModel, TextWriter writer)
            : base(SyntaxWalkerDepth.StructuredTrivia)
        {
            this.ctx = ctx;
            this.semanticModel = semanticModel;
            this.writer = writer;
        }

        private readonly BuildContext ctx;
        private readonly SemanticModel semanticModel;
        private readonly TextWriter writer;

        private string startLink;

        public override void VisitToken(SyntaxToken token)
        {
            base.VisitLeadingTrivia(token);

            var txt = WebUtility.HtmlEncode(token.Text);
            var kind = token.CSharpKind();
            if (token.IsKeyword() || SyntaxFacts.IsPreprocessorPunctuation(kind) || SyntaxFacts.IsPreprocessorKeyword(kind))
            {
                this.writer.Write("<span class=\"keyword\">\{txt}</span>");
            }
            else
            {
                switch (kind)
                {
                    case SyntaxKind.StringLiteralToken:
                    case SyntaxKind.CharacterLiteralToken:
                    case SyntaxKind.InterpolatedStringStartToken:
                    case SyntaxKind.InterpolatedStringMidToken:
                    case SyntaxKind.InterpolatedStringEndToken:
                        this.writer.Write("<span class=\"string\">\{txt}</span>");
                        break;
                    case SyntaxKind.IdentifierToken:
                        var isLink = startLink != null;
                        if (isLink) this.writer.Write(startLink);
                        this.writer.Write(txt);
                        if (isLink) this.writer.Write("</a>");
                        startLink = null;
                        break;
                    default:
                        this.writer.Write(txt);
                        break;
                }
            }

            base.VisitTrailingTrivia(token);
        }

        public override void VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.IsDirective)
            {
                this.Visit(trivia.GetStructure());
            }
            else
            {
                var txt = WebUtility.HtmlEncode(trivia.ToFullString());
                switch (trivia.CSharpKind())
                {
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                        this.writer.Write("<span class=\"comment\">\{txt}</span>");
                        break;
                    case SyntaxKind.DisabledTextTrivia:
                        this.writer.Write("<span class=\"disabled\">\{txt}</span>");
                        break;
                    default:
                        this.writer.Write(txt);
                        break;
                }
            }
        }

        private void SetStartLink(ExpressionSyntax node)
        {
            var symbol = this.semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol != null && symbol.Kind != SymbolKind.Namespace)
            {
                var syntaxRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
                var doc = this.ctx.Workspace.CurrentSolution.GetDocument(syntaxRef?.SyntaxTree);
                if (doc != null)
                {
                    this.startLink = "<a \{symbol.Kind == SymbolKind.NamedType ? "class=\"type\" " : ""}href =\"../\{doc.Id.ProjectId.Id}/\{doc.Id.Id}.html#\{syntaxRef.GetSyntax().GetHashCode()}\">";
                }
            }
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.SetStartLink(node);
            base.VisitIdentifierName(node);
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            this.SetStartLink(node);
            base.VisitGenericName(node);
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            var isDecl = false;
            switch (node.CSharpKind())
            {
                case SyntaxKind.CatchDeclaration:
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.EnumMemberDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.VariableDeclaration:
                case SyntaxKind.Parameter:
                case SyntaxKind.TypeParameter:
                case SyntaxKind.VariableDeclarator:
                    this.writer.Write("<span id=\"\{node.GetHashCode()}\">");
                    isDecl = true;
                    break;
            }

            base.DefaultVisit(node);

            if (isDecl) this.writer.Write("</span>");
        }
    }
}
