using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Binary
{
    public static class ReferenceTypeHelper
    {
        public static void Write<T>(BinaryWriter bw, T obj, WriteContext context) where T : class
        {
            if (obj == null)
            {
                bw.Write(-1);
                return;
            }
                
            int pos;
            if (context.ObjectOffsets.TryGetValue(obj, out pos))
            {
                bw.Write(pos);
            }
            else
            {
                bw.Write(-2);
                TypeHandler<T>.Writer(bw, obj, context);
            }
        }

        public static T Read<T>(BinaryReader br, ReadContext context) where T : class
        {
            var pos = br.ReadInt32();
            switch (pos)
            {
                case -1:
                    return null;
                case -2:
                    return TypeHandler<T>.Reader(br, context);
                default:
                    return (T) context.OffsetObjects[pos];
            }
        }
    }
}
