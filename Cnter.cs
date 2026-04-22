using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using Index = LineCnt.Index;

namespace LineCnt
{
    public delegate bool IndexNodeFilter(string name);
    // .NET file iterations are kind of slow in any situation
    // 
    // Someone on SO said that filtering files manually by extensions is faster than providing wildcards to the system
    public static class Cnter
    {
        public static int MaxBytesValid = 5 * 1024 * 1024;
        public static readonly List<IndexNodeFilter> FileExtensionFilters;
        public static readonly List<IndexNodeFilter> DirectoryFilters;
        private static readonly HashSet<string> _directoryBlacklist;
        private static readonly HashSet<string> _knownFileExtensions;

        static Cnter()
        {
            FileExtensionFilters = new List<IndexNodeFilter>();
            DirectoryFilters = new List<IndexNodeFilter>();
            _directoryBlacklist = [".git"];
            _knownFileExtensions = [".js", ".ts", ".c", ".h", ".cpp", ".hpp", ".cs"];

            DirectoryFilters.Add(x => !_directoryBlacklist.Contains(x));
            FileExtensionFilters.Add(x => _knownFileExtensions.Contains(x));
        }

        public static async Task<Index> CntDirectoryAsync(string path, string[] patterns)
        {
            using CnterTimer timer = new CnterTimer();
            var index = new Index();

            foreach (string pattern in patterns)
            {
                await Parallel.ForEachAsync(Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories),
                   async (file, c) =>
                    {
                        //index.AddConcurrent(file, await CntFileAsync(file));
                    });
            }

            return index;
        }

        public static async Task<Index> CntDirectoryAsync(string path)
        {
            var index = new Index();
            using CnterTimer timer = new CnterTimer();
            IndexDirectories(path, index);

            // Parse dir tree to queue all dirs in parallel
            List<IndexDirectory> indexDirectories = index.Flattened();

            await Parallel.ForEachAsync(indexDirectories, CntIndexDirectoryAsync);

            index.IndexCumulativeCount();

            return index;
        }

        private static void IndexDirectories(string path, Index index)
        {
            DirectoryInfo rootDirInfo = new DirectoryInfo(path);
            StringBuilder sb = new StringBuilder();
            index.CreateIndexDirectory(rootDirInfo);

            // Create dir tree
            foreach (DirectoryInfo info in rootDirInfo.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                if (info.Name.Contains("ref"))
                {

                    Console.WriteLine("Ref");
                }
                if (IsDirectoryBlacklisted(Path.GetFileName(info.FullName.AsSpan())))
                {
                    continue;
                }

                IndexDirectory iDirectory = index.GetOrCreateDirectory(info.FullName);
                iDirectory.LastModified = info.LastWriteTime;

                sb.Append(Path.GetDirectoryName(iDirectory.FullName.AsSpan()));

                IndexDirectory iParentDirectory = index.GetOrCreateDirectory(sb.ToString());
                iParentDirectory.Children.Add(iDirectory);

                sb.Clear();
            }
        }

        private static async ValueTask CntIndexDirectoryAsync(IndexDirectory directory, CancellationToken c)
        {
            foreach (string fileName in Directory.EnumerateFiles(directory.FullName, "*", SearchOption.TopDirectoryOnly))
            {
                if (new FileInfo(fileName).Length > MaxBytesValid)
                {
                    continue;
                }

                if (IsExtBlacklisted(Path.GetExtension(fileName.AsSpan())))
                {
                    continue;
                }

                uint count = await CntFileAsync(fileName);
                IndexFile iFile = new IndexFile(fileName, count);
                directory.Add(in iFile);
            }
        }

        public static async Task<uint> CntFileAsync(string filePath)
        {
            uint count = 0;

            FileStreamOptions options = new() { Mode = FileMode.Open, Access = FileAccess.Read };
            using (StreamReader stream = new StreamReader(filePath, options))
            {
                while (!stream.EndOfStream && await stream.ReadLineAsync() != null)
                {
                    count++;
                }
            }

            return count;
        }

        [Obsolete("Prototype")]
        public static bool IsExtKnownText(ReadOnlySpan<char> extension)
        {
            if (extension.Length == 0) return false;

            ReadOnlySpan<char> known = ".txt.c.h.cpp.hpp.cs.js.ts.md";

            return known.Contains(extension, StringComparison.OrdinalIgnoreCase);
        }

        [Obsolete("Prototype")]
        public static bool IsExtBlacklisted(ReadOnlySpan<char> extension)
        {
            if (extension.Length == 0) return false;

            ReadOnlySpan<char> blacklist = ".wav.BIN.mp4.mp3.flac.mkv.unity.exe.obj.zip.dll.jpeg.jpg.ico.png.so.obj";

            return blacklist.Contains(extension, StringComparison.OrdinalIgnoreCase);
        }

        [Obsolete("Prototype")]
        public static bool IsDirectoryBlacklisted(ReadOnlySpan<char> dir)
        {
            if (dir.Length == 0) return false;

            ReadOnlySpan<char> blacklist = ".git.vs.idea";


            return blacklist.Contains(dir, StringComparison.OrdinalIgnoreCase);
        }

        private class CnterTimer : IDisposable
        {
            private Stopwatch _sw;
            public CnterTimer()
            {
                _sw = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _sw.Stop();
                Console.WriteLine("LineCnt in {0}ms", _sw.ElapsedMilliseconds.ToString());
            }
        }
    }
}
