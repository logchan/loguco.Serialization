using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loguco.Serialization.Tests.Model
{
    public class School
    {
        public List<Course> Courses { get; } = new List<Course>();
        public Dictionary<string, Course> CoursesDictionary { get; } = new Dictionary<string, Course>();
        public List<Student> Students { get; } = new List<Student>();
        public List<Teacher> Teachers { get; } = new List<Teacher>();
        public string Name { get; set; }

        public string Verify(School other)
        {
            var sb = new StringBuilder();
            if (Name != other.Name)
                sb.AppendLine($"School.Name: {Name}, {other.Name}");

            ComparisonHelper.VerifyList(Courses, other.Courses, "School.Courses", sb);
            ComparisonHelper.VerifyList(Students, other.Students, "School.Students", sb);
            ComparisonHelper.VerifyList(Teachers, other.Teachers, "School.Teachers", sb);
            ComparisonHelper.VerifyDictionary(CoursesDictionary, other.CoursesDictionary, "School.CoursesDictionary", sb);

            return sb.ToString();
        }
    }
}
