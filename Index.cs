using System.Text;

namespace LineCnt
{
    public class Index
    {
        public IndexDirectory? RootDirectory { get; private set; }

        private Dictionary<string, IndexDirectory> _directories;


        public Index()
        {
            _directories = new Dictionary<string, IndexDirectory>();
        }

        public Index(IndexDirectory root) : this()
        {
            RootDirectory = root;
        }


        public IndexDirectory CreateIndexDirectory(DirectoryInfo info)
        {
            IndexDirectory dir = new IndexDirectory(info.FullName, info.LastWriteTime);
            _directories[dir.FullName] = dir;

            RootDirectory ??= dir;
            return dir;
        }

        public IndexDirectory GetOrCreateDirectory(string fullName)
        {
            if (_directories.TryGetValue(fullName, out IndexDirectory? directory))
            {
                return directory;
            }

            directory = new IndexDirectory(fullName, default);
            _directories[fullName] = directory;
            return directory;
        }

        public List<IndexDirectory> Flattened()
            => DirectoriesFlattened(RootDirectory);

        public static List<IndexDirectory> DirectoriesFlattened(IndexDirectory root)
        {
            List<IndexDirectory> indexDirectories = new List<IndexDirectory>(30);
            Stack<IndexDirectory> iStack = new Stack<IndexDirectory>(30);
            iStack.Push(root);

            while (iStack.TryPop(out IndexDirectory? iDirectory))
            {
                indexDirectories.Add(iDirectory);
                foreach (IndexDirectory child in iDirectory.Children)
                {
                    iStack.Push(child);
                }
            }

            return indexDirectories;
        }

        public void IndexCumulativeCount()
        {
            Stack<CumulativeStackItem> iStack = new Stack<CumulativeStackItem>(30);
            iStack.Push(new CumulativeStackItem(RootDirectory, false));

            while (iStack.TryPop(out CumulativeStackItem item))
            {
                IndexDirectory iDirectory = item.Directory;

                if (item.allChildrenProcessed)
                {
                    uint totalCount = iDirectory.LineCount;
                    foreach (var child in iDirectory.Children)
                    {
                        totalCount += child.TotalLineCount;
                    }
                    iDirectory.TotalLineCount = totalCount;
                }
                else
                {
                    iStack.Push(new CumulativeStackItem(iDirectory, true));

                    foreach (var child in iDirectory.Children)
                    {
                        iStack.Push(new CumulativeStackItem(child, false));
                    }
                }
            }
        }

#if DEBUG
        public void DebugDumpDirectories()
        {
            StringBuilder sb = new StringBuilder(_directories.Count * 50);

            sb.Append(_directories.Count);
            sb.AppendLine(" directories");

            foreach (var dir in _directories)
            {
                sb.AppendLine(dir.Value.FullName);
            }

            Debug.Print(sb.ToString());
        }
#endif

        private record struct CumulativeStackItem(IndexDirectory Directory, bool allChildrenProcessed);
    }
}