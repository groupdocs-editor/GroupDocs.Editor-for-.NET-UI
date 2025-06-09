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
    /// Adds a license service for the GroupDocs.Editor for .NET.
    /// You can choose from available license services such as <see cref="Base64FileLicenseService"/>,
    /// <see cref="RemoteUrlFileLicenseService"/>, or <see cref="LocalFileLicenseService"/> with the corresponding
    /// <see cref="LicenseOptions"/> by specifying the <see cref="LicenseSourceType"/>.
    /// </summary>
    /// <typeparam name="T">The type of the license service. Choose from <see cref="Base64FileLicenseService"/>,
    /// <see cref="RemoteUrlFileLicenseService"/>, or <see cref="LocalFileLicenseService"/>.</typeparam>
    /// <param name="services">The services' collection.</param>
    /// <param name="configuration">The configuration containing license options.</param>
    /// <returns>The modified services collection.</returns>
    public static IServiceCollection AddEditorLicense<T>(
        this IServiceCollection services, IConfiguration configuration) where T : LicenseServiceBase<T>
    {
        services.Configure<LicenseOptions>(configuration.GetSection(nameof(LicenseOptions)));
        return services.AddHostedService<T>();
    }

    /// <summary>
    /// Adds DI service required by the editor in trial mode.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    public static IServiceCollection AddEditor<T>(
        this IServiceCollection services, IConfiguration configuration) where T : IStorage
    {
        services.AddAutoMapper(typeof(DocumentInfoProfile));
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddSingleton<IIdGeneratorService, IdGeneratorService>();
        var featureManager = (IFeatureManager)services.BuildServiceProvider().GetService(typeof(IFeatureManager))!;
        if (featureManager.IsEnabledAsync(EditorProductFamily.WordProcessing).Result)
        {
            services.AddScoped<IWordProcessingStorageCache, WordProcessingStorageCache>();
            services.AddTransient<IWordProcessingEditorService, WordProcessingEditorService>();
        }
        if (featureManager.IsEnabledAsync(EditorProductFamily.Presentation).Result)
        {
            services.AddScoped<IPresentationStorageCache, PresentationStorageCache>();
            services.AddTransient<IPresentationEditorService, PresentationEditorService>();
        }
        if (featureManager.IsEnabledAsync(EditorProductFamily.Spreadsheet).Result)
        {
            services.AddScoped<ISpreadsheetStorageCache, SpreadsheetStorageCache>();
            services.AddTransient<ISpreadsheetEditorService, SpreadsheetEditorService>();
        }
        if (featureManager.IsEnabledAsync(EditorProductFamily.Pdf).Result)
        {
            services.AddScoped<IPdfStorageCache, PdfStorageCache>();
            services.AddTransient<IPdfEditorService, PdfEditorService>();
        }
        if (featureManager.IsEnabledAsync(EditorProductFamily.Email).Result)
        {
            services.AddScoped<IEmailStorageCache, EmailStorageCache>();
            services.AddTransient<IEmailEditorService, EmailEditorService>();
        }

        services.AddScoped(typeof(IStorage), typeof(T));
        if (typeof(T) == typeof(AwsS3Storage))
        {
            services.Configure<AwsOptions>(configuration.GetSection("AWS"));
        }
        if (typeof(T) == typeof(LocalStorage))
        {
            services.Configure<LocalStorageOptions>(configuration.GetSection(nameof(LocalStorageOptions)));
        }
        if (typeof(T) == typeof(AzureBlobStorage))
        {
            services.Configure<AzureBlobOptions>(configuration.GetSection(nameof(AzureBlobOptions)));
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
        IMvcBuilder mvcBuilder;
        services.AddFeatureManagement();

        var featureManager = services.BuildServiceProvider().GetRequiredService<IFeatureManager>();
        if (mvcOption == null)
        {
            mvcBuilder = services.AddControllers(opt =>
            {
                opt.Conventions.Add(new ActionHidingConvention(featureManager));
            });
        }
        else
        {
            MvcOptions options = new MvcOptions();
            options.Conventions.Add(new ActionHidingConvention(featureManager));
            mvcOption.Invoke(options);
            mvcBuilder = services.AddControllers(mvcOption);
        }

        if (jsonOption == null)
        {
            mvcBuilder.AddJsonOptions(option =>
            {
                option.JsonSerializerOptions.Converters.Add(new FormatJsonConverter());
                option.JsonSerializerOptions.Converters.Add(new PresentationFormatsJsonConverter());
                option.JsonSerializerOptions.Converters.Add(new WordProcessingFormatJsonConverter());
                option.JsonSerializerOptions.Converters.Add(new SpreadsheetFormatJsonConverter());
                option.JsonSerializerOptions.Converters.Add(new EmailFormatJsonConverter());
            });
        }
        else
        {
            JsonOptions options = new();
            options.JsonSerializerOptions.Converters.Add(new FormatJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new PresentationFormatsJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new WordProcessingFormatJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new SpreadsheetFormatJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new EmailFormatJsonConverter());
            jsonOption.Invoke(options);
            mvcBuilder.AddJsonOptions(jsonOption);
        }

        return services;
    }
}