using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using loguco.Serialization.Binary.Handlers;

namespace loguco.Serialization.Binary
{
    public static class BinarySerializer
    {
        static BinarySerializer()
        {
            CommonTypeHandlers.Register();
            TypeHandlerCreator.GenericTypeHandlerMakerTypes[typeof(List<>)] = typeof(ListHandler<>);
            TypeHandlerCreator.GenericTypeHandlerMakerTypes[typeof(Dictionary<,>)] = typeof(DictionaryHandler<,>);
        }

        public static void Serialize<T>(Stream stream, T obj)
        {
            if (TypeHandler<T>.Writer == null)
                Initialize<T>();

            using (var bw = new BinaryWriter(stream))
            {
                var context = new WriteContext();
                TypeHandler<T>.Writer(bw, obj, context);
            }
        }

        public static T Deserialize<T>(Stream stream)
        {
            if (TypeHandler<T>.Reader == null)
                Initialize<T>();

            using (var br = new BinaryReader(stream))
            {
                var context = new ReadContext();
                return TypeHandler<T>.Reader(br, context);
            }
        }

        public static void Initialize<T>()
        {
            if (TypeHandler<T>.Writer != null)
                return;

            TypeHandlerCreator.CreateHandlerForType<T>();
        }
    }
}
