using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Binary
{
    public static class TypeHandler<T>
    {
        public static Action<BinaryWriter, T, WriteContext> Writer { get; set; }
        public static Func<BinaryReader, ReadContext, T> Reader { get; set; }
    }
}
