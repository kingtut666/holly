using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace KingTutUtils
{
    public class UnreferencedVariable
    {
        [Conditional("Debug")]
        public static void Ignore(object obj)
        {

        }
    }
}
