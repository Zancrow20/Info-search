using System.Text;
using ThirdTask.Services;

namespace FourthTask.Services;

public class TfIdfService
{
    private static readonly string BasePath = Directory.GetCurrentDirectory();
    private static readonly string ProcessedPath = Path.Combine(BasePath, "Processed");
    private static readonly string TfIdfPath = Path.GetFullPath(Path.Combine(BasePath, @"..\..\..\..\..\TF-IDF"));
    private static readonly string TfIdfFilePath = Path.Combine(TfIdfPath, "tf-idf.csv");
    
    private static readonly SortedDictionary<string, SortedDictionary<string, decimal>> TfIdf = new(new NaturalSortComparer());
    
    public static async Task<SortedDictionary<string, SortedDictionary<string, decimal>>> CreateTermFrequencyInverseDocumentFrequency(
        SortedDictionary<string, decimal> idf, 
        SortedDictionary<string, SortedDictionary<string, decimal>> tf)
    {
        foreach (var (term, frequencies) in tf)
        {
            var frequency = idf[term];
            var termTfIdf = frequencies.ToDictionary(
                kv => kv.Key,
                kv => Math.Round(kv.Value * frequency, 6));
            
            TfIdf.TryAdd(term, new SortedDictionary<string, decimal>(termTfIdf, new NaturalSortComparer()));
        }
     
        var txtFiles = Directory.GetFiles(ProcessedPath, "*.txt");
        await CsvService.SaveCsv(TfIdfFilePath, TfIdf, txtFiles);
        return TfIdf;
    }
    
    public static bool TryGetTermFrequency(out SortedDictionary<string, SortedDictionary<string, decimal>> tfIdf)
    {
        tfIdf = new SortedDictionary<string, SortedDictionary<string, decimal>>(new NaturalSortComparer());
        
        if (!File.Exists(TfIdfFilePath))
            return false;
        
        var tfIdfFile = File.ReadAllLines(TfIdfFilePath, Encoding.UTF8);

        var docs = tfIdfFile[0].Split(";").Skip(1);
        
        foreach (var tfInfo in tfIdfFile.Skip(1))
        {
            var content = tfInfo.Split(";").ToList();
            var word = content[0];

            var values = content
                .Skip(1)
                .Select(decimal.Parse)
                .Zip(docs);
            
            var frequencies = values.ToDictionary(
                kv => kv.Second, 
                kv => kv.First);
            tfIdf.TryAdd(word, new SortedDictionary<string, decimal>(frequencies, new NaturalSortComparer()));
        }

        return tfIdf.Count > 0;
    }
}