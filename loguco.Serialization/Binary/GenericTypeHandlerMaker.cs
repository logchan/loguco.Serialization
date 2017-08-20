using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Binary
{
    public abstract class GenericTypeHandlerMaker
    {
        public abstract void AddExpressions(TypeHandlerCreator.HandlerExpressionPackage package);
    }
}
