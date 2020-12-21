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

        // Diagnostic triggered and checked for
        [TestMethod]
        public async Task TestExponentialEnumerableDiagnosticForLinq()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ExponentialEnumerable
{
    class ExponentialEnumerableTest
    {   
        public void Main() 
        {
            // An enumerable of ones [1,1,1,1,1,1,1,1,1,1]
	        var ones = Enumerable.Repeat(1, 10);
	        // An enumerable of twos [2,2,2,2,2,2,2,2,2,2]
	        var twos = Enumerable.Repeat(2, 10);
	
	        ones.Where(o => twos.Contains(o)).ToList();
        }
    }
}";

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(20, 26);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // Diagnostic triggered and checked for
        [TestMethod]
        public async Task TestExponentialEnumerableDiagnosticForExpressiveLinq()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ExponentialEnumerable
{
    class ExponentialEnumerableTest
    {   
        public void Main() 
        {
            // An enumerable of ones [1,1,1,1,1,1,1,1,1,1]
	        var ones = Enumerable.Repeat(1, 10);
	        // An enumerable of twos [2,2,2,2,2,2,2,2,2,2]
	        var twos = Enumerable.Repeat(2, 10);
	
	        ones.Select(o => o).Where(o => twos.Contains(o)).ToList();
        }
    }
}";

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(20, 41);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // Diagnostic triggered and checked for
        [TestMethod]
        public async Task TestExponentialEnumerableDiagnosticForForLoop()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ExponentialEnumerable
{
    class ExponentialEnumerableTest
    {   
        public void Main() 
        {
            // An enumerable of ones [1,1,1,1,1,1,1,1,1,1]
	        var ones = Enumerable.Repeat(1, 10);
	        // An enumerable of twos [2,2,2,2,2,2,2,2,2,2]
	        var twos = Enumerable.Repeat(2, 10);
	
	        for (var i = 0; i < ones.Count(); i++) 
            {
                twos.Contains(i);
            }
        }
    }
}";

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(22, 17);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // Diagnostic triggered and checked for
        [TestMethod]
        public async Task TestExponentialEnumerableDiagnosticForForeachLoop()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ExponentialEnumerable
{
    class ExponentialEnumerableTest
    {   
        public void Main() 
        {
            // An enumerable of ones [1,1,1,1,1,1,1,1,1,1]
	        var ones = Enumerable.Repeat(1, 10);
	        // An enumerable of twos [2,2,2,2,2,2,2,2,2,2]
	        var twos = Enumerable.Repeat(2, 10);
	
	        foreach (var one in ones) 
            {
                twos.Contains(one);
            }
        }
    }
}";

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(22, 17);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // No diagnostics expected to show up
        [TestMethod]
        public async Task TestExponentialEnumerableDiagnosticIgnoresListType()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ExponentialEnumerable
{
    class ExponentialEnumerableTest
    {   
        public void Main() 
        {
            // An enumerable of ones [1,1,1,1,1,1,1,1,1,1]
	        var ones = Enumerable.Repeat(1, 10);
	        // A list of twos [2,2,2,2,2,2,2,2,2,2]
	        var twos = Enumerable.Repeat(2, 10).ToList();
	
	        foreach (var one in ones) 
            {
                twos.Contains(one);
            }
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
