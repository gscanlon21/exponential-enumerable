using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = ExponentialEnumerable.Test.CSharpCodeFixVerifier<
    ExponentialEnumerable.ExponentialEnumerableAnalyzer,
    ExponentialEnumerable.ExponentialEnumerableCodeFixProvider>;

namespace ExponentialEnumerable.Test
{
    [TestClass]
    public class ParenthesizedLambdaExpressionTests
    {
        [TestMethod]
        public async Task TestNoDiagnosticFromList()
        {
            TestHelpers.BuildTestClass(out string test, @"
var ones = Enumerable.Repeat(1, 10);
var twos = Enumerable.Repeat(2, 10).ToList();
	
ones.Where(o => twos.Contains(o)).ToList();
");

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestDiagnosticFromIEnumerable()
        {
            var mainLine = TestHelpers.BuildTestClass(out string test, @"
var ones = Enumerable.Repeat(1, 10);
var twos = Enumerable.Repeat(2, 10);
	
ones.Where((o) => { return twos.Contains(o); }).ToList();
");

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(mainLine + 4, 28).WithArguments("twos");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
