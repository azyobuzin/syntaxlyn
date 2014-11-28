using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Syntaxlyn.Core
{
    abstract class VisualBasicAsyncSyntaxWalker : VisualBasicSyntaxVisitor<Task>
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

    class VisualBasicHtmlWalker : VisualBasicAsyncSyntaxWalker
    {
        public VisualBasicHtmlWalker(BuildContext ctx, Document doc, SemanticModel semanticModel, TextWriter writer)
        {
            this.impl = new WalkerImpl(ctx, doc, semanticModel, writer);
        }

        private readonly WalkerImpl impl;

        public override async Task VisitToken(SyntaxToken token)
        {
            await this.VisitLeadingTrivia(token).ConfigureAwait(false);

            var kind = token.VBKind();
            if (token.IsKeyword() || SyntaxFacts.IsPreprocessorPunctuation(kind) || token.IsPreprocessorKeyword())
            {
                await this.impl.WriteKeyword(token).ConfigureAwait(false);
            }
            else
            {
                switch (kind)
                {
                    case SyntaxKind.StringLiteralToken:
                    case SyntaxKind.CharacterLiteralToken:
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
                switch (trivia.VBKind())
                {
                    case SyntaxKind.CommentTrivia:
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
            switch (node.VBKind())
            {
                case SyntaxKind.ClassStatement:
                case SyntaxKind.EnumStatement:
                case SyntaxKind.DelegateFunctionStatement:
                case SyntaxKind.DelegateSubStatement:
                case SyntaxKind.InterfaceStatement:
                case SyntaxKind.ModuleStatement:
                case SyntaxKind.StructureStatement:
                case SyntaxKind.TypeParameter:
                    await this.impl.VisitTypeDeclaration(node).ConfigureAwait(false);
                    isDecl = true;
                    break;
                case SyntaxKind.CatchStatement:
                case SyntaxKind.DeclareFunctionStatement:
                case SyntaxKind.DeclareSubStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.OperatorStatement:
                case SyntaxKind.PropertyStatement:
                case SyntaxKind.SubNewStatement:
                case SyntaxKind.SubStatement:
                case SyntaxKind.UsingStatement:
                case SyntaxKind.EnumMemberDeclaration:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.VariableDeclarator:
                case SyntaxKind.Parameter:
                case SyntaxKind.ModifiedIdentifier:
                    await this.impl.WriteDeclarationId(node).ConfigureAwait(false);
                    isDecl = true;
                    break;
            }

            await base.DefaultVisit(node).ConfigureAwait(false);

            if (isDecl) await this.impl.WriteEndDeclaration().ConfigureAwait(false);
        }
    }
}
