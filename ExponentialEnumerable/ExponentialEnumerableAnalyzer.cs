using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ExponentialEnumerable
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExponentialEnumerableAnalyzer : DiagnosticAnalyzer
    {
        public const string ExponentialEnumerableDiagnosticId = "ExponentialEnumerable";

        private const string Category = "Usage";
        private const string GenericIQueryableMetadataName = "System.Linq.IQueryable`1";
        private const string GenericIOrderedQueryableMetadataName = "System.Linq.IOrderedQueryable`1";
        private const string GenericIEnumerableMetadataName = "System.Collections.Generic.IEnumerable`1";
        private const string GenericIOrderedEnumerableMetadataName = "System.Linq.IOrderedEnumerable`1";

        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString ExponentialEnumerableAnalyzerTitle = new LocalizableResourceString(nameof(Resources.ExponentialEnumerableAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ExponentialEnumerableAnalyzerMessageFormat = new LocalizableResourceString(nameof(Resources.ExponentialEnumerableAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ExponentialEnumerableAnalyzerDescription = new LocalizableResourceString(nameof(Resources.ExponentialEnumerableAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor ExponentialEnumerableRule = new DiagnosticDescriptor(ExponentialEnumerableDiagnosticId, ExponentialEnumerableAnalyzerTitle, ExponentialEnumerableAnalyzerMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: ExponentialEnumerableAnalyzerDescription);
        
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(ExponentialEnumerableRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(LookForExponentialEnumerablesInLoops, 
                SyntaxKind.ForEachVariableStatement, SyntaxKind.ForStatement, SyntaxKind.ForEachStatement,
                SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
        }

        /// <summary>
        /// Looks for enumerable usages inside a loop.
        /// 
        /// Those may cause the backing query to be executed multiple times.
        /// While that may be desired since the query will always grab fresh data
        /// , it has the potential to cause lag/bugs when the query has side effects or iterates over a large data set
        /// </summary>
        /// <param name="context">The analyzer context</param>
        private static void LookForExponentialEnumerablesInLoops(SyntaxNodeAnalysisContext context)
        {
            var iEnumerableType = context.SemanticModel.Compilation.GetTypeByMetadataName(GenericIEnumerableMetadataName);
            var iQueryableType = context.SemanticModel.Compilation.GetTypeByMetadataName(GenericIQueryableMetadataName);
            var iOrderedEnumerableType = context.SemanticModel.Compilation.GetTypeByMetadataName(GenericIOrderedEnumerableMetadataName);
            var iOrderedQueryableType = context.SemanticModel.Compilation.GetTypeByMetadataName(GenericIOrderedQueryableMetadataName);

            var loopDataFlow = context.SemanticModel.AnalyzeDataFlow(context.Node);
            if (!loopDataFlow.Succeeded) { return; }

            // Get the syntax scope of the context node, ignoring arguments and loop variable declarations
            var nodeScopes = context.Node.ChildNodes().Where(c =>
                c is BlockSyntax /* For ParenthesizedLambdaExpression and loops */ 
                || c is InvocationExpressionSyntax /* For SimpleLamdaExpressions */
            );
            foreach (var nodeScope in nodeScopes)
            {
                var dataFlow = context.SemanticModel.AnalyzeDataFlow(nodeScope);
                if (!dataFlow.Succeeded) { continue; }

                // Iterate over any symbols that are read in the loop's scope. excluding arguments and variable declarations directly under the context node
                foreach (var readSymbol in dataFlow.ReadInside.Except(loopDataFlow.VariablesDeclared, SymbolEqualityComparer.Default))
                {
                    if (!(readSymbol is ILocalSymbol localSymbol) || localSymbol.Type == null) { continue; }

                    // Check if the symbol is of a type that commonly uses deferred execution
                    if (SymbolEqualityComparer.Default.Equals(localSymbol.Type.OriginalDefinition, iEnumerableType)
                        || SymbolEqualityComparer.Default.Equals(localSymbol.Type.OriginalDefinition, iQueryableType)
                        || SymbolEqualityComparer.Default.Equals(localSymbol.Type.OriginalDefinition, iOrderedEnumerableType)
                        || SymbolEqualityComparer.Default.Equals(localSymbol.Type.OriginalDefinition, iOrderedQueryableType))
                    {
                        // Report a diagnostic for all of the symbol's references that may invoke deferred execution, advise them to consider pulling the execution out of the loop
                        var syntaxReferences = GetDescendantNodesThatInvokeEnumerableEvaluation(nodeScope, localSymbol);
                        foreach (var syntaxReference in syntaxReferences)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(ExponentialEnumerableRule, syntaxReference.GetLocation(), localSymbol.Name));   
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Searches descendant nodes for syntax that may evaluate an enumerable
        /// </summary>
        /// <param name="contextNode">Where to search for descendants</param>
        /// <param name="enumerableSymbol">Return nodes that reference this symbol</param>
        /// <returns>An enumerable of IdentifierNameSyntax nodes</returns>
        private static IEnumerable<IdentifierNameSyntax> GetDescendantNodesThatInvokeEnumerableEvaluation(SyntaxNode contextNode, ILocalSymbol enumerableSymbol)
        {
            foreach (var descendantNode in contextNode.DescendantNodes())
            {
                if (descendantNode is MemberAccessExpressionSyntax memberAccessExpressionSyntax
                    && memberAccessExpressionSyntax.Expression is IdentifierNameSyntax memberAccessExpression
                    && memberAccessExpression.Identifier.ValueText == enumerableSymbol.Name)
                {
                    yield return memberAccessExpression;
                }

                if (descendantNode is ArgumentSyntax argumentSyntax
                    && argumentSyntax.Expression is IdentifierNameSyntax argumentExpression
                    && argumentExpression.Identifier.ValueText == enumerableSymbol.Name)
                {
                    yield return argumentExpression;
                }
            }
        }
    }
}
