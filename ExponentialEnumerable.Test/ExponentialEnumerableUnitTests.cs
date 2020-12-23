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
	        var ones = Enumerable.Repeat(1, 10);
	        var twos = Enumerable.Repeat(2, 10);
	
	        ones.Where(o => twos.Contains(o)).ToList();
        }
    }
}";

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(18, 26).WithArguments("twos");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // Diagnostic triggered and checked for
        [TestMethod]
        public async Task TestExponentialEnumerableDiagnosticForVariableUse()
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
	        var ones = Enumerable.Repeat(1, 10);
	        var twos = Enumerable.Repeat(2, 10);
	
	        foreach (var one in ones) 
            {
                Debug.WriteLine(twos);
            }
        }
    }
}";

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(20, 33).WithArguments("twos");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // Diagnostic triggered and checked for
        [TestMethod]
        public async Task TestExponentialEnumerableDiagnosticForListLinq()
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
	        var ones = Enumerable.Repeat(1, 10).ToList();
	        var twos = Enumerable.Repeat(2, 10);
	
	        ones.Where(o => twos.Contains(o)).ToList();
        }
    }
}";

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(18, 26).WithArguments("twos");
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
	        var ones = Enumerable.Repeat(1, 10);
	        var twos = Enumerable.Repeat(2, 10);
	
	        ones.Select(o => o).Where(o => twos.Contains(o)).ToList();
        }
    }
}";

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(18, 41).WithArguments("twos");
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
	        var ones = Enumerable.Repeat(1, 10);
	        var twos = Enumerable.Repeat(2, 10);
	
	        for (var i = 0; i < ones.Count(); i++) 
            {
                twos.Contains(i);
            }
        }
    }
}";

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(20, 17).WithArguments("twos");
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
	        var ones = Enumerable.Repeat(1, 10);
	        var twos = Enumerable.Repeat(2, 10);
	
	        foreach (var one in ones) 
            {
                twos.Contains(one);
            }
        }
    }
}";

            var expected = VerifyCS.Diagnostic(ExponentialEnumerableAnalyzer.ExponentialEnumerableDiagnosticId).WithLocation(20, 17).WithArguments("twos");
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
	        var ones = Enumerable.Repeat(1, 10);
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

        // No diagnostics expected to show up
        [TestMethod]
        public async Task TestExponentialEnumerableDiagnosticIgnoresNestedLoop()
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
	        var ones = Enumerable.Repeat(1, 10);
	
	        foreach (var one in ones) 
            {
                var twos = Enumerable.Repeat(one, 10);
                foreach (var two in twos) 
                {
                    Debug.WriteLine(two);
                }
            }
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // No diagnostics expected to show up
        [TestMethod]
        public async Task TestExponentialEnumerableDiagnosticIgnoresLoop()
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
	        var ones = Enumerable.Repeat(1, 10);
	
	        foreach (var one in ones) 
            {
                var twos = Enumerable.Repeat(one, 10);
                foreach (var two in twos) 
                {
                    Debug.WriteLine(two);
                }
            }
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // No diagnostics expected to show up
        [TestMethod]
        public async Task TestExponentialEnumerableDiagnosticIgnoresVariableDeclaredInLoop()
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
	        var ones = Enumerable.Repeat(Enumerable.Empty<int>(), 10);
	
	        foreach (var two in ones) 
            {
                Debug.WriteLine(two);
            }
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        // No diagnostics expected to show up
        [TestMethod]
        public async Task TestExponentialEnumerableDiagnosticIgnoresReassignment()
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
	        var ones = Enumerable.Repeat(1, 10);
            var twos = Enumerable.Repeat(2, 10);
	
	        foreach (var one in ones) 
            {
                twos = Enumerable.Empty<int>();
            }
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
