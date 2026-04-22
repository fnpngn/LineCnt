namespace LineCnt
{
    public struct LineCntOptions
    {
        public bool DoSerialize;
        public bool DoSerializeShallow;
        public string RootPath;
        public string[] Patterns;

        private static LineCntOptions _instance;

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
            if (args.Length <= 0)
            {
                RootPath = Directory.GetCurrentDirectory();
                return;
            }

            RootPath = string.Empty;

            if (Path.Exists(args[0]))
            {
                RootPath = args[0];
                args = args.Slice(1, args.Length - 1);
            }
            else if (args[0] == ".")
            {
                RootPath = Directory.GetCurrentDirectory();
                args = args.Slice(1, args.Length - 1);
            }

            int settingsArgs = 0;
            foreach (ReadOnlySpan<char> arg in args)
            {
                if (IsOptionArg(arg, "i", "index"))
                {
                    DoSerialize = true;
                    settingsArgs++;
                }
                else if (IsOptionArg(arg, "is", "index-shallow"))
                {
                    DoSerialize = true;
                    DoSerializeShallow = true;
                    settingsArgs++;
                }
                else
                {
                    break;
                }
            }

            Patterns = GetSearchPatternsFromArgs(args.Slice(settingsArgs, args.Length - settingsArgs));
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

        private static string[] GetSearchPatternsFromArgs(ReadOnlySpan<string> args)
        {
            List<string> patterns = new List<string>();

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

            return patterns.ToArray();
        }
    }
}
