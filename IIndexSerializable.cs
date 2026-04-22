namespace LineCnt
{
    public interface IIndexSerializable
    {
        public void Serialize(BinaryWriter writer, in IndexSerializationContext ctx);
        public void Deserialize(BinaryReader reader, in IndexSerializationContext ctx);
    }

    public readonly ref struct IndexSerializationContext
    {
        public readonly bool IsShallow;

        public IndexSerializationContext(bool isShallow)
        {
            IsShallow = isShallow;
        }
    }
}
