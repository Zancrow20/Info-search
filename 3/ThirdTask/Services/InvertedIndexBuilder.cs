using System.Text;

namespace ThirdTask.Services;

public static class InvertedIndexBuilder
{
    private static readonly string BasePath = Directory.GetCurrentDirectory();   
    private static string ProcessedPath = Path.Combine(BasePath, "Processed");
    private static string IndexPath = Path.GetFullPath(Path.Combine(BasePath, @"..\..\..\..\..\Index\index.txt"));
    
    public static async Task<Dictionary<string, List<string>>?> BuildIndex()
    {
        if (!Directory.Exists(ProcessedPath))
        {
            Console.WriteLine("Директория с файлами не найдена!");
            return null;
        }
     
        var index = new SortedDictionary<string, SortedSet<string>>(new NaturalSortComparer());
        
        var txtFiles = Directory.GetFiles(ProcessedPath, "*.txt");

        foreach (var file in txtFiles)
        {
            var filePath = Path.GetFileName(file);
            var content = await File.ReadAllTextAsync(file, Encoding.UTF8);

            var tokens = content.Split(" ").Distinct();

            foreach (var token in tokens)
            {
                var isAdded = index.TryAdd(token, [filePath]);
                if (!isAdded)
                    index[token].Add(filePath);
            }
        }

        return index.ToDictionary(x => x.Key, x => x.Value.ToList());
    }

    public static async Task SaveIndex(Dictionary<string, List<string>>? index)
    {
        ArgumentNullException.ThrowIfNull(index);
        
        var sb = new StringBuilder();

        foreach (var entry in index)
        {
            sb.AppendLine($"{entry.Key}: {string.Join(", ", entry.Value)}");
        }

        await File.WriteAllTextAsync(IndexPath, sb.ToString(), Encoding.UTF8);
    }

    public static bool TryGetIndex(out Dictionary<string, List<string>> index)
    {
        index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(IndexPath))
            return false;
        
        var lines = File.ReadAllText(IndexPath, Encoding.UTF8).Split(Environment.NewLine);

        foreach (var line in lines)
        {
            var split = line.Split(":", StringSplitOptions.RemoveEmptyEntries);
            if(split.Length < 2)
                continue;
            var key = split[0];
            var values = split[1].Split(",", StringSplitOptions.RemoveEmptyEntries);
            index.TryAdd(key, [..values]);
        }

        return index.Count > 0;
    }
}