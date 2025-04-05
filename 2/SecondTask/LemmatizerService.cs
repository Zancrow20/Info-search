using System.Diagnostics;
using System.Text;

namespace SecondTask;

public class LemmatizerService
{
    private static readonly string BasePath = Directory.GetCurrentDirectory();
    private static readonly string PagesPath = Path.Combine(BasePath, "Pages");

    private static readonly string ProcessedPagesPath =
        Path.GetFullPath(Path.Combine(BasePath, @"..\..\..\..\..\Processed"));

    private static readonly string StopWordsPath = Path.Combine(BasePath, @"Stopwords\stopwords.txt");

    private static readonly string MystemPath =
        Path.Combine(Directory.GetCurrentDirectory(), @"mystem-yandex\mystem.exe");

    private static Process _process;

    public static async Task Lemmatize()
    {
        if (!File.Exists(MystemPath))
        {
            Console.WriteLine("mystem.exe не найден!");
            return;
        }

        if (!File.Exists(StopWordsPath))
        {
            Console.WriteLine("stopwords.txt не найден!");
            return;
        }

        if (!Directory.Exists(PagesPath))
        {
            Console.WriteLine("Директория с файлами не найдена!");
            return;
        }

        Directory.CreateDirectory(ProcessedPagesPath);

        var stopWords = new HashSet<string>(
            await File.ReadAllLinesAsync(StopWordsPath),
            StringComparer.OrdinalIgnoreCase);

        var txtFiles = Directory
            .GetFiles(PagesPath, "*.txt")
            .Where(f => Path.GetFileName(f) != "index.txt");
        
        //Настройка процесса
        ConfigureProcess();

        foreach (var textFile in txtFiles)
        {
            var content = await File.ReadAllTextAsync(textFile, Encoding.UTF8);

            //Извлекаем слова из текста
            var words = content.Split(" ");
            var tokens = words.Select(m => m.ToLower()).ToList();

            //Удаляем стоп-слова
            var filtered = tokens.Where(token => !stopWords.Contains(token)).ToList();

            //Лемматизация
            var lemmatized = await LemmatizeWithMystem(filtered);

            //Запись значений в файл
            var outputFile = Path.Combine(ProcessedPagesPath, Path.GetFileName(textFile));
            await File.WriteAllTextAsync(outputFile, string.Join(" ", lemmatized), Encoding.UTF8);
        }
    }

    private static void ConfigureProcess()
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = MystemPath,
                Arguments = "-nld",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true, 
                CreateNoWindow = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8
            },
            EnableRaisingEvents = true
        };
        
        _process.ErrorDataReceived += (_, e) => 
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"MyStem ERROR: {e.Data}");
        };
        
        _process.Start();
        _process.BeginErrorReadLine();
    }

    /// <summary>
    /// Лемматизация списка слов через mystem.exe
    /// </summary>
    private static async Task<IEnumerable<string>> LemmatizeWithMystem(IEnumerable<string> tokens)
    {
        List<string> lemmas = [];
        foreach (var token in tokens)
        {
            if (_process.HasExited)
                _process.Start();

            await _process.StandardInput.WriteLineAsync(token);
            var result = await _process.StandardOutput.ReadLineAsync();
            var lemma = result?.Replace("?", "") ?? token;
            lemmas.Add(lemma);
        }

        return lemmas;
    }
}