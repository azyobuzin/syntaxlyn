using System.IO;
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
            this.impl = new WalkerImpl(ctx, semanticModel, writer);
        }

        private readonly WalkerImpl impl;

        public override void VisitToken(SyntaxToken token)
        {
            base.VisitLeadingTrivia(token);

            var kind = token.VBKind();
            if (token.IsKeyword() || SyntaxFacts.IsPreprocessorPunctuation(kind) || token.IsPreprocessorKeyword())
            {
                this.impl.WriteKeyword(token);
            }
            else
            {
                switch (kind)
                {
                    case SyntaxKind.StringLiteralToken:
                    case SyntaxKind.CharacterLiteralToken:
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
                switch (trivia.VBKind())
                {
                    case SyntaxKind.CommentTrivia:
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
                    this.impl.WriteDeclarationId(node);
                    isDecl = true;
                    break;
            }

            base.DefaultVisit(node);

            if (isDecl) this.impl.WriteEndDeclaration();
        }
    }
}
