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

        public override async Task VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.IsDirective)
            {
                await this.Visit(trivia.GetStructure()).ConfigureAwait(false);
            }
            else
            {
                switch (trivia.CSharpKind())
                {
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.SingleLineCommentTrivia:
                        await this.impl.WriteComment(trivia).ConfigureAwait(false);
                        break;
                    case SyntaxKind.DisabledTextTrivia:
                        await this.impl.WriteDisabledText(trivia).ConfigureAwait(false);
                        break;
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                        await this.impl.WriteStartXmlComment().ConfigureAwait(false);
                        await this.Visit(trivia.GetStructure()).ConfigureAwait(false);
                        await this.impl.WriteEndXmlComment().ConfigureAwait(false);
                        break;
                    default:
                        await this.impl.Write(trivia).ConfigureAwait(false);
                        break;
                }
            }
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
                case SyntaxKind.IdentifierName:
                case SyntaxKind.GenericName:
                    await this.impl.VisitIdentifierName(node).ConfigureAwait(false);
                    break;
            }

            await base.DefaultVisit(node).ConfigureAwait(false);

            if (isDecl) await this.impl.WriteEndDeclaration().ConfigureAwait(false);
        }

        public override async Task VisitQualifiedCref(QualifiedCrefSyntax node)
        {
            await this.impl.WriteStartXmlIdentifier().ConfigureAwait(false);
            await base.VisitQualifiedCref(node).ConfigureAwait(false);
            await this.impl.WriteEndXmlIdentifier().ConfigureAwait(false);
        }

        public override Task VisitXmlName(XmlNameSyntax node)
        {
            return this.impl.Write(node.ToFullString());
        }

        public override async Task VisitXmlText(XmlTextSyntax node)
        {
            foreach (var t in node.ChildTokens())
            {
                if (t.CSharpKind() == SyntaxKind.XmlTextLiteralToken)
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
