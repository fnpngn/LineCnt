using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineCnt
{

    //├└─╠═╔╚
    public interface IIndexCharsetProvider
    {
        public IndexCharset Get();

        public static IIndexCharsetProvider GetDefault() => new SlimCharset();
    }

    public sealed class WideCharset : IIndexCharsetProvider
    {
        public IndexCharset Get() => new IndexCharset('║', '╠', '╚', '═');
    }

    public sealed class SlimCharset : IIndexCharsetProvider
    {
        public IndexCharset Get() => new IndexCharset('│', '├', '└', '─');
    }

    public record struct IndexCharset(char Sibling, char Entry, char Bottom, char EntryBody)
    {
        public char Separator { readonly get; set; } = Path.DirectorySeparatorChar;
    };
}
