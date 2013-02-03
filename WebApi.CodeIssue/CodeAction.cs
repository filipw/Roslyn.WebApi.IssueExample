using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Formatting;

namespace WebApi.CI
{
    class CodeAction : ICodeAction
    {
        private readonly IDocument _document;
        private readonly MethodDeclarationSyntax _method;

        public CodeAction(IDocument document, MethodDeclarationSyntax method)
        {
            _document = document;
            _method = method;
        }

        public CodeActionEdit GetEdit(CancellationToken cancellationToken = new CancellationToken())
        {
            var newReturnType = Syntax.ParseTypeName("HttpResponseMessage").WithTrailingTrivia(Syntax.Space);
            var exp = Syntax.ParseExpression("new HttpResponseMessage(HttpStatusCode.Created)").WithLeadingTrivia(Syntax.Space);

            var returnStatement = Syntax.ReturnStatement(Syntax.Token(SyntaxKind.ReturnKeyword), exp ,Syntax.Token(SyntaxKind.SemicolonToken));
            var oldBody = _method.Body;

            var statements = oldBody.Statements.Where(x => x.GetType() != typeof (ReturnStatementSyntax)).ToList();
            var syntaxListStatements = new SyntaxList<StatementSyntax>();
            syntaxListStatements = statements.Aggregate(syntaxListStatements, (current, syntaxListStatement) => current.Add(syntaxListStatement));
            syntaxListStatements = syntaxListStatements.Add(returnStatement);

            var newBody = Syntax.Block(Syntax.Token(SyntaxKind.OpenBraceToken), syntaxListStatements, Syntax.Token(SyntaxKind.CloseBraceToken));

            var newmethod = Syntax.MethodDeclaration(_method.AttributeLists, _method.Modifiers, newReturnType,
                                                     _method.ExplicitInterfaceSpecifier, _method.Identifier,
                                                     _method.TypeParameterList, _method.ParameterList,
                                                     _method.ConstraintClauses, newBody);

            var oldRoot = _document.GetSyntaxRoot(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(_method,newmethod);

            var newDoc = _document.UpdateSyntaxRoot(newRoot.Format(FormattingOptions.GetDefaultOptions()).GetFormattedRoot());
            return new CodeActionEdit(newDoc);

        }

        public string Description
        {
            get { return "Replace with HttpResponseMessage"; }
        }
    }
}