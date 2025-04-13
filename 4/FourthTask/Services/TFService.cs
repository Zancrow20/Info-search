using System.Text;
using ThirdTask.Services;

namespace FourthTask.Services;

public class TfService
{
    private static readonly string BasePath = Directory.GetCurrentDirectory();
    private static readonly string ProcessedPath = Path.Combine(BasePath, "Processed");
    private static readonly string TfIdfPath = Path.GetFullPath(Path.Combine(BasePath, @"..\..\..\..\..\TF-IDF"));
    private static readonly string TfFilePath = Path.Combine(TfIdfPath, "tf.csv");

    private static readonly SortedDictionary<string, SortedDictionary<string, decimal>> Tf = new (new NaturalSortComparer());
    public static async Task<SortedDictionary<string, SortedDictionary<string, decimal>>> CreateTermFrequency()
    {
        var txtFiles = Directory.GetFiles(ProcessedPath, "*.txt");

        var frequencies = txtFiles
            .Order(new NaturalSortComparer())
            .Select(Path.GetFileName)
            .ToDictionary(kv => kv!, _ => 0m);
        
        foreach (var textFile in txtFiles)
        {
            var content = await File.ReadAllTextAsync(textFile, Encoding.UTF8);
            var filePath = Path.GetFileName(textFile);

            var words = content.Split(" ");
            var terms = words
                .GroupBy(x => x)
                .Select(x => new { x.Key, Count = x.Count() })
                .ToList();
            foreach (var term in terms)
            {
                Tf.TryAdd(term.Key, new SortedDictionary<string, decimal>(frequencies, new NaturalSortComparer()));
                var frequency = Math.Round((decimal) term.Count / words.Length, 6);;
                Tf[term.Key][filePath] = frequency;
            }
        }

        await CsvService.SaveCsv(TfFilePath, Tf, txtFiles);
        return Tf;
    }
    
    public static bool TryGetTermFrequency(out SortedDictionary<string, SortedDictionary<string, decimal>> tf)
    {
        tf = new SortedDictionary<string, SortedDictionary<string, decimal>>(new NaturalSortComparer());
        
        if (!File.Exists(TfFilePath))
            return false;
        
        var tfFile = File.ReadAllLines(TfFilePath, Encoding.UTF8);

        var docs = tfFile[0].Split(";")[1..];
        
        foreach (var tfInfo in tfFile.Skip(1))
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
            tf.TryAdd(word, new SortedDictionary<string, decimal>(frequencies, new NaturalSortComparer()));
        }

        return tf.Count > 0;
    }
}