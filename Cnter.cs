using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using Index = LineCnt.Index;

namespace LineCnt
{
    // Generics for ref structs forbidden by C#
    public delegate bool IndexNodeFilter(string name);
    public delegate bool IndexNodeFilterSpan(ReadOnlySpan<char> name);

    public static class Cnter
    {
        public static int MaxBytesValid = 5 * 1024 * 1024;
        public static readonly List<IndexNodeFilter> FileExtensionFilters;
        public static readonly List<IndexNodeFilterSpan> DirectoryFilters;

        static Cnter()
        {
            FileExtensionFilters = new List<IndexNodeFilter>();
            DirectoryFilters = new List<IndexNodeFilterSpan>() { IsDirectoryBlacklisted };
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
                if (Filter(DirectoryFilters, Path.GetFileName(info.FullName.AsSpan())))
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
            var filters = FileExtensionFilters;

            foreach (string fileName in Directory.EnumerateFiles(directory.FullName, "*", SearchOption.TopDirectoryOnly))
            {
                if (new FileInfo(fileName).Length > MaxBytesValid)
                {
                    continue;
                }

                if (Filter(filters, Path.GetExtension(fileName)) == false)
                {
                    continue;
                }

                uint count = await CntFileAsync(fileName);
                IndexFile iFile = new IndexFile(fileName, count);
                directory.Add(in iFile);
            }
        }

        private static bool Filter(List<IndexNodeFilter> filters, string name)
        {
            foreach (var filter in filters)
            {
                if (filter(name) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Filter(List<IndexNodeFilterSpan> filters, ReadOnlySpan<char> name)
        {
            foreach (var filter in filters)
            {
                if (filter(name) == false)
                {
                    return false;
                }
            }

            return true;
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
        public static bool IsDirectoryBlacklisted(ReadOnlySpan<char> dir)
        {
            if (dir.Length == 0) return false;

            ReadOnlySpan<char> blacklist = ".git.vs.ideaCMakeCache";


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
