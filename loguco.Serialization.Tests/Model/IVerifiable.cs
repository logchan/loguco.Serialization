using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Tests.Model
{
    public interface IVerifiable<T>
    {
        void Verify(T other, StringBuilder sb);
    }
}
