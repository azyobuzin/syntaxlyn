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
        public VisualBasicHtmlWalker(SyntaxlynBuilder ctx, Document doc, SemanticModel semanticModel, TextWriter writer)
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

        public override async Task VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.IsDirective)
            {
                await this.Visit(trivia.GetStructure());
            }
            else
            {
                switch (trivia.VBKind())
                {
                    case SyntaxKind.CommentTrivia:
                        await this.impl.WriteComment(trivia);
                        break;
                    case SyntaxKind.DisabledTextTrivia:
                        await this.impl.WriteDisabledText(trivia);
                        break;
                    case SyntaxKind.DocumentationCommentTrivia:
                        await this.impl.WriteStartXmlComment().ConfigureAwait(false);
                        await this.Visit(trivia.GetStructure()).ConfigureAwait(false);
                        await this.impl.WriteEndXmlComment().ConfigureAwait(false);
                        break;
                    default:
                        await this.impl.Write(trivia);
                        break;
                }
            }
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
                case SyntaxKind.FunctionStatement:
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
                case SyntaxKind.IdentifierName:
                case SyntaxKind.GenericName:
                    await this.impl.VisitIdentifierName(node).ConfigureAwait(false);
                    break;
            }

            await base.DefaultVisit(node).ConfigureAwait(false);

            if (isDecl) await this.impl.WriteEndDeclaration().ConfigureAwait(false);
        }

        public override async Task VisitCrefReference(CrefReferenceSyntax node)
        {
            await this.impl.WriteStartXmlIdentifier().ConfigureAwait(false);
            await base.VisitCrefReference(node).ConfigureAwait(false);
            await this.impl.WriteEndXmlIdentifier().ConfigureAwait(false);
        }

        public override async Task VisitXmlText(XmlTextSyntax node)
        {
            foreach (var t in node.ChildTokens())
            {
                if (t.VBKind() == SyntaxKind.XmlTextLiteralToken)
                {
                    await this.VisitLeadingTrivia(t).ConfigureAwait(false);
                    await this.impl.WriteComment(t).ConfigureAwait(false);
                    await this.VisitTrailingTrivia(t).ConfigureAwait(false);
                }
                else
                {
                    await this.VisitToken(t);
                }
            }
        }
    }
}
