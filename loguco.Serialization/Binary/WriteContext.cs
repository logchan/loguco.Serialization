using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Binary
{
    public class WriteContext
    {
        public Dictionary<object, int> ObjectOffsets { get; set; } = new Dictionary<object, int>();
    }
}
