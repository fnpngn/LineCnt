using LineCnt;
using Index = LineCnt.Index;

LineCntOptions.ParseFromArgs(args);
LineCntOptions options = LineCntOptions.Get();

if (options.ShowHelp)
{
    PrintManual();
    return;
}

Cnter.FileExtensionFilters.Add(options.Extensions.Contains);

Index? index = null;
if (options.DoSerialize || options.DoSerializeShallow)
{
    index = IndexSerializer.Load(IndexSerializer.PathToIndex(options.RootPath));
}

if (index?.RootDirectory == null)
{
    if (options.Patterns.Length > 0)
    {
        index = await Cnter.CntDirectoryAsync(options.RootPath, options.Patterns);
    }
    else
    {
        index = await Cnter.CntDirectoryAsync(options.RootPath);
    }
}

#if DEBUG
if (options.DebugDump)
{
    index.DebugDumpDirectories();
}
#endif

if ((options.DoSerialize || options.DoSerializeShallow) && !options.DeserializeOnly)
{
    IndexSerializer.RunSerialization(index, options.DoSerializeShallow);
}

Console.WriteLine(IndexPrinter.ToOutStringIndexedDirectories(index.RootDirectory));
Console.Beep();
await Console.Out.FlushAsync();

await IndexSerializer.SerializationTask;

static void PrintManual()
{
    Console.WriteLine("LineCnt [path] [ext]..");
    Console.WriteLine("LineCnt [path] [ext]..");
    Console.WriteLine("[path]: optional, path of the root directory to start in");
    Console.WriteLine("[ext]: extension (wildcard) or multiple extensions: ");
    Console.WriteLine("\t LineCnt *.cpp .py js");
}