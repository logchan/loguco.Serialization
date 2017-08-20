using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Tests.Model
{
    public class Course : IVerifiable<Course>
    {
        public string Code { get; set; }
        public string Name { get; set; }

        public void Verify(Course other, StringBuilder sb)
        {
            if (Code != other.Code)
            {
                sb.AppendLine($"Code: {Code}, {other.Code}");
            }
            if (Name != other.Name)
            {
                sb.AppendLine($"Name: {Name}, {other.Name}");
            }
        }
    }
}
