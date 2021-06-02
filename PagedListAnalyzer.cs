// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace dotnetupgrade
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PagedListAnalyzer : DiagnosticAnalyzer
    {
        // The diagnostic ID can be any unique string. The default analyzers
        // that Upgrade Assistant uses have IDs prefixed with 'UA' for
        // 'Upgrade Assitant' but any diagnostic ID can be used.
        public const string DiagnosticId = "GCP0002";

        // Upgrade Assistant analyzers typically have a category
        // of 'Upgrade' but, again, any value that makes sense for
        // the analyzer can be used here.
        private const string Category = "Upgrade";

        public static readonly string Title = "ASP.NET Core should use PagedList.Core";
        private static readonly string MessageFormat = "PagedList {0} is not valid in ASP.NET Core";
        private static readonly string Description = "Identifies PagedList instead of PagedList.Core.";

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, 
            Title, 
            MessageFormat, 
            Category, 
            DiagnosticSeverity.Warning, 
            isEnabledByDefault: true, 
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            // Analyzers used by Upgrade Assistant can use any of the usual registered action
            // (syntax-based, symbol-based, operation-based, etc.) but bear in mind that source
            // updaters run relatively late in the upgrade process and changes may have been made
            // earlier (removing .NET Framework references, changing TFM, etc.) that could prevent
            // the project from building correctly. Because of that, analyzers that can work on
            // syntax are especially useful.
            context.RegisterSyntaxNodeAction(AnalyzeUsingStatement, 
                SyntaxKind.UsingDirective);
        }

        // This analysis is specific to this sample analyzer, as described at
        // https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix
        private void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
        {
            var node = (UsingDirectiveSyntax)context.Node;
        
            if (node.Name.ToString() == "PagedList")
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, 
                    node.GetLocation(), 
                    node.ToString()));
            }
            else
            {
                return;
            }
        }
    }
}