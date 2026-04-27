using LineCnt;
using Index = LineCnt.Index;

LineCntOptions.ParseFromArgs(args);
LineCntOptions options = LineCntOptions.Get();

if (options.ShowHelp)
{
    PrintManual();
    return;
}

if ((options.SingleFile))
{
    Console.WriteLine("Single file not (yet) supported\n");
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
    [ext] - extension or multiple file extensions. If specified, the default known extensions are ignored.
    If you want to instead include your extensions in the overall search use -e
        LineCnt *.cpp .py js
    -e  | --extensions 
    -i  | --index - generate an index. Creates .cnt file inside the root folder
    -is | --index-shallow - not implemented (WIP)
    -id | --index-deserialize-only - read existing index file and print contents without actualizing
    -x  | --exclude - exclude folder names. Default is .vs .git .idea
        LineCnt -x .vs .git
""");
}