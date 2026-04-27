using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace LineCnt
{
    public partial struct KnownExtensions
    {
        private FrozenSet<string> _extensions = new string[] {
            ""
        }.ToFrozenSet(StringComparer.Ordinal);

        public KnownExtensions()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(string extension)
        {
            return _extensions.Contains(extension);
        }
    }
}
