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
        private const string IEnumerableMetadataName = "System.Collections.IEnumerable";
        private const string GenericIEnumerableMetadataName = "System.Collections.Generic.IEnumerable`1";
        private const string Var = "var";

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
            context.RegisterSyntaxNodeAction(LookForExponentialEnumerables, SyntaxKind.IdentifierName);
        }

        /// <summary>
        /// Looks for enumerable usages inside a loop.
        /// 
        /// Those may cause the backing query to be executed multiple times.
        /// While that may be desired since the query will always grab fresh data
        /// , it has the potential to cause lag/bugs when the query has side effects or iterates over a large data set
        /// </summary>
        /// <param name="context">The analyzer context</param>
        private static void LookForExponentialEnumerables(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is IdentifierNameSyntax identNameSyntax && identNameSyntax.Identifier.ValueText != Var)
            {
                if (!IsGenericIEnumerable(context, identNameSyntax)) { return; }

                // Loop for usages inside of a loop block
                var loopContext = context.Node.FirstAncestorOrSelf<SyntaxNode>(n => n.Kind() == SyntaxKind.Block);
                if (loopContext != null && LoopKinds.Contains(loopContext.Parent.Kind()))
                {
                    // Make sure we aren't taking into account variables declared inside a loop
                    var isDeclaredInContainingBlock = loopContext.DescendantNodes().OfType<VariableDeclarationSyntax>()
                        .Any(n => n.Variables.Any(v => v.Identifier.ValueText == identNameSyntax.Identifier.ValueText));
                    if (!isDeclaredInContainingBlock)
                    {
                        // The enumerable is being evaluated multiple times inside a loop
                        context.ReportDiagnostic(Diagnostic.Create(ExponentialEnumerableRule, context.Node.GetLocation(), identNameSyntax.Identifier.ValueText));
                    }
                    // We found a loop block, stop the search before we go looking for a LINQ lambda
                    return;
                }

                // Check if the enumerable is used inside another enumerable's lambda expression
                var linqInvocation = context.Node
                    .FirstAncestorOrSelf<SyntaxNode>(n => n.Kind() == SyntaxKind.SimpleLambdaExpression)
                    ?.FirstAncestorOrSelf<SyntaxNode>(n => n.Kind() == SyntaxKind.InvocationExpression);
                
                if (!TryGetIdentSyntax(linqInvocation, out IdentifierNameSyntax linqIdentExpressionSyntax, out _) 
                    || linqIdentExpressionSyntax == identNameSyntax
                    || !InheritsIEnumerable(context, linqIdentExpressionSyntax))
                {
                    return;
                }

                // The enumerable is being evaluated multiple times inside a lambda expression
                context.ReportDiagnostic(Diagnostic.Create(ExponentialEnumerableRule, context.Node.GetLocation(), identNameSyntax.Identifier.ValueText));
            }
        }

        //private static bool IsDeclaredWithingScope(SyntaxNode scope, )

        /// <summary>
        /// Checks if an expression's type inherits System.Collections.IEnumerable
        /// </summary>
        /// <param name="context">The analysis context</param>
        /// <param name="expression">The expression to search on</param>
        /// <returns>True if the expression's type inherits System.CollectionsIEnumerable, otherwise False</returns>
        private static bool InheritsIEnumerable(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            if (expression == null) { return false; }

            var iEnumerableType = context.SemanticModel.Compilation.GetTypeByMetadataName(IEnumerableMetadataName);

            var type = context.SemanticModel.GetTypeInfo(expression).Type?.OriginalDefinition;
            if (type == null) { return false; };

            return type.Interfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, iEnumerableType));
        }

        /// <summary>
        /// Checks if an expression's type is an IEnumerable<T>
        /// </summary>
        /// <param name="context">The analysis context</param>
        /// <param name="expression">The expression to search on</param>
        /// <returns>True if the expression's type is an IEnumerable<T>, otherwise False</returns>
        private static bool IsGenericIEnumerable(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            if (expression == null) { return false; }

            var iEnumerableType = context.SemanticModel.Compilation.GetTypeByMetadataName(GenericIEnumerableMetadataName);

            var type = context.SemanticModel.GetTypeInfo(expression).Type?.OriginalDefinition;
            if (type is null) { return false; };

            return SymbolEqualityComparer.Default.Equals(type, iEnumerableType);
        }

        /// <summary>
        /// Tries to grab the closest expression and name IdentifierNameSyntax nodes of an InvocationExpressionSyntax
        /// </summary>
        /// <param name="invocationExpression">The InvocationExpressionSyntax node to start the search at</param>
        /// <param name="identExpressionSyntax">The expression IdentifierNameSyntax if one was found, otherwise null</param>
        /// <param name="identNameSyntax">The name IdentifierNameSyntax if one was found, otherwise null</param>
        /// <returns>True if both the expression and name IdentifierNameSyntax nodes were found, otherwise False</returns>
        private static bool TryGetIdentSyntax(SyntaxNode invocationExpression, out IdentifierNameSyntax identExpressionSyntax, out IdentifierNameSyntax identNameSyntax)
        {
            if (invocationExpression is InvocationExpressionSyntax invocationExpressionSyntax) {
                if (invocationExpressionSyntax.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                {
                    if (memberAccessExpressionSyntax.Expression != null && memberAccessExpressionSyntax.Name != null)
                    {
                        if (memberAccessExpressionSyntax.Expression is IdentifierNameSyntax identExpression && memberAccessExpressionSyntax.Name is IdentifierNameSyntax identName)
                        {
                            identExpressionSyntax = identExpression;
                            identNameSyntax = identName;
                            return true;
                        }
                        else if (memberAccessExpressionSyntax.Expression is InvocationExpressionSyntax nestedInvocationExpressionSyntax)
                        {
                            return TryGetIdentSyntax(nestedInvocationExpressionSyntax, out identExpressionSyntax, out identNameSyntax);
                        }
                    }
                }
            }

            identExpressionSyntax = null;
            identNameSyntax = null;
            return false;
        }
    }
}
