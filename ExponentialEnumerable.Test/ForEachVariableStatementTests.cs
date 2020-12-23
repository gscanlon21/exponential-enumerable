using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = ExponentialEnumerable.Test.CSharpCodeFixVerifier<
    ExponentialEnumerable.ExponentialEnumerableAnalyzer,
    ExponentialEnumerable.ExponentialEnumerableCodeFixProvider>;


namespace ExponentialEnumerable.Test
{
    [TestClass]
    public class ForEachVariableStatementTests
    {
        [TestMethod]
        public async Task TestNoDiagnosticFromList()
        {
            TestHelpers.BuildTestClass(out string test, @"
var ones = Enumerable.Repeat(1, 10);
var twos = Enumerable.Repeat(2, 10).ToList();
	
foreach (var one in ones) 
{
    Debug.WriteLine(twos);
}
");

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestNoDiagnosticFromLoopVariable()
        {
            TestHelpers.BuildTestClass(out string test, @"
var ones = Enumerable.Repeat(Enumerable.Empty<int>(), 10);
	
foreach (var one in ones) 
{
    Debug.WriteLine(one);
}
");

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestNoDiagnosticFromReassignment()
        {
            TestHelpers.BuildTestClass(out string test, @"
var ones = Enumerable.Repeat(1, 10);
var twos = Enumerable.Repeat(2, 10);
	
foreach (var one in ones) 
{
    twos = Enumerable.Empty<int>();
}
");

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestNoDiagnosticFromNestedLoop()
        {
            TestHelpers.BuildTestClass(out string test, @"
var ones = Enumerable.Repeat(1, 10);
	
foreach (var one in ones) 
{
    var twos = Enumerable.Repeat(one, 10);
    foreach (var two in twos) 
    {
        Debug.WriteLine(two);
    }
}
");

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestDiagnosticFromNestedLoop()
        {
            var mainLine = TestHelpers.BuildTestClass(out string test, @"
var ones = Enumerable.Repeat(1, 10);
var twos = Enumerable.Repeat(2, 10);
	
foreach (var one1 in ones) 
{
    foreach (var one2 in ones) 
    {
        Debug.WriteLine(twos);
    }
}
");

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(mainLine + 8, 21).WithArguments("twos");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestDiagnosticFromIEnumerable()
        {
            var mainLine = TestHelpers.BuildTestClass(out string test, @"
var ones = Enumerable.Repeat(1, 10);
var twos = Enumerable.Repeat(2, 10);
	
foreach (var one in ones) 
{
    Debug.WriteLine(twos);
}
");

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(mainLine + 6, 21).WithArguments("twos");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestDiagnosticFromIQueryable()
        {
            var mainLine = TestHelpers.BuildTestClass(out string test, @"
var ones = Enumerable.Repeat(1, 10).AsQueryable();
var twos = Enumerable.Repeat(2, 10).AsQueryable();
	
foreach (var one in ones) 
{
    Debug.WriteLine(twos);
}
");

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(mainLine + 6, 21).WithArguments("twos");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestDiagnosticFromIOrderedEnumerable()
        {
            var mainLine = TestHelpers.BuildTestClass(out string test, @"
var ones = Enumerable.Repeat(1, 10).OrderBy(i => i);
var twos = Enumerable.Repeat(2, 10).OrderBy(i => i);
	
foreach (var one in ones) 
{
    Debug.WriteLine(twos);
}
");

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(mainLine + 6, 21).WithArguments("twos");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestDiagnosticFromIOrderedQueryable()
        {
            var mainLine = TestHelpers.BuildTestClass(out string test, @"
var ones = Enumerable.Repeat(1, 10).AsQueryable().OrderBy(i => i);
var twos = Enumerable.Repeat(2, 10).AsQueryable().OrderBy(i => i);
	
foreach (var one in ones) 
{
    Debug.WriteLine(twos);
}
");

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(mainLine + 6, 21).WithArguments("twos");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
