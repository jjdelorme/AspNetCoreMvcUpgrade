// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace dotnetupgrade
{
    /// <summary>
    /// As with the analzyers, code fix providers that are registered into Upgrade Assistant's
    /// dependency injection container (by IExtensionServiceProvider) will be used during
    /// the source update step.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "HttpStatusCodeResult CodeFix Provider")]
    public class HttpStatusCodeResultCodeFix : CodeFixProvider
    {
        // The Upgrade Assistant will only use analyzers that have an associated code fix provider registered including
        // the analyzer's ID in the code fix provider's FixableDiagnosticIds array.
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HttpStatusCodeResultAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            if (root is null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var node = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<ObjectCreationExpressionSyntax>().First(); 

            if (node is null)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    HttpStatusCodeResultAnalyzer.Title,
                    c => ReplaceHttpStatusCodeResultAsync(context.Document, node, c),
                    nameof(HttpStatusCodeResultAnalyzer)),
                diagnostic);
        }

        private static async Task<Document> ReplaceHttpStatusCodeResultAsync(Document document, 
            ObjectCreationExpressionSyntax node, CancellationToken cancellationToken)
        {
            var statusCodeId = node.DescendantNodes().OfType<IdentifierNameSyntax>().Single(id =>
                id.Identifier.Text == "HttpStatusCodeResult");

            // Use ASP.NET Core object equivalent.
            var newNode = node.ReplaceNode(statusCodeId, GetStatusCodeIdentifier(node));

            // Remove arguments.
            newNode = newNode.RemoveNodes(node.DescendantNodes().OfType<ArgumentSyntax>(), 
                SyntaxRemoveOptions.KeepExteriorTrivia);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot!.ReplaceNode(node, newNode);

            // Return document with transformed tree.
            return document.WithSyntaxRoot(newRoot);
        }

        private static IdentifierNameSyntax GetStatusCodeIdentifier(SyntaxNode node)
        {
            var args = node.DescendantNodes().OfType<ArgumentSyntax>();

            var statusCode = args.Single().DescendantNodes().OfType<IdentifierNameSyntax>()
                .Last().Identifier.Text;

            string name;

            switch (statusCode)
            {
                case "Conflict":
                    name = "ConflictResult";
                    break;
                case "NoContent":
                    name = "NoContentResult";
                    break;
                case "NotFound":
                    name = "NotFoundResult";
                    break;
                case "OK":
                    name = "OkResult";
                    break;
                case "Unauthorized":
                    name = "UnauthorizedResult";
                    break;
                case "UnprocessableEntity":
                    name = "UnprocessableEntityResult";
                    break;
                case "UnsupportedMediaType":
                    name = "UnsupportedMediaTypeResult";
                    break;
                case "InternalServerError":
                    name = "System.Web.Http.InternalServerErrorResult";
                    break;
                default:
                    name = "BadRequestResult";
                    break;
            }

            return SyntaxFactory.IdentifierName(name);
        }
    }
}