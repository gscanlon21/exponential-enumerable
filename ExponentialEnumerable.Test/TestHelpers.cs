using System;
using System.Linq;

namespace ExponentialEnumerable.Test
{
    public static class TestHelpers
    {
        private const string EmptyString = "";
        private const string MainFunction = "public void Main()";

        /// <summary>
        /// Builds out a simple C# program 
        /// </summary>
        /// <param name="test">The formatted program</param>
        /// <param name="code">The Main function code block</param>
        /// <param name="imports">Additional namespace imports</param>
        /// <returns>The line number the Main function starts on</returns>
        public static int BuildTestClass(out string test, string code, string imports = EmptyString)
        {
            test = String.Format(@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
{1}

namespace ExponentialEnumerable
{{
    class ExponentialEnumerableTest
    {{
        public void Main() 
        {{
            {0}
        }}
    }}
}}
".TrimStart(), code.TrimStart(), imports.Trim());

            return test.Split(Environment.NewLine).TakeWhile(l => !l.Contains(MainFunction)).Count() + 2;
        }
    }
}
