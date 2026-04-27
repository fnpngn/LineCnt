using System.IO;
using System.Text;

namespace LineCnt
{
    public static class IndexSerializer
    {
        private const UInt64 MAGIC = 0x4c_69_6e_65_43_6e_74_00;
        public static Task SerializationTask { get; private set; } = Task.CompletedTask;

        public static string PathToIndex(string rootPath)
        {
            ReadOnlySpan<char> extension = stackalloc char[] { '.', 'c', 'n', 't' };
            ReadOnlySpan<char> rootSpan = rootPath.AsSpan();

            StringBuilder sb = new StringBuilder(rootPath);
            if (Path.EndsInDirectorySeparator(rootSpan) == false)
            {
                sb.Append(Path.DirectorySeparatorChar);
            }

            sb.Append(Path.GetFileName(rootSpan));
            sb.Append(extension);

            return sb.ToString();
        }

        public static Index Load(string path)
        {
            if (File.Exists(path))
            {
                Index index = LoadFile(path);
                Console.WriteLine("Loaded index " + Path.GetFileName(path));
                return index;
            }

            return new Index();
        }

        public static Index LoadFile(string path)
        {
            using var reader = new BinaryReader(File.OpenRead(path));

            CheckMagic(reader);
            IndexSerializationContext ctx = CreateContext(reader);
            Stack<IndexDirectoryChildren> iStack = new Stack<IndexDirectoryChildren>();

            int childCount = reader.ReadInt32();

            ArgumentOutOfRangeException.ThrowIfNegative(childCount, "Loaded index error: invalid child count");

            IndexDirectory root = new IndexDirectory(childCount);

            root.Deserialize(reader, in ctx);

            iStack.Push(new IndexDirectoryChildren(root, childCount));

            while (iStack.TryPop(out IndexDirectoryChildren item))
            {
                IndexDirectory directory = item.Directory;
                int count = item.ChildCount;

                if (count > 1)
                {
                    iStack.Push(new IndexDirectoryChildren(directory, count - 1));
                }

                childCount = reader.ReadInt32();

                IndexDirectory child = new IndexDirectory(childCount);
                child.Deserialize(reader, in ctx);
                directory.Children.Add(child);

                if (childCount > 0)
                {
                    iStack.Push(new IndexDirectoryChildren(child, childCount));
                }
            }

            Index index = new Index(root);
            index.IndexCumulativeCount();
            return index;
        }

        private static IndexSerializationContext CreateContext(BinaryReader reader)
        {
            bool shallow = reader.ReadBoolean();
            string rootFullName = reader.ReadString();

            return new IndexSerializationContext(shallow);
        }

        private static void CheckMagic(BinaryReader reader)
        {
            UInt64 magic = reader.ReadUInt64();

            if (MAGIC != magic)
            {
                throw new InvalidDataException("Corrupt or not an index file");
            }
        }

        public static void SaveFile(Index index, string path, bool shallow)
        {
            using var stream = File.Create(path);
            using var writer = new BinaryWriter(stream);
            var children = ConcurrentPool<List<IndexDirectory>>.Get().Take();

            writer.Write(MAGIC);
            writer.Write(shallow);

            IndexDirectory? root = index.RootDirectory;
            if (root == null)
            {
                return;
            }

            writer.Write(root.FullName);

            Stack<IndexDirectory> iStack = new Stack<IndexDirectory>();
            iStack.Push(root);

            IndexSerializationContext ctx = new IndexSerializationContext(shallow);

            while (iStack.TryPop(out IndexDirectory? directory))
            {
                List<IndexDirectory> currentChildren = directory.Children;
                int childCount = currentChildren.Count;
                writer.Write(childCount);
                directory.Serialize(writer, in ctx);

                for (int i = childCount - 1; i >= 0; i--)
                {
                    iStack.Push(currentChildren[i]);
                }
            }
        }

        public static void RunSerialization(Index index, bool isShallow)
        {
            if (index.RootDirectory == null)
            {
                return;
            }

            string path = IndexSerializer.PathToIndex(index.RootDirectory.FullName);

            SerializationTask = SerializeAsync(index, path, isShallow);
        }

        private static async Task SerializeAsync(Index index, string path, bool isShallow)
        {
            await Task.Run(delegate
            {
                IndexSerializer.SaveFile(index, path, isShallow);
            });
        }

        private record struct IndexDirectoryChildren(IndexDirectory Directory, int ChildCount);
    }
}
