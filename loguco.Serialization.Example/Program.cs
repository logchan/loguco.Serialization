using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using loguco.Serialization.Binary;

namespace loguco.Serialization.Example
{
    public class Data
    {
        public int X { get; set; }
        public int? Y { get; set; }
        public int? Z { get; set; }
        public string Name { get; set; }
        public Data Next { get; set; }

        public override string ToString()
        {
            return $"{Name}: {X}, {Y}, {Z} -> {Next?.Name}";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var data = new Data {X = 0x00000001, Y = 0x00000002, Z= 0x00000003, Name = "Answer"};
            data.Next = new Data {X = 0x00000004, Y = 0x00000005, Name = "Another"};
            data.Next.Next = new Data {X = 0x00000006, Z = 0x00000007, Name = "lol"};
            data.Next.Next.Next = data.Next;

            BinarySerializer.Initialize<Data>();

            byte[] buffer;
            using (var ms = new MemoryStream())
            {
                BinarySerializer.Serialize(ms, data);
                buffer = ms.ToArray();
            }

            for (var i = 0; i < buffer.Length; ++i)
            {
                Console.Write(buffer[i].ToString("X2"));
                if (i % 16 == 15)
                    Console.WriteLine();
                else if (i % 4 == 3)
                    Console.Write(' ');
            }
            Console.WriteLine();

            using (var ms = new MemoryStream(buffer))
            {
                data = BinarySerializer.Deserialize<Data>(ms);
            }

            Console.WriteLine(data);
            Console.WriteLine(data.Next);
            Console.WriteLine(data.Next.Next);
            Console.ReadLine();
        }
    }
}
