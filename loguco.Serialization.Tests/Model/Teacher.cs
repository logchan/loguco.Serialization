using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Tests.Model
{
    public class Teacher : IVerifiable<Teacher>
    {
        public string Name { get; set; }
        public DateTime Employed { get; set; }
        public List<Course> Teaching { get; set; }
        public int? Number { get; set; }

        public void Verify(Teacher other, StringBuilder sb)
        {
            if (Name != other.Name)
            {
                sb.AppendLine($"Teacher.Name: {Name}, {other.Name}");
            }
            if (Employed != other.Employed)
            {
                sb.AppendLine($"Teacher.Employed: {Employed}, {other.Employed}");
            }
            if (Number != other.Number)
            {
                sb.AppendLine($"Teacher.Number: {Number}, {other.Number}");
            }
            ComparisonHelper.VerifyList(Teaching, other.Teaching, "Teacher.Teaching", sb);
        }
    }
}
