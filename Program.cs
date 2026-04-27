using LineCnt;
using Index = LineCnt.Index;

LineCntOptions.ParseFromArgs(args);
LineCntOptions options = LineCntOptions.Get();

if (options.ShowHelp)
{
    PrintManual();
    return;
}

if (options.DoSerialize || options.DoSerializeShallow)
{
    Console.WriteLine("Serialized index not yet supported");
}

Cnter.FileExtensionFilters.Add(options.Extensions.Contains);

Index index;
if (options.Patterns.Length > 0)
{
    index = await Cnter.CntDirectoryAsync(options.RootPath, options.Patterns);
}
else
{
    index = await Cnter.CntDirectoryAsync(options.RootPath);
}

#if DEBUG
if (options.DebugDump)
{
    index.DebugDumpDirectories();
}
#endif

Console.WriteLine(IndexPrinter.ToOutStringIndexedDirectories(index.RootDirectory));

static void PrintManual()
{
    Console.WriteLine("LineCnt [path] [ext]..");
    Console.WriteLine("LineCnt [path] [ext]..");
    Console.WriteLine("[path]: optional, path of the root directory to start in");
    Console.WriteLine("[ext]: extension (wildcard) or multiple extensions: ");
    Console.WriteLine("\t LineCnt *.cpp .py js");
}