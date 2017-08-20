using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using loguco.Serialization.Binary;
using loguco.Serialization.Tests.Model;

namespace loguco.Serialization.Tests
{
    class Program
    {
        private static School CreateSchool(int numCourses, int numStudents, int numTeachers)
        {
            var school = new School {Name = "S"};
            for (var i = 0; i < numCourses; ++i)
            {
                school.Courses.Add(new Course
                {
                    Code = "CCCC" + i.ToString("D4"),
                    Name = "Course " + i
                });
            }
            foreach (var course in school.Courses)
            {
                school.CoursesDictionary[course.Code] = course;
            }

            var timeBase = DateTime.Now.AddYears(-20);
            for (var i = 0; i < numStudents; ++i)
            {
                var s = new Student
                {
                    Birth = i % 17 == 0 ? null : (DateTime?)timeBase.AddDays(1),
                    Name = "Student " + i,
                    Id = 1000000 + i
                };
                for (var j = 0; j < i; ++j)
                {
                    var idx = j % school.Courses.Count;
                    var year = j % 4 + 1;
                    s.Courses.Add(school.Courses[idx]);
                    if (!s.CoursesByYear.ContainsKey(year))
                    {
                        s.CoursesByYear[year] = new List<Course>();
                    }
                    s.CoursesByYear[year].Add(school.Courses[idx]);
                }
            }

            for (var i = 0; i < numTeachers; ++i)
            {
                var t = new Teacher
                {
                    Employed = timeBase.AddYears(-i),
                    Name = "Teacher " + i,
                    Number = 900000 + i
                };
                if (i % 17 == 0)
                {
                    continue;
                }

                t.Teaching = new List<Course>();

                t.Teaching.Add(school.Courses[i % school.Courses.Count]);
                if (i % 3 == 1)
                {
                    t.Teaching.Add(school.Courses[(i + 1) % school.Courses.Count]);
                }
            }

            return school;
        }

        static void Main(string[] args)
        {
            var school = CreateSchool(10, 100, 5);
            BinarySerializer.Initialize<School>();

            byte[] buffer;
            using (var ms = new MemoryStream())
            {
                BinarySerializer.Serialize(ms, school);
                buffer = ms.ToArray();
            }
            Console.WriteLine($"Size of buffer: {buffer.Length:N}");

            using (var ms = new MemoryStream(buffer))
            {
                var s2 = BinarySerializer.Deserialize<School>(ms);
                Console.WriteLine(s2.Verify(school));
            }

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}
