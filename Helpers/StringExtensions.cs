using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class StringExtensions
{
    public static string LinesToString(this IEnumerable<string> target)
    {
        return string.Join(string.Empty, target);
    }
}

