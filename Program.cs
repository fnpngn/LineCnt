using LineCnt;
using Index = LineCnt.Index;

if (args.Length > 0 && (args[0] == "-h" || args[0] == "--help" || args[0] == "/?"))
{
    PrintManual();
    return;
}

LineCntOptions.ParseFromArgs(args);
LineCntOptions options = LineCntOptions.Get();

if (options.DoSerialize || options.DoSerializeShallow)
{
    Console.WriteLine("Serialized index not yet supported");
}

Index index;
if (options.Patterns.Length > 0)
{
    index = await Cnter.CntDirectoryAsync(options.RootPath, options.Patterns);
}
else
{
    index = await Cnter.CntDirectoryAsync(options.RootPath);
}

Console.WriteLine(IndexPrinter.ToOutStringIndexedDirectories(index.RootDirectory));

static void PrintManual()
{
    Console.WriteLine("LineCnt [path] [ext]..");
    Console.WriteLine("LineCnt [path] [ext]..");
    Console.WriteLine("[path]: optional, path of the root directory to start in");
    Console.WriteLine("[ext]: extension (wildcard) or multiple extensions: ");
    Console.WriteLine("\t LineCnt *.cpp .py js");
}