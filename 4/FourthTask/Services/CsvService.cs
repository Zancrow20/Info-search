using System.Text;
using ThirdTask.Services;

namespace FourthTask.Services;

public static class CsvService
{
    public static async Task SaveCsv(string path, SortedDictionary<string, SortedDictionary<string, decimal>> dictionary, IEnumerable<string> fileNames)
    {
        if(File.Exists(path))
            return;

        var sorted = fileNames
            .Select(Path.GetFileName)
            .Order(new NaturalSortComparer()!);
        
        List<string> lines = [$"terms;{string.Join(";", sorted)}"];

        lines.AddRange(dictionary.Select(kv => $"{kv.Key};{string.Join(";", kv.Value.Values)}"));

        await File.WriteAllLinesAsync(path, lines, Encoding.UTF8);
    }
    
    public static async Task SaveCsv(string path, SortedDictionary<string, decimal> dictionary)
    {
        if(File.Exists(path))
            return;

        List<string> lines = ["terms;values"];
        lines.AddRange(dictionary.Select(kv => $"{kv.Key};{kv.Value}"));

        await File.WriteAllLinesAsync(path, lines, Encoding.UTF8);
    }
}