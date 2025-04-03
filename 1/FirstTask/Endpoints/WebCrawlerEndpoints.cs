using FirstTask.Contracts;
using FirstTask.Services;

namespace FirstTask.Endpoints;

public static class WebCrawlerEndpoints
{
    public static void MapWebCrawlerEndpoints(this IEndpointRouteBuilder routes)
    {
        var routeGroup = routes
            .MapGroup("/api/crawler")
            .WithTags("Crawler")
            .WithOpenApi();
        
        routeGroup.MapPost("/createDocs", 
            async (CrawlerDto dto, WebCrawlerService webCrawlerService, CancellationToken cancellationToken) =>
            {
                await webCrawlerService.Cralw(dto, cancellationToken);
            })
            .WithName("CreateDocs");
        
        routeGroup.MapPost("/check", 
                async (string uriStr, WebCrawlerService webCrawlerService, CancellationToken cancellationToken) =>
                {
                    var canCreate = Uri.TryCreate(uriStr, UriKind.Absolute, out var uri);
                    if(canCreate && uri is not null)
                        return Results.Ok(await webCrawlerService.GetHtmlPageInfo(uri, cancellationToken));
                    return Results.BadRequest($"Не получилось распарсить uri: {uriStr}");
                })
            .WithName("Check");
    }
}