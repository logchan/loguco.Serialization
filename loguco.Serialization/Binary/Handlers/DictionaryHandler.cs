using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Binary.Handlers
{
    public class DictionaryHandler<TKey, TValue> : GenericTypeHandlerMaker
    {
        private readonly MethodInfo _helperWrite = typeof(DictionaryHandlerHelper).GetMethod("WriteDictionary");
        private readonly MethodInfo _helperRead = typeof(DictionaryHandlerHelper).GetMethod("ReadDictionary");

        public override void AddExpressions(TypeHandlerCreator.HandlerExpressionPackage package)
        {
            package._writeExpressions.Add(
                Expression.Call(_helperWrite.MakeGenericMethod(typeof(TKey), typeof(TValue)),
                    package._writeObject,
                    package._writeBinaryWriter,
                    package._writeContext)
            );

            package._readExpressions.Add(
                Expression.Assign(package._readObject,
                    Expression.Call(_helperRead.MakeGenericMethod(typeof(TKey), typeof(TValue)),
                        package._readBinaryReader,
                        package._readContext
                    ))
            );

            package._pendingTypes.Add(typeof(TKey));
            package._pendingTypes.Add(typeof(TValue));
        }
    }
}
