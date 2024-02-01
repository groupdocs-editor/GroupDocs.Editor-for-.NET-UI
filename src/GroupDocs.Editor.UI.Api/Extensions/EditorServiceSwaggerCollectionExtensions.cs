using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

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

        if (setupAction == null)
        {
            services.AddSwaggerGen(opt =>
            {
                opt.CustomOperationIds(e =>
                {
                    string httpMethod = e.HttpMethod == null ? "" : $"{char.ToUpper(e.HttpMethod[0])}{e.HttpMethod[1..].ToLower()}";
                    if (e.GroupName != null && e.GroupName.Equals(EditorProductFamily.WordProcessing, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return $"{e.ActionDescriptor.RouteValues["action"]}_{e.ActionDescriptor.RouteValues["controller"]}_{httpMethod}";
                    }
                    return $"{e.ActionDescriptor.RouteValues["action"]}_{httpMethod}";

                });
                opt.SwaggerDoc(EditorProductFamily.WordProcessing, GetInfo(EditorProductFamily.WordProcessing));
                opt.SwaggerDoc(EditorProductFamily.LocalFile, GetInfo(EditorProductFamily.LocalFile));
                opt.SwaggerDoc(EditorProductFamily.Presentation, GetInfo(EditorProductFamily.Presentation));
                opt.SwaggerDoc(EditorProductFamily.Spreadsheet, GetInfo(EditorProductFamily.Spreadsheet));
            });
        }
        else
        {
            SwaggerGenOptions swaggerGenOptions = new();
            swaggerGenOptions.CustomOperationIds(e =>
            {
                string httpMethod = e.HttpMethod == null ? "" : $"{char.ToUpper(e.HttpMethod[0])}{e.HttpMethod[1..].ToLower()}";
                if (e.GroupName != null && e.GroupName.Equals(EditorProductFamily.WordProcessing, StringComparison.InvariantCultureIgnoreCase))
                {
                    return $"{e.ActionDescriptor.RouteValues["action"]}_{e.ActionDescriptor.RouteValues["controller"]}_{httpMethod}";
                }
                return $"{e.ActionDescriptor.RouteValues["action"]}_{httpMethod}";

            });
            swaggerGenOptions.SwaggerDoc(EditorProductFamily.WordProcessing, GetInfo(EditorProductFamily.WordProcessing));
            swaggerGenOptions.SwaggerDoc(EditorProductFamily.LocalFile, GetInfo(EditorProductFamily.LocalFile));
            swaggerGenOptions.SwaggerDoc(EditorProductFamily.Presentation, GetInfo(EditorProductFamily.Presentation));
            setupAction.Invoke(swaggerGenOptions);
            services.AddSwaggerGen(setupAction);
        }

        return services;
    }

    public static OpenApiInfo GetInfo(string version)
    {
        return new OpenApiInfo
        {
            Title = $"{version} GroupDocs.Editor.UI Api",
            Version = version,
            Contact = new OpenApiContact
            { Url = new Uri("https://docs.groupdocs.com/editor/net/"), Name = "GroupDocs.Editor for .NET" },
            Description =
                "Edit Word documents using GroupDocs.Editor for .NET powerful document editing API. It can be used with any external, open source or paid HTML editor.",
            TermsOfService = new Uri("https://about.groupdocs.com/legal/terms-of-use/"),
            License = new OpenApiLicense
            {
                Name = "Metered licenses",
                Url = new Uri("https://docs.groupdocs.com/editor/net/licensing-and-subscription/")
            }
        };
    }

    public static IApplicationBuilder UseEditorSwaggerUI(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            var featureManager = app.ApplicationServices.GetRequiredService<IFeatureManager>();
            if (featureManager.IsEnabledAsync(EditorProductFamily.WordProcessing).Result)
            {
                c.SwaggerEndpoint($"{EditorProductFamily.WordProcessing}/swagger.json", $"{EditorProductFamily.WordProcessing} API");
            }
            if (featureManager.IsEnabledAsync(EditorProductFamily.LocalFile).Result)
            {
                c.SwaggerEndpoint($"{EditorProductFamily.LocalFile}/swagger.json", $"{EditorProductFamily.LocalFile} API");
            }
            if (featureManager.IsEnabledAsync(EditorProductFamily.Presentation).Result)
            {
                c.SwaggerEndpoint($"{EditorProductFamily.Presentation}/swagger.json", $"{EditorProductFamily.Presentation} API");
            }
            if (featureManager.IsEnabledAsync(EditorProductFamily.Spreadsheet).Result)
            {
                c.SwaggerEndpoint($"{EditorProductFamily.Spreadsheet}/swagger.json", $"{EditorProductFamily.Spreadsheet} API");
            }
        });
        return app;
    }
}