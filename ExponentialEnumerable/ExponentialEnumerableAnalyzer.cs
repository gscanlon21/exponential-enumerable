using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace ExponentialEnumerable
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExponentialEnumerableAnalyzer : DiagnosticAnalyzer
    {
        public const string ExponentialEnumerableDiagnosticId = "ExponentialEnumerable";

        private const string Category = "Usage";
        private const string IEnumerableMetadataName = "System.Collections.Generic.IEnumerable`1";

        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString ExponentialEnumerableAnalyzerTitle = new LocalizableResourceString(nameof(Resources.ExponentialEnumerableAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ExponentialEnumerableAnalyzerMessageFormat = new LocalizableResourceString(nameof(Resources.ExponentialEnumerableAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ExponentialEnumerableAnalyzerDescription = new LocalizableResourceString(nameof(Resources.ExponentialEnumerableAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor ExponentialEnumerableRule = new DiagnosticDescriptor(ExponentialEnumerableDiagnosticId, ExponentialEnumerableAnalyzerTitle, ExponentialEnumerableAnalyzerMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: ExponentialEnumerableAnalyzerDescription);
        
        private static readonly ImmutableArray<SyntaxKind> LoopKinds = ImmutableArray.Create(SyntaxKind.ForEachStatement, SyntaxKind.ForStatement, SyntaxKind.ForEachVariableStatement);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(ExponentialEnumerableRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(LookForExponentialEnumerables, SyntaxKind.InvocationExpression);
        }

        /// <summary>
        /// Looks for enumerable method invocations that reside inside a loop.
        /// 
        /// Those cause the backing query to be executed multiple times.
        /// While that may be desired since the query will always grab fresh data
        /// , it has the potential to cause lag/bugs when the query has side effects or iterates over a large data set
        /// </summary>
        /// <param name="context">The analyzer context</param>
        private static void LookForExponentialEnumerables(SyntaxNodeAnalysisContext context)
        {
            if (TryGetIdentNameSyntax(context.Node, out IdentifierNameSyntax identNameSyntax))
            {
                if (!IsIEnumerable(context, identNameSyntax)) { return; }

                // Loop for invocations inside of a loop block
                var loopNode = context.Node.FirstAncestorOrSelf<SyntaxNode>(n => n.Kind() == SyntaxKind.Block);
                if (loopNode != null && LoopKinds.Contains(loopNode.Parent.Kind()))
                {
                    // The enumerable is being executed multiple times inside a loop
                    context.ReportDiagnostic(Diagnostic.Create(ExponentialEnumerableRule, context.Node.GetLocation()));
                }

                // If loopNode is an invocation expression and it's caller identifier is an enumerable, then report diagnostic (for linq expressions)
                var potentialLinqInvocation = context.Node.Parent.FirstAncestorOrSelf<SyntaxNode>(a => a.Kind() == SyntaxKind.InvocationExpression);
                
                // Check if the parent invocation is invoked on an enumerable
                if (!TryGetIdentNameSyntax(potentialLinqInvocation, out IdentifierNameSyntax linqIdentNameSyntax) || linqIdentNameSyntax == identNameSyntax || !IsIEnumerable(context, linqIdentNameSyntax))
                {
                    return;
                }

                // The enumerable is being invoked multiple times inside a loop
                context.ReportDiagnostic(Diagnostic.Create(ExponentialEnumerableRule, context.Node.GetLocation()));
            }
        }

        /// <summary>
        /// Checks if an expression's type is an IEnumerable<T>
        /// </summary>
        /// <param name="context">The analysis context</param>
        /// <param name="expression">The expression to search for an enumerable type</param>
        /// <returns>True if the expression's type is an IEnumerable<T>, otherwise False</returns>
        private static bool IsIEnumerable(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            // Returns the IEnumerable<T> generic type
            var iEnumerableType = context.SemanticModel.Compilation.GetTypeByMetadataName(IEnumerableMetadataName);

            // Grab the generic type of the identifier
            var type = context.SemanticModel.GetTypeInfo(expression).Type?.OriginalDefinition;
            if (type is null) { return false; };

            return SymbolEqualityComparer.Default.Equals(type, iEnumerableType);
        }

        /// <summary>
        /// Tries to grab the direct IdentifierNameSyntax child of a SyntaxNode
        /// 
        /// If the IdentifierNameSyntax node is nested too deeply, this method will not find it
        /// </summary>
        /// <param name="invocationExpression">The InvocationExpressionSyntax node to start the search at</param>
        /// <param name="identNameSyntax">The IdentifierNameSyntax if one was found, otherwise null</param>
        /// <returns>True if the IdentifierNameSyntax node was found, otherwise False</returns>
        private static bool TryGetIdentNameSyntax(SyntaxNode invocationExpression, out IdentifierNameSyntax identNameSyntax)
        {
            if (invocationExpression is InvocationExpressionSyntax invocationExpressionSyntax) {
                if (invocationExpressionSyntax.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                {
                    if (!(memberAccessExpressionSyntax.Expression is null))
                    {
                        if (memberAccessExpressionSyntax.Expression is IdentifierNameSyntax identName)
                        {
                            identNameSyntax = identName;
                            return true;
                        }
                        else if (memberAccessExpressionSyntax.Expression is InvocationExpressionSyntax invocationExpressionSyntax2)
                        {
                            return TryGetIdentNameSyntax(invocationExpressionSyntax2, out identNameSyntax);
                        }
                    }
                }
            }

            identNameSyntax = null;
            return false;
        }
    }
}
