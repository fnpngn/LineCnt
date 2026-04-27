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
    if (options.DeserializeOnly)
    {
        Console.WriteLine($"[{options.RootPath}] Index failed to deserialize or is void. Stopped.");
        return;
    }

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

Console.WriteLine();

static void PrintManual()
{
    Console.WriteLine("""
LineCnt [path] [-i|is|id] [-h] [-e]  [ext]
    [path] - path of the root directory to start in. default: current
        LineCnt .
    [ext] - extension or multiple file extensions
        LineCnt *.cpp .py js
    -i  | --index - generate an index. Creates .cnt file inside the root folder
    -is | --index-shallow - not implemented (WIP)
    -id | --index-deserialize-only - read existing index file and print contents without actualizing
    -e  | --exclude - exclude folder names
""");
}