using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ValueObjectSyntaxTreeAnalyzer;

namespace Analyzer1
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Analyzer1Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IsValidMethodAnalyzer1";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Code Structure Reformator";

        private static DiagnosticDescriptor CreateIsValidRule
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(CreateIsValidRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AttributeList);
        }


        private static bool IsValidMethodContains(MethodDeclarationSyntax mds)
        {
            var sourceText = mds.ReturnType.GetText();
            var s = sourceText.ToString();
            var b = s.Contains("IEnumerable<ValidationResult>");
            var b1 = mds.Identifier.ValueText == "IsValid";
            var any = !mds.ParameterList.Parameters.Any();
            var isPublicStatic = new[] { "public", "static" }.All(x => mds.Modifiers.Any(mod => x.Contains(mod.Text)));
            return b && b1 && any && isPublicStatic;
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            AttributeSyntax att = null;

            var als = context.Node as AttributeListSyntax;

            if (!als.Attributes.Any(xx => xx.Name.GetText().ToString() == "ValueObject"))
            {
                return;
            }

            if (!(als.Parent is ClassDeclarationSyntax cds))
            {
                return;
            }

            var maybeIsValidMethod = cds.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(x => x.Identifier.ValueText == "IsValid");

            if (!maybeIsValidMethod.Any() || !maybeIsValidMethod.Any(x => IsValidMethodContains(x)))
            {
                context.ReportDiagnostic(Diagnostic.Create(CreateIsValidRule, cds.Identifier.GetLocation()));
            }
        }
    }
}
