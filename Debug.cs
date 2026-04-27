using System.Diagnostics;

namespace LineCnt
{
    public static class Debug
    {
        [Conditional("DEBUG")]
        public static void Print(string message)
        {
            Console.Write("dbg: ");
            Console.WriteLine(message);
        }
    }
}
