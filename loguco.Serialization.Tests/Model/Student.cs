using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Tests.Model
{
    public class Student : IVerifiable<Student>
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public DateTime? Birth { get; set; }
        public List<Course> Courses { get; } = new List<Course>();
        public Dictionary<int, List<Course>> CoursesByYear { get; } = new Dictionary<int, List<Course>>();

        public void Verify(Student other, StringBuilder sb)
        {
            if (Name != other.Name)
            {
                sb.AppendLine($"Student.Name: {Name}, {other.Name}");
            }
            if (Id != other.Id)
            {
                sb.AppendLine($"Student.Id: {Id}, {other.Id}");
            }
            if (Birth != other.Birth)
            {
                sb.AppendLine($"Student.Birth: {Birth}, {other.Birth}");
            }

            ComparisonHelper.VerifyList(Courses, other.Courses, "Student.Courses", sb);
            ComparisonHelper.CheckDictionary(CoursesByYear, other.CoursesByYear, "Student.CoursesByYear", sb,
                ComparisonHelper.VerifyList);
        }
    }
}
