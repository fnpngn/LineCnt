namespace LineCnt
{

    public record struct IndexFile(string Name, uint Count);
    public record IndexDirectory : IIndexSerializable
    {
        private const int MIN_CAPACITY = 5;

        public ReadOnlySpan<IndexFile> Files => _files.AsSpan(0, _count);
        public ReadOnlySpan<char> Name => FullName.AsSpan(FullName.Length - _nameLength, _nameLength);

        public DateTime LastModified { get; set; }
        public string FullName { get; private set; }
        public uint LineCount { get; private set; }
        public uint TotalLineCount { get; set; }
        public List<IndexDirectory> Children { get; init; }

        private IndexFile[] _files;
        private SortedList<string, IndexFile> _filesSorted;
        private int _count;
        private int _nameLength;


        public IndexDirectory(int capacity = 5)
        {
            _filesSorted = new SortedList<string, IndexFile>(capacity, StringComparer.Ordinal);
            _files = Array.Empty<IndexFile>();
            FullName = string.Empty;
            Children = [];
        }
        public IndexDirectory(string fullName, in DateTime lastModified, int capacity)
        {
            FullName = fullName;
            LastModified = lastModified;
            _nameLength = Path.GetFileName(fullName.AsSpan()).Length;
            _count = 0;

            if (capacity < MIN_CAPACITY)
            {
                capacity = MIN_CAPACITY;
            }

            Children = new List<IndexDirectory>();
            _files = new IndexFile[capacity];
        }

        public IndexDirectory(string fullName, in DateTime lastModified)
            : this(fullName, in lastModified, MIN_CAPACITY) { }

        public void Add(in IndexFile file)
        {
            if (_count >= _files.Length)
            {
                Array.Resize(ref _files, _files.Length * 2);
            }

            _files[_count++] = file;
            LineCount += file.Count;
            _filesSorted.Add(file.Name, file);
        }

        public void Serialize(BinaryWriter writer, in IndexSerializationContext ctx)
        {
            writer.Write(FullName);
            writer.Write(LineCount);
            writer.Write(LastModified.ToBinary());

            if (!ctx.IsShallow)
            {
                writer.Write(_count);
                foreach (IndexFile file in Files)
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
                if (count > _files.Length)
                {
                    _files = new IndexFile[count];
                }

                for (int i = 0; i < count; i++)
                {
                    _files[i] = new IndexFile(reader.ReadString(), reader.ReadUInt32());
                }

                _count = count;
            }
            else
            {
                _count = 0;
            }
        }
    }
}
