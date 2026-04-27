using System.Collections.Frozen;

namespace LineCnt
{
    public partial struct LineCntOptions
    {
        public bool DoSerialize;
        public bool DoSerializeShallow;
        public bool DeserializeOnly;
        public bool ShowHelp;
        public bool SingleFile;
        public string RootPath;
        public string[] Patterns;
        public string[] ExcludeDirectories;
        public FrozenSet<string> Extensions;
#if DEBUG
        public bool DebugDump;
#endif

        private static LineCntOptions _instance;

        public LineCntOptions()
        {
            ExcludeDirectories = Patterns = Array.Empty<string>();
            RootPath = string.Empty;
            Extensions = null!; // Needs to be initialized, but we do it from args only because it's one expensive object
        }

        public static LineCntOptions Get()
        {
            return _instance;
        }

        public static void ParseFromArgs(string[] args)
        {
            LineCntOptions options = new LineCntOptions();
            options.ParseArgs(args);
            _instance = options;
        }

        private void ParseArgs(ReadOnlySpan<string> args)
        {
            Extensions = KnownExtensions.ToFrozenSet(StringComparer.Ordinal);

            if (args.Length <= 0)
            {
                RootPath = Directory.GetCurrentDirectory();
                return;
            }

            if (args[0] == ".")
            {
                RootPath = Directory.GetCurrentDirectory();
                args = args.Slice(1, args.Length - 1);
            }
            else if (Path.Exists(args[0]))
            {
                RootPath = args[0];
                SingleFile = !Directory.Exists(args[0]);
                args = args.Slice(1, args.Length - 1);
            }
            else
            {
                RootPath = Directory.GetCurrentDirectory();
            }

            int argsLength = args.Length;
            int index = 0;
            for (index = 0; index < argsLength; index++)
            {
                ReadOnlySpan<char> arg = args[index];

                if (IsOptionArg(arg, "i", "index"))
                {
                    DoSerialize = true;
                }
                else if (IsOptionArg(arg, "is", "index-shallow"))
                {
                    DoSerialize = true;
                    DoSerializeShallow = true;
                }
                else if (IsOptionArg(arg, "h", "help") || arg == "/?")
                {
                    ShowHelp = true;
                }
                else if (IsOptionArg(arg, "x", "exclude"))
                {
                    int taken = TakeExcludeDirectories(args.Slice(index, args.Length - index));
                    index += taken;
                }
                else if (IsOptionArg(arg, "id", "index-deserialize-only"))
                {
                    DoSerialize = true;
                    DeserializeOnly = true;
                }
                else if (IsOptionArg(arg, "e", "extensions"))
                {
                    int taken = TakeExtensions(args.Slice(index, args.Length - index));
                    index += taken;
                }
#if DEBUG
                else if (IsOptionArg(arg, "dmp", "dump"))
                {
                    DebugDump = true;
                }
#endif
                else
                {
                    ShowHelp = true;
                    break;
                }
            }

            Patterns = GetSearchPatternsFromArgs(args.Slice(index, args.Length - index));
        }

        private static bool IsOptionArg(ReadOnlySpan<char> arg, ReadOnlySpan<char> shortName, ReadOnlySpan<char> longName)
        {
            if (arg.Length < 2)
            {
                return false;
            }

            if (arg[0] != '-') return false;

            if (arg[0] == arg[1] && arg[0] == '-')
            {
                return arg.Slice(2, arg.Length - 2).Equals(longName, StringComparison.Ordinal);
            }

            return arg.Slice(1, arg.Length - 1).Equals(shortName, StringComparison.Ordinal);
        }

        private int TakeExcludeDirectories(ReadOnlySpan<string> args)
        {
            int taken = 0;
            List<string> exclude = ConcurrentPool<List<string>>.Get().Take();

            exclude.EnsureCapacity(args.Length);

            foreach (string arg in args)
            {
                if (arg[0] == '-')
                {
                    return taken;
                }

                exclude.Add(arg);
                taken++;
            }

            ExcludeDirectories = exclude.ToArray();

            exclude.Clear();
            ConcurrentPool<List<string>>.Get().Return(exclude);

            return taken;
        }

        /// <summary>
        /// Appends to the known extensions for -e argument
        /// </summary>
        private int TakeExtensions(ReadOnlySpan<string> args)
        {
            int taken = 0;
            List<string> extensions = LineCntOptions.KnownExtensions;

            extensions.EnsureCapacity(args.Length);

            Debug.Print("\"Extension\" arguments are not yet normalized");

            foreach (string arg in args)
            {
                if (arg[0] == '-')
                {
                    break;
                }

                extensions.Add(arg);
                taken++;
            }

            Extensions = extensions.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
            return taken;
        }

        private static string[] GetSearchPatternsFromArgs(ReadOnlySpan<string> args)
        {
            List<string> patterns = ConcurrentPool<List<string>>.Get().Take();
            patterns.EnsureCapacity(args.Length);

            foreach (string arg in args)
            {
                if (arg[0] == '.')
                {
                    patterns.Add("*" + arg);
                    continue;
                }

                if (arg.Length > 1 && arg[0] == '*' && arg[1] == '.')
                {
                    patterns.Add(arg);
                    continue;
                }

                int dotIndex = arg.IndexOf('.');
                if (dotIndex != -1)
                {
                    patterns.Add("*" + arg.Substring(dotIndex, arg.Length - dotIndex));
                    continue;
                }

                patterns.Add("*." + arg);
            }

            string[] array = patterns.ToArray();
            patterns.Clear();

            ConcurrentPool<List<string>>.Get().Return(patterns);
            return array;
        }
    }
}
