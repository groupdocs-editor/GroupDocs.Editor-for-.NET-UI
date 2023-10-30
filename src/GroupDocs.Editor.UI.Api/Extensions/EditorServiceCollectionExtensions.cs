using Amazon.S3;
using GroupDocs.Editor.UI.Api.AutoMapperProfiles;
using GroupDocs.Editor.UI.Api.JsonConverters;
using GroupDocs.Editor.UI.Api.Services.Implementation;
using GroupDocs.Editor.UI.Api.Services.Interfaces;
using GroupDocs.Editor.UI.Api.Services.Licensing;
using GroupDocs.Editor.UI.Api.Services.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace GroupDocs.Editor.UI.Api.Extensions;

public static class EditorServiceCollectionExtensions
{
    /// <summary>
    /// Adds DI service required by the editor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    public static IServiceCollection AddEditor<T>(
        this IServiceCollection services, IConfiguration configuration) where T : IStorage
    {
        services.Configure<LicenseOptions>(configuration.GetSection(nameof(LicenseOptions)));
        services.AddHostedService<RemoteUrlFileLicenseService>();
        return services.AddEditorTrial<T>(configuration);
    }

    /// <summary>
    /// Adds DI service required by the editor in trial mode.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    public static IServiceCollection AddEditorTrial<T>(
        this IServiceCollection services, IConfiguration configuration) where T : IStorage
    {
        services.AddFeatureManagement(configuration.GetSection("EditorProductFamily"));
        services.AddSingleton<IIdGeneratorService, IdGeneratorService>();
        services.AddAutoMapper(typeof(DocumentInfoProfile));
        services.AddMemoryCache();
        services.AddScoped<IMetaFileStorageCache, MetaFileStorageCache>();
        services.AddTransient<IEditorService, EditorService>();
        services.AddScoped(typeof(IStorage), typeof(T));
        if (typeof(T) == typeof(AwsS3Storage))
        {
            services.AddDefaultAWSOptions(configuration.GetAWSOptions());
            services.AddAWSService<IAmazonS3>();
            services.Configure<AwsOptions>(configuration.GetSection("AWS"));

        }

        if (typeof(T) == typeof(LocalStorage))
        {
            services.Configure<LocalStorageOptions>(configuration.GetSection(nameof(LocalStorageOptions)));
        }

        return services;
    }

    /// <summary>
    /// Adds the editor controllers with default JsonSerializerOptions.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="jsonOption">The json option.</param>
    /// <param name="mvcOption">The MVC option.</param>
    /// <returns></returns>
    public static IServiceCollection AddEditorControllers(
        this IServiceCollection services, Action<JsonOptions>? jsonOption = null, Action<MvcOptions>? mvcOption = null)
    {

        IMvcBuilder mvcBuilder = mvcOption != null ? services.AddControllers(mvcOption) : services.AddControllers();
        if (jsonOption == null)
        {
            mvcBuilder.AddJsonOptions(option =>
            {
                option.JsonSerializerOptions.Converters.Add(new FormatJsonConverter());
                option.JsonSerializerOptions.Converters.Add(new WordProcessingFormatJsonConverter());
            });
        }
        else
        {
            JsonOptions options = new();
            options.JsonSerializerOptions.Converters.Add(new FormatJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new WordProcessingFormatJsonConverter());
            jsonOption.Invoke(options);
            mvcBuilder.AddJsonOptions(jsonOption);
        }

        return services;
    }
}

public static class EditorProductFamily
{
    public const string WordProcessing = "WordProcessing";
    public const string Fixed = "Fixed";
    public const string Presentation = "Presentation";
}