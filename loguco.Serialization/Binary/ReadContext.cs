using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Binary
{
    public class ReadContext
    {
        public Dictionary<int, object> OffsetObjects { get; set; } = new Dictionary<int, object>();
    }
}
