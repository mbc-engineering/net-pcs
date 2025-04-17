using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mbc.Pcs.Net.AsyncUtils
{
    public static class IEnumerableExtensions
    {
        public static async Task ForEachAsync<T>(this IEnumerable<T> list, Func<T, Task> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            foreach (var value in list)
            {
                await func(value);
            }
        }
    }
}
