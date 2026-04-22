using System.Runtime.CompilerServices;
using static LineCnt.IndexPrinter;

namespace LineCnt
{
    public static partial class IndexPrinter
    {
        /// <summary>
        /// Used for stackallocated char "Stack" that tracks the indentation sequence 
        /// instead of re-generating it for every element.
        /// Switches to char[] forever if exceeds stack buffer size <br></br>
        /// <see cref="IndentationSequence.GetSpan(ref IndentationSequence)"/> <br></br>
        /// <see cref="IndentationSequenceExtensions.GetSpan(ref IndentationSequence)"/>
        /// </summary>

        public ref struct IndentationSequence
        {
            private int _count;
            private bool _isUsingStackBuf;
            private InlineCharArray _stackBuf;
            private char[] _strBuf;
            private IndexCharset _charset;

            public IndentationSequence(in IndexCharset charset)
            {
                _charset = charset;
                _count = 0;
                _isUsingStackBuf = true;
                _strBuf = Array.Empty<char>();
            }

            public void Push(bool isLast)
            {
                Span<char> span = _stackBuf;
                if (_isUsingStackBuf && _count + 2 > InlineCharArray.LENGTH)
                {
                    UseStrBuf();
                }
                else if (!_isUsingStackBuf)
                {
                    span = _strBuf;
                }

                if (isLast)
                {
                    span[_count++] = ' ';
                    span[_count++] = ' ';
                }
                else
                {
                    span[_count++] = _charset.Sibling;
                    span[_count++] = ' ';
                }
            }

            public void Pop()
            {
                _count -= 2;
            }

            public void Pop(int count)
            {
                _count -= 2 * count;
            }

            public static int MockIndentationLength(int level)
            {
                return level * 2;
            }

            public void UseStrBuf()
            {
                _strBuf = new char[InlineCharArray.LENGTH * 2];
                _isUsingStackBuf = false;
                Span<char> chars = _stackBuf;
                chars.CopyTo(_strBuf.AsSpan());
            }

            public static ReadOnlySpan<char> GetSpan(ref IndentationSequence seq)
            {
                int count = seq._count;

                if (seq._isUsingStackBuf)
                {
                    ReadOnlySpan<char> buffer = seq._stackBuf;
                    return buffer.Slice(0, count);
                }
                else
                {
                    return seq._strBuf.AsSpan(0, count);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ReadOnlySpan<char> GetDirectory(ReadOnlySpan<char> sequence)
            {
                return sequence.Slice(0, sequence.Length - 2);
            }

            public void SetTopLast(bool isLast)
            {
                Span<char> span = _isUsingStackBuf ? _stackBuf : _strBuf;

                if (isLast)
                {
                    span[_count - 1] = ' ';
                    span[_count - 2] = ' ';
                }
                else
                {
                    span[_count - 2] = _charset.Sibling;
                    span[_count - 1] = ' ';
                }
            }
        }

        [InlineArray(InlineCharArray.LENGTH)]
        public struct InlineCharArray
        {
            public const int LENGTH = 256;
            char c;
        }
    }

    public static class IndentationSequenceExtensions
    {
        // C# limitation: can't return parts of self in ref struct => have to make public + ext methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> GetSpan(ref this IndentationSequence seq)
        {
            return IndentationSequence.GetSpan(ref seq);
        }
    }
}