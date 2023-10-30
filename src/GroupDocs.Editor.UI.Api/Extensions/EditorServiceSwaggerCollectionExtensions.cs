using GroupDocs.Editor.UI.Api.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace GroupDocs.Editor.UI.Api.Extensions;

public static class EditorServiceSwaggerCollectionExtensions
{
    /// <summary>
    /// Adds the swagger functionality to editor.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="setupAction">The setup action.</param>
    /// <returns></returns>
    public static IServiceCollection AddEditorSwagger(
        this IServiceCollection services, Action<SwaggerGenOptions>? setupAction = null)
    {
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        SwaggerGenOptions swaggerGenOptions = new();
        swaggerGenOptions.SwaggerDoc("v1",
            new OpenApiInfo
            {
                Title = "GroupDocs.Editor.UI Api",
                Version = "v1",
                Contact = new OpenApiContact { Url = new Uri("https://docs.groupdocs.com/editor/net/"), Name = "GroupDocs.Editor for .NET" },
                Description = "Edit Word documents using GroupDocs.Editor for .NET powerful document editing API. It can be used with any external, open source or paid HTML editor.",
                TermsOfService = new Uri("https://about.groupdocs.com/legal/terms-of-use/"),
                License = new OpenApiLicense { Name = "Metered licenses", Url = new Uri("https://docs.groupdocs.com/editor/net/licensing-and-subscription/") }
            });
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlFilename2 = $"{typeof(EditorService).Assembly.GetName().Name}.xml";
        swaggerGenOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        swaggerGenOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename2));
        setupAction ??= _ => { };
        setupAction.Invoke(swaggerGenOptions);
        services.AddSwaggerGen(setupAction);

        return services;
    }
}