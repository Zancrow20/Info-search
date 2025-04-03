namespace FirstTask.Contracts;

public record CrawlerDto(IEnumerable<string> UriList, int Count = 100); 

public record HtmlPageInfo(List<Uri> ChildUris, List<string> Words);
