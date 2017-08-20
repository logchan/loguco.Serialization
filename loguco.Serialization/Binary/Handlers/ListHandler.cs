using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Binary.Handlers
{
    public class ListHandler<T> : GenericTypeHandlerMaker
    {
        private readonly MethodInfo _helperWrite = typeof(ListHandlerHelper).GetMethod("WriteList");
        private readonly MethodInfo _helperRead = typeof(ListHandlerHelper).GetMethod("ReadList");

        public override void AddExpressions(TypeHandlerCreator.HandlerExpressionPackage package)
        {
            package._writeExpressions.Add(
                Expression.Call(_helperWrite.MakeGenericMethod(typeof(T)),
                    package._writeObject,
                    package._writeBinaryWriter,
                    package._writeContext)
            );

            package._readExpressions.Add(
                Expression.Assign(package._readObject,
                    Expression.Call(_helperRead.MakeGenericMethod(typeof(T)),
                        package._readBinaryReader,
                        package._readContext
                    ))
            );

            package._pendingTypes.Add(typeof(T));
        }
    }
}
