using System.Text;
using ThirdTask.Services;

namespace FourthTask.Services;

public class IdfService
{
    private static readonly string BasePath = Directory.GetCurrentDirectory();
    private static readonly string ProcessedPath = Path.Combine(BasePath, "Processed");
    private static readonly string TfIdfPath = Path.GetFullPath(Path.Combine(BasePath, @"..\..\..\..\..\TF-IDF"));
    private static readonly string IdfFilePath = Path.Combine(TfIdfPath, "idf.csv");
    
    private static readonly SortedDictionary<string, decimal> Idf = new(new NaturalSortComparer());

    public static async Task<SortedDictionary<string, decimal>> CreateInverseDocumentFrequency(SortedDictionary<string, SortedSet<string>> index)
    {
        var txtFilesCount = Directory.GetFiles(ProcessedPath, "*.txt").Length;

        foreach (var kv in index)
            Idf.TryAdd(kv.Key, (decimal) kv.Value.Count / txtFilesCount);

        await CsvService.SaveCsv(IdfFilePath, Idf);
        return Idf;
    }
    
    public static bool TryGetInverseDocumentFrequency(out SortedDictionary<string, decimal> idf)
    {
        idf = [];
        
        if (!File.Exists(IdfFilePath))
            return false;
        
        var idfFile = File.ReadAllLines(IdfFilePath, Encoding.UTF8);

        var dictionary = idfFile
            .Skip(1)
            .Select(x => x.Split(";"))
            .ToDictionary(
                kv => kv[0], 
                kv => decimal.Parse(kv[1]));
        idf = new SortedDictionary<string, decimal>(dictionary, new NaturalSortComparer());

        return idf.Count > 0;
    }
}