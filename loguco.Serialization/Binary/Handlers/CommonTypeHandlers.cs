using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Binary.Handlers
{
    public static class CommonTypeHandlers
    {
        public static void Register()
        {
            TypeHandler<DateTime>.Writer = WriteDateTime;
            TypeHandler<DateTime>.Reader = ReadDateTime;
        }

        private static DateTime ReadDateTime(BinaryReader br, ReadContext context)
        {
            return new DateTime(br.ReadInt64());
        }

        public static void WriteDateTime(BinaryWriter bw, DateTime dt, WriteContext context)
        {
            bw.Write(dt.Ticks);
        }
    }
}
