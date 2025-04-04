using System.Text;
using System.Text.RegularExpressions;
using FirstTask.Contracts;
using HtmlAgilityPack;

namespace FirstTask.Services;

public partial class WebCrawlerService
{
    private readonly HttpClient _httpClient;
    private const int MinPagesCount = 100;
    private const int MaxPagesCount = 150;
    private readonly ILogger<WebCrawlerService> _logger;
    
    private static readonly string PagesPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\Pages"));

    public WebCrawlerService(IHttpClientFactory httpClientFactory, ILogger<WebCrawlerService> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("WebCrawler");;
    }

    public async Task Cralw(CrawlerDto dto, CancellationToken cancellationToken)
    {
        Queue<Uri> uriQueue = [];
        var count = Math.Clamp(dto.Count, MinPagesCount, MaxPagesCount);
        _logger.LogInformation("Page count: {count}", count);
        HashSet<Uri> visitedUris = [];
        var i = 0;
        
        foreach (var uriStr in dto.UriList)
        {
            var canCreateUri = Uri.TryCreate(uriStr, UriKind.Absolute, out var uri);
            if (!canCreateUri || uri is null)
            {
                _logger.LogWarning("Не получилось определить url: {UriList}", dto.UriList);
                continue;
            }
            uriQueue.Enqueue(uri);
        }
        
        while (uriQueue.TryDequeue(out var currentUri) && i < count)
        {
            if(!visitedUris.Add(currentUri)) 
                continue;
            
            _logger.LogInformation("Processing uri: {currentUri}", currentUri);
            var htmlPageInfo = await GetHtmlPageInfo(currentUri, cancellationToken);
            if(htmlPageInfo is null)
                continue;
            
            foreach (var childUri in htmlPageInfo.ChildUris)
                uriQueue.Enqueue(childUri);
            
            if(htmlPageInfo.Words.Count < 1000)
                continue;
            
            await WriteResults(htmlPageInfo, currentUri, i, cancellationToken);
            i++;
        }
    }

    private async Task WriteResults(HtmlPageInfo htmlPageInfo, Uri currentUri, int index, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(PagesPath))
            Directory.CreateDirectory(PagesPath);

        var indexFilePath = Path.Combine(PagesPath, "index.txt");
        var content = string.Join(Environment.NewLine, htmlPageInfo.Words);
        var contentFilePath = Path.Combine(PagesPath, $"{index}.txt");
        
        _logger.LogInformation("Writing {index}.txt file.", index);
        await File.WriteAllTextAsync(contentFilePath, content, cancellationToken);
        await File.AppendAllTextAsync(indexFilePath, $"{index.ToString(),-5}{currentUri}{Environment.NewLine}", cancellationToken);
    }

    public async Task<HtmlPageInfo?> GetHtmlPageInfo(Uri currentUri, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(currentUri, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(responseStream, Encoding.UTF8);
            var html = await reader.ReadToEndAsync(cancellationToken);
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            var lang = doc.DocumentNode
                .SelectSingleNode("//html[@lang]")
                .GetAttributeValue("lang", string.Empty);
            if (lang.ToLowerInvariant() is not ("ru" or "ru-ru"))
                return null;
            
            var childUris = GetLinks(doc, currentUri);
            var words = GetWords(html);
            
            return new HtmlPageInfo(childUris, words);
        }
        catch (Exception ex)
        {
            _logger.LogError("Не удалось получить данные по ссылке {currentUri}: {errMessage}", currentUri, ex.Message);
            return null;
        }
    }

    private List<string> GetWords(string html)
    {
        // Удаляем теги <script>, <head>, <style> с их содержимым
        html = RemoveMainTagsRegex().Replace(html, " ");
        
        // Убираем все оставшиеся HTML-теги
        html = RemoveOnlyTagsRegex().Replace(html, " ");
        
        //Получаем только русские слова 
        var matches = CyrillicTextRegex().Matches(html);

        return matches.Select(x => x.ToString()).ToList();
    }

    private List<Uri> GetLinks(HtmlDocument htmlDoc, Uri currentUri)
    {
        List<Uri> childUris = [];
        childUris
            .AddRange(htmlDoc.DocumentNode.SelectNodes("//a[@href]")
                .Select(link => link.GetAttributeValue("href", string.Empty))
                .Where(href => !string.IsNullOrWhiteSpace(href))
                .Select(href => new Uri(currentUri, href)));
        return childUris;
    }

    [GeneratedRegex(@"<(script|head|style)[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled, "ru-RU")]
    private static partial Regex RemoveMainTagsRegex();
    
    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex RemoveOnlyTagsRegex();
    
    [GeneratedRegex(@"\p{IsCyrillic}+", RegexOptions.Compiled)]
    private static partial Regex CyrillicTextRegex();
}