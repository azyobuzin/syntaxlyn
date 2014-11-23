using System.IO;
using System.Linq;
using System.Net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Syntaxlyn
{
    class VisualBasicHtmlWalker : VisualBasicSyntaxWalker
    {
        public VisualBasicHtmlWalker(BuildContext ctx, SemanticModel semanticModel, TextWriter writer)
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
            var kind = token.VBKind();
            if (token.IsKeyword() || SyntaxFacts.IsPreprocessorPunctuation(kind) || token.IsPreprocessorKeyword())
            {
                this.writer.Write("<span class=\"keyword\">\{txt}</span>");
            }
            else
            {
                switch (kind)
                {
                    case SyntaxKind.StringLiteralToken:
                    case SyntaxKind.CharacterLiteralToken:
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
                switch (trivia.VBKind())
                {
                    case SyntaxKind.CommentTrivia:
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

        private void SetStartLink(ExpressionSyntax node) //TODO: C# と統合
        {
            var symbol = this.semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol != null && symbol.Kind != SymbolKind.Namespace)
            {
                var syntaxRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
                var doc = this.ctx.Workspace.CurrentSolution.GetDocument(syntaxRef?.SyntaxTree);
                if (doc != null)
                {
                    System.Diagnostics.Debug.WriteLine(syntaxRef.GetSyntax().GetType());
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
            switch (node.VBKind())
            {
                case SyntaxKind.ClassStatement:
                case SyntaxKind.CatchStatement:
                case SyntaxKind.DeclareFunctionStatement:
                case SyntaxKind.DeclareSubStatement:
                case SyntaxKind.DelegateFunctionStatement:
                case SyntaxKind.DelegateSubStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.InterfaceStatement:
                case SyntaxKind.ModuleStatement:
                case SyntaxKind.OperatorStatement:
                case SyntaxKind.PropertyStatement:
                case SyntaxKind.StructureStatement:
                case SyntaxKind.SubNewStatement:
                case SyntaxKind.SubStatement:
                case SyntaxKind.UsingStatement:
                case SyntaxKind.EnumMemberDeclaration:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.VariableDeclarator:
                case SyntaxKind.Parameter:
                case SyntaxKind.TypeParameter:
                case SyntaxKind.ModifiedIdentifier:
                    this.writer.Write("<span id=\"\{node.GetHashCode()}\">");
                    isDecl = true;
                    break;
            }

            base.DefaultVisit(node);

            if (isDecl) this.writer.Write("</span>");
        }
    }
}
