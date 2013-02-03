using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;
using Roslyn.Services.Formatting;

namespace WebApi.CI
{
    [ExportCodeIssueProvider("WebApi.CI", LanguageNames.CSharp)]
    class CodeIssueProvider : ICodeIssueProvider
    {
        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxNode node, CancellationToken cancellationToken)
        {
            var typedNode = (ClassDeclarationSyntax) node;
            if (!typedNode.BaseList.Types.Any(x => x.GetFirstToken().ValueText == "ApiController")) yield break;

            var methods = typedNode.Members.OfType<MethodDeclarationSyntax>().
                                    Where(x => (x.Identifier.ValueText.StartsWith("Post") || x.AttributeLists.Any(a => a.Attributes.Any(s => s.Name.ToString() == "HttpPost"))) && 
                                        x.ReturnType.GetType().IsAssignableFrom(typeof(PredefinedTypeSyntax)) && ((PredefinedTypeSyntax)x.ReturnType).Keyword.Kind == SyntaxKind.VoidKeyword);

            foreach (var method in methods)
            {
                yield return new CodeIssue(CodeIssueKind.Warning, method.ReturnType.Span, 
                    "Consider returning a 201 Status Code", new CodeAction(document, method));
            }
        }

        public IEnumerable<Type> SyntaxNodeTypes
        {
            get
            {
                yield return typeof(ClassDeclarationSyntax);
            }
        }

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxToken token, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> SyntaxTokenKinds
        {
            get
            {
                return null;
            }
        }
    }
}
