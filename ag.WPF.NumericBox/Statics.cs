using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ag.WPF.NumericBox
{
    internal static class Statics
    {
        internal static decimal Epsilon { get; } = (decimal)(1 / Math.Pow(10, 28));
    }
}
