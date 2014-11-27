using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Syntaxlyn.Core
{
    class CSharpHtmlWalker : CSharpSyntaxWalker
    {
        public CSharpHtmlWalker(BuildContext ctx, SemanticModel semanticModel, TextWriter writer)
            : base(SyntaxWalkerDepth.StructuredTrivia)
        {
            this.impl = new WalkerImpl(ctx, semanticModel, writer);
        }

        private readonly WalkerImpl impl;

        public override void VisitToken(SyntaxToken token)
        {
            base.VisitLeadingTrivia(token);

            var kind = token.CSharpKind();
            if (token.IsKeyword() || SyntaxFacts.IsPreprocessorPunctuation(kind) || SyntaxFacts.IsPreprocessorKeyword(kind))
            {
                this.impl.WriteKeyword(token);
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
                        this.impl.WriteString(token);
                        break;
                    case SyntaxKind.IdentifierToken:
                        this.impl.WriteIdentifierToken(token);
                        break;
                    default:
                        this.impl.Write(token);
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
                switch (trivia.CSharpKind())
                {
                    case SyntaxKind.MultiLineCommentTrivia:
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                        this.impl.WriteComment(trivia);
                        break;
                    case SyntaxKind.DisabledTextTrivia:
                        this.impl.WriteDisabledText(trivia);
                        break;
                    default:
                        this.impl.Write(trivia);
                        break;
                }
            }
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            this.impl.VisitIdentifierName(node);
            base.VisitIdentifierName(node);
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            this.impl.VisitIdentifierName(node);
            base.VisitGenericName(node);
        }

        public override void DefaultVisit(SyntaxNode node)
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
                    this.impl.VisitTypeDeclaration(node);
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
                    this.impl.WriteDeclarationId(node);
                    isDecl = true;
                    break;
            }

            base.DefaultVisit(node);

            if (isDecl) this.impl.WriteEndDeclaration();
        }
    }
}
