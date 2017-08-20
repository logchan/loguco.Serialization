using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Binary.Handlers
{
    public static class ListHandlerHelper
    {
        public static void WriteList<T>(List<T> list, BinaryWriter bw, WriteContext context)
        {
            bw.Write(list.Count);
            foreach (var value in list)
            {
                TypeHandler<T>.Writer(bw, value, context);
            }
        }

        public static List<T> ReadList<T>(BinaryReader br, ReadContext context)
        {
            var list = new List<T>();
            var count = br.ReadInt32();
            list.Capacity = count;

            for (var i = 0; i < count; ++i)
            {
                list.Add(TypeHandler<T>.Reader(br, context));
            }
            return list;
        }
    }
}
