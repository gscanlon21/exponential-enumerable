using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = ExponentialEnumerable.Test.CSharpCodeFixVerifier<
    ExponentialEnumerable.ExponentialEnumerableAnalyzer,
    ExponentialEnumerable.ExponentialEnumerableCodeFixProvider>;

namespace ExponentialEnumerable.Test
{
    [TestClass]
    public class ForStatementTests
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
	
for (var i = 0; i < ones.Count(); i++) 
{
    twos.Contains(i);
}
");

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(mainLine + 6, 5).WithArguments("twos");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
