using System.Collections.Generic;

namespace NewPlugin.WorldgenAPI
{
    public static class LinqUtilities
    {
        public static IEnumerable<T> ToEnumerable<T>(IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }
}