using FileStore = LineCnt.SortedStore<string, LineCnt.IndexFile>;

namespace LineCnt
{

    public record struct IndexFile(string Name, uint Count);
    public record IndexDirectory : IIndexSerializable
    {
        private const int MIN_CAPACITY = 5;

        public ReadOnlySpan<IndexFile> Files => _files.Values;
        public ReadOnlySpan<char> Name => FullName.AsSpan(FullName.Length - _nameLength, _nameLength);

        public DateTime LastModified { get; set; }
        public string FullName { get; private set; }
        public uint LineCount { get; private set; }
        public uint TotalLineCount { get; set; }
        public List<IndexDirectory> Children { get; init; }

        private FileStore _files;
        private int _nameLength;


        public IndexDirectory(int capacity = 5)
        {
            _files = new FileStore(capacity, StringComparer.OrdinalIgnoreCase);
            FullName = string.Empty;
            Children = [];
        }
        public IndexDirectory(string fullName, in DateTime lastModified, int capacity)
        {
            FullName = fullName;
            LastModified = lastModified;
            _nameLength = Path.GetFileName(fullName.AsSpan()).Length;

            if (capacity < MIN_CAPACITY)
            {
                capacity = MIN_CAPACITY;
            }

            Children = new List<IndexDirectory>();
            _files = new FileStore(capacity, StringComparer.OrdinalIgnoreCase);
        }

        public IndexDirectory(string fullName, in DateTime lastModified)
            : this(fullName, in lastModified, MIN_CAPACITY) { }

        public void Add(in IndexFile file)
        {
            _files.Add(file.Name, file);
            LineCount += file.Count;
        }

        public void Serialize(BinaryWriter writer, in IndexSerializationContext ctx)
        {
            writer.Write(FullName);
            writer.Write(LineCount);
            writer.Write(LastModified.ToBinary());

            if (!ctx.IsShallow)
            {
                writer.Write(_files.Count);

                foreach (IndexFile file in _files.Values)
                {
                    writer.Write(file.Name);
                    writer.Write(file.Count);
                    // No date yet
                }
            }
        }

        public void Deserialize(BinaryReader reader, in IndexSerializationContext ctx)
        {
            FullName = reader.ReadString();
            LineCount = reader.ReadUInt32();
            LastModified = DateTime.FromBinary(reader.ReadInt64());

            _nameLength = Path.GetFileName(FullName.AsSpan()).Length;

            if (!ctx.IsShallow)
            {
                int count = reader.ReadInt32();
                _files.EnsureCapacity(count);

                for (int i = 0; i < count; i++)
                {
                    string name = reader.ReadString();
                    uint fileCount = reader.ReadUInt32();
                    _files.Add(name, new IndexFile(name, fileCount));
                }
            }
        }
    }
}
