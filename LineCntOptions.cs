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
            RootPath = string.Empty;

            if (args.Length > 0)
            {
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
            }
            else
            {
                RootPath = Directory.GetCurrentDirectory();
            }

            Patterns = GetSearchPatternsFromArgs(args);
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
