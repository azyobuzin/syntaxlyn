using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Syntaxlyn.Core
{
    abstract class CSharpAsyncSyntaxWalker : CSharpSyntaxVisitor<Task>
    {
        public abstract Task VisitToken(SyntaxToken token);

        public abstract Task VisitTrivia(SyntaxTrivia trivia);

        public async Task VisitLeadingTrivia(SyntaxToken token)
        {
            foreach (var t in token.LeadingTrivia)
                await this.VisitTrivia(t).ConfigureAwait(false);
        }

        public async Task VisitTrailingTrivia(SyntaxToken token)
        {
            foreach (var t in token.TrailingTrivia)
                await this.VisitTrivia(t).ConfigureAwait(false);
        }

        public override async Task DefaultVisit(SyntaxNode node)
        {
            foreach (var child in node.ChildNodesAndTokens())
                await (child.IsNode ? this.Visit(child.AsNode()) : this.VisitToken(child.AsToken())).ConfigureAwait(false);
        }
    }

    class CSharpHtmlWalker : CSharpAsyncSyntaxWalker
    {
        public CSharpHtmlWalker(BuildContext ctx, Document doc, SemanticModel semanticModel, TextWriter writer)
        {
            this.impl = new WalkerImpl(ctx, doc, semanticModel, writer);
        }

        private readonly WalkerImpl impl;

        public override async Task VisitToken(SyntaxToken token)
        {
            await this.VisitLeadingTrivia(token).ConfigureAwait(false);

            var kind = token.CSharpKind();
            if (token.IsKeyword() || SyntaxFacts.IsPreprocessorPunctuation(kind) || SyntaxFacts.IsPreprocessorKeyword(kind))
            {
                await this.impl.WriteKeyword(token).ConfigureAwait(false);
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
                        await this.impl.WriteString(token).ConfigureAwait(false);
                        break;
                    case SyntaxKind.IdentifierToken:
                        await this.impl.WriteIdentifierToken(token).ConfigureAwait(false);
                        break;
                    default:
                        await this.impl.Write(token).ConfigureAwait(false);
                        break;
                }
            }

            await this.VisitTrailingTrivia(token).ConfigureAwait(false);
        }

        public override Task VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.IsDirective)
            {
                return this.Visit(trivia.GetStructure());
            }
            else
            {
                switch (trivia.CSharpKind())
                {
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                        return this.impl.WriteComment(trivia);
                    case SyntaxKind.DisabledTextTrivia:
                        return this.impl.WriteDisabledText(trivia);
                    default:
                        return this.impl.Write(trivia);
                }
            }
        }

        public override async Task VisitIdentifierName(IdentifierNameSyntax node)
        {
            await this.impl.VisitIdentifierName(node).ConfigureAwait(false);
            await base.VisitIdentifierName(node).ConfigureAwait(false);
        }

        public override async Task VisitGenericName(GenericNameSyntax node)
        {
            await this.impl.VisitIdentifierName(node).ConfigureAwait(false);
            await base.VisitGenericName(node).ConfigureAwait(false);
        }

        public override async Task DefaultVisit(SyntaxNode node)
        {
            var isDecl = false;
            switch (node.CSharpKind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.TypeParameter:
                    await this.impl.VisitTypeDeclaration(node).ConfigureAwait(false);
                    isDecl = true;
                    break;
                case SyntaxKind.CatchDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.EnumMemberDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.VariableDeclaration:
                case SyntaxKind.Parameter:
                case SyntaxKind.VariableDeclarator:
                case SyntaxKind.FromClause:
                    await this.impl.WriteDeclarationId(node).ConfigureAwait(false);
                    isDecl = true;
                    break;
            }

            await base.DefaultVisit(node).ConfigureAwait(false);

            if (isDecl) await this.impl.WriteEndDeclaration().ConfigureAwait(false);
        }
    }
}
