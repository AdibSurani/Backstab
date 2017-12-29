namespace Backstab
{
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            foreach (var path in args)
                File.WriteAllLines(path + ".txt", STB.GetLines(File.OpenRead(path)));
        }
    }
}
