using System.Runtime.CompilerServices;
using System.Text;

namespace LineCnt
{
    public static partial class IndexPrinter
    {
        private record struct IndexPrintItem(IndexDirectory Directory, bool IsLast, int depth);
        public static string ToOutStringIndexedDirectories(IndexDirectory root, IIndexCharsetProvider? charsetProvider = null)
        {
            charsetProvider ??= IIndexCharsetProvider.GetDefault();

            StringBuilder sb = new StringBuilder();
            IndexCharset charset = charsetProvider.Get();
            IndentationSequence indentationSequence = new IndentationSequence(in charset);

            int maxLineLength = 0;
            Stack<IndexPrintItem> iStack = new Stack<IndexPrintItem>();

            int pad = CalculatePadLength(root, iStack);
            sb.Append(root.Name);
            sb.Append(' ', pad - root.Name.Length + 1);
            sb.Append(root.TotalLineCount);
            sb.AppendLine();
            //PrintFiles(root, sb, 0, ref charset);


            PrintFiles(iStack, root, sb, ref charset, indentationSequence.GetSpan(), pad);
            PushChildren(iStack, root, 0);

            int lastDepth = 0;

            while (iStack.TryPop(out IndexPrintItem item))
            {
                var (iDirectory, isLast, depth) = item;

                if (depth > lastDepth)
                {
                    indentationSequence.Push(isLast);
                    lastDepth = depth;
                }
                else if (depth < lastDepth)
                {
                    indentationSequence.Pop(lastDepth - depth);
                }

                if (isLast)
                {
                    indentationSequence.SetTopLast(true);
                }

                ReadOnlySpan<char> indent = indentationSequence.GetSpan();

                sb.Append(IndentationSequence.GetDirectory(indent));
                sb.Append(isLast ? charset.Bottom : charset.Entry);
                sb.Append(charset.EntryBody);

                ReadOnlySpan<char> name = iDirectory.Name;
                sb.Append(name);
                sb.Append(' ', pad - name.Length + 1);
                sb.Append(iDirectory.TotalLineCount);
                sb.AppendLine();

                PrintFiles(iStack, iDirectory, sb, ref charset, indent, pad);

                PushChildren(iStack, iDirectory, depth);
            }


            sb.AppendLine();
            sb.Append(root.FullName);
            sb.Append(' ', pad - root.FullName.Length + 1);
            sb.Append(root.TotalLineCount);
            sb.Append(" lines total");

            return sb.ToString();
        }

        private static void PushChildren(Stack<IndexPrintItem> iStack, IndexDirectory iDirectory, int depth)
        {
            var children = iDirectory.Children;
            int count = children.Count;

            if (count <= 0) return;

            iStack.Push(new IndexPrintItem(children[count - 1], true, depth + 1));

            for (int i = count - 2; i > 0; i--)
            {
                iStack.Push(new IndexPrintItem(children[i], false, depth + 1));
            }

        }

        private static void PrintFiles(Stack<IndexPrintItem> iStack, IndexDirectory iDirectory, StringBuilder sb, ref IndexCharset ch, ReadOnlySpan<char> indentation, int pad)
        {
            ReadOnlySpan<IndexFile> files = iDirectory.Files;

            if (files.Length == 0) return;

            int dirNameLength = iDirectory.FullName.Length + 1;
            int nameLength;

            foreach (IndexFile file in files.Slice(0, files.Length - 1))
            {
                sb.Append(indentation);
                //sb.Append(' ', entryIndentation);
                sb.Append(ch.Entry);
                sb.Append(ch.EntryBody);
                nameLength = file.Name.Length - dirNameLength;
                sb.Append(file.Name.AsSpan().Slice(dirNameLength, nameLength));
                sb.Append(' ', pad - nameLength - indentation.Length + 1);// pad with one space + compliment to pad
                sb.Append(file.Count);
                sb.AppendLine();
            }

            IndexFile fileLast = files[files.Length - 1];
            sb.Append(indentation);
            sb.Append(iDirectory.Children.Count > 0 ? ch.Entry : ch.Bottom);
            sb.Append(ch.EntryBody);
            nameLength = fileLast.Name.Length - dirNameLength;
            sb.Append(fileLast.Name.AsSpan().Slice(dirNameLength, nameLength));
            sb.Append(' ', pad - nameLength - indentation.Length + 1);
            sb.Append(fileLast.Count);
            sb.AppendLine();
        }

        private static int CalculatePadLength(IndexDirectory root, Stack<IndexPrintItem> iStack)
        {
            int nameLength;
            int maxLength = 0;

            iStack.Push(new IndexPrintItem(root, false, 0));

            while (iStack.TryPop(out IndexPrintItem item))
            {
                var (iDirectory, _, depth) = item;
                int dirNameLength = iDirectory.FullName.Length;
                int indent = IndentationSequence.MockIndentationLength(depth);

                nameLength = iDirectory.Name.Length - iDirectory.FullName.Length + indent;

                if (maxLength < nameLength)
                {
                    maxLength = nameLength;
                }

                foreach (IndexFile file in iDirectory.Files)
                {
                    nameLength = file.Name.Length - dirNameLength + indent;
                    if (maxLength < nameLength)
                    {
                        maxLength = nameLength;
                    }
                }

                // Don't care about inverting the sequence so faster forward loop inlined here
                foreach (IndexDirectory child in iDirectory.Children)
                {
                    iStack.Push(new IndexPrintItem(child, false, depth + 1));
                }
            }

            return maxLength;
        }
    }
}