using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Tests.Model
{
    public static class ComparisonHelper
    {
        public static int CompareNullable<T>(T? a, T? b) where T : struct, IComparable<T>
        {
            if (a.HasValue)
                return b.HasValue? a.Value.CompareTo(b.Value) : 1;
            return b.HasValue ? -1 : 0;
        }

        public static bool CompareReference<T>(T a, T b) where T : class
        {
            if (a == null)
                return b == null;
            return ReferenceEquals(a, b);
        }

        private static bool VerifyNull(object a, object b, string title, StringBuilder sb)
        {
            if (a == null)
            {
                if (b != null)
                {
                    sb.AppendLine($"{title} : (null), not null");
                }
                return false;
            }

            if (b == null)
            {
                sb.AppendLine($"{title} : not null, (null)");
                return false;
            }

            return true;
        }

        public static void VerifyList<T>(List<T> list, List<T> other, string title, StringBuilder sb) where T : IVerifiable<T>
        {
            if (!VerifyNull(list, other, title, sb))
                return;

            if (list.Count != other.Count)
            {
                sb.AppendLine($"{title}: {list.Count}, {other.Count}");
            }
            else
            {
                for (var i = 0; i < list.Count; ++i)
                {
                    list[i].Verify(other[i], sb);
                }
            }
        }

        public static void CheckDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict, Dictionary<TKey, TValue> other,
            string title, StringBuilder sb, Action<TValue, TValue, string, StringBuilder> valueCheck)
        {
            if (!VerifyNull(dict, other, title, sb))
                return;

            if (dict.Count != other.Count)
            {
                sb.AppendLine($"{title} : {dict.Count}, {other.Count}");
            }
            else
            {
                foreach (var key in dict.Keys)
                {
                    if (!other.ContainsKey(key))
                    {
                        sb.AppendLine($"{title} : {key} only presents in first dictionary");
                    }
                    else
                    {
                        valueCheck(dict[key], other[key], title, sb);
                    }
                }

                foreach (var key in other.Keys)
                {
                    if (!dict.ContainsKey(key))
                    {
                        sb.AppendLine($"{title} : {key} only presents in second dictionary");
                    }
                }
            }

        }

        public static void VerifyDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict, Dictionary<TKey, TValue> other,
            string title, StringBuilder sb) where TValue : IVerifiable<TValue>
        {
            CheckDictionary(dict, other, title, sb, (a, b, t, s) =>
            {
                a.Verify(b, sb);
            });
        }
    }
}
