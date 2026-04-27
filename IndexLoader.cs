namespace LineCnt
{
    public interface IIndexLoader
    {
        Index Load(string path);
        void Save(Index index, string path, bool shallow);
    }


    public class IndexLoader : IIndexLoader
    {
        private const UInt64 MAGIC = 0x4c_69_6e_65_43_6e_74_00;

        public Index Load(string path)
        {
            using var reader = new BinaryReader(File.OpenRead(path));

            CheckMagic(reader);
            IndexSerializationContext ctx = CreateContext(reader);

            IndexDirectory root = new IndexDirectory();
            root.Deserialize(reader, in ctx);

            Stack<IndexDirectory> iStack = new Stack<IndexDirectory>();
            List<IndexDirectory> children = new List<IndexDirectory>();
            iStack.Push(root);

            while (iStack.TryPop(out IndexDirectory? directory))
            {
                int childCount = reader.ReadInt32();

                for (int i = 0; i < childCount; i++)
                {
                    IndexDirectory child = new IndexDirectory();
                    child.Deserialize(reader, in ctx);

                    children.Add(child);
                    directory.Children.Add(child);
                }

                for (int i = children.Count - 1; i >= 0; i--)
                {
                    iStack.Push(children[i]);
                }
                children.Clear();
            }

            return new Index(root);
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

        public void Save(Index index, string path, bool shallow)
        {
            using var stream = File.OpenWrite(path);
            using var writer = new BinaryWriter(stream);

            writer.Write(MAGIC);
            writer.Write(shallow);

            IndexDirectory root = index.RootDirectory;
            if (root == null)
            {
                return;
            }

            writer.Write(root.FullName);

            Stack<IndexDirectory> iStack = new Stack<IndexDirectory>();
            iStack.Push(root);

            IndexSerializationContext ctx = new IndexSerializationContext(shallow);

            while (iStack.TryPop(out IndexDirectory directory))
            {
                directory.Serialize(writer, in ctx);
                writer.Write(directory.Children.Count);
                foreach (var child in directory.Children)
                {
                    iStack.Push(child);
                }
            }
        }
    }
}
