using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music
{
    public static class Helpers
    {
        // Forces the value for a numeric up-down control to an integer within its min/max range.
        public  static decimal LimitRange(NumericUpDown control, int value)
        {
            var min = (int)control.Minimum;
            var max = (int)control.Maximum;
            return (decimal)Math.Max(min, Math.Min(max, value));
        }
    }
}
