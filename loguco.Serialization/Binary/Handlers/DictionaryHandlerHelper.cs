using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Binary.Handlers
{
    public static class DictionaryHandlerHelper
    {
        public static void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict, BinaryWriter bw, WriteContext context)
        {
            bw.Write(dict.Count);
            foreach (var pair in dict)
            {
                TypeHandler<TKey>.Writer(bw, pair.Key, context);
                TypeHandler<TValue>.Writer(bw, pair.Value, context);
            }
        }

        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(BinaryReader br, ReadContext context)
        {
            var dict = new Dictionary<TKey, TValue>();
            var count = br.ReadInt32();
            for (var i = 0; i < count; ++i)
            {
                var key = TypeHandler<TKey>.Reader(br, context);
                dict[key] = TypeHandler<TValue>.Reader(br, context);
            }
            return dict;
        }

        public static void CopyDictionary<TKey, TValue>(Dictionary<TKey, TValue> dest, Dictionary<TKey, TValue> source)
        {
            foreach (var pair in source)
            {
                dest.Add(pair.Key, pair.Value);
            }
        }
    }
}
