using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = ExponentialEnumerable.Test.CSharpCodeFixVerifier<
    ExponentialEnumerable.ExponentialEnumerableAnalyzer,
    ExponentialEnumerable.ExponentialEnumerableCodeFixProvider>;

namespace ExponentialEnumerable.Test
{
    [TestClass]
    public class ExponentialEnumerableUnitTest
    {
        // No diagnostics expected to show up
        [TestMethod]
        public async Task TestNoDiagnostic()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
