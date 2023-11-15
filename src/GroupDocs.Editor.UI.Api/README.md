# Exploring the UI for GroupDocs.Editor for .NET

![Build Packages](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/actions/workflows/build_packages.yml/badge.svg)
![Test ubuntu-latest](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/actions/workflows/Test_linux.yml/badge.svg)
![Test windows-latest](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/actions/workflows/Test_windows-latest.yml/badge.svg)
![Nuget](https://img.shields.io/nuget/v/groupdocs.editor.ui?label=GroupDocs.Editor.UI)
![Nuget](https://img.shields.io/nuget/dt/groupdocs.editor.ui?label=GroupDocs.Editor.UI)

GroupDocs.Editor UI is an essential interface that complements the [GroupDocs.Editor for .NET](https://products.groupdocs.com/editor/net) library, offering a feature-rich platform for displaying a wide range of popular word-processing formats (such as DOC, DOCX, RTF, ODT, and more) directly within a web browser. This article will provide insights into the capabilities and resources offered by the GroupDocs.Editor for .NET UI. You can find the primary repository for this UI interface at [GroupDocs.Editor-for-.NET-UI](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI).

## Overview of GroupDocs.Editor.UI

GroupDocs.Editor UI is designed to seamlessly collaborate with the GroupDocs.Editor for .NET library, extending the document processing capabilities for .NET applications. It delivers an interactive interface, enabling users to view and edit word-processing documents of various formats directly within a web browser.

## Repository Highlights

### 1. Creating a Web API App

The GroupDocs.Editor.UI repository provides the code for creating a Web API app using the NuGet package `GroupDocs.Editor.UI.Api`. You can install this package using the following PowerShell command:

```PowerShell
dotnet add package GroupDocs.Editor.UI.Api
```

To integrate this package into your ASP.NET Core project, add the necessary services and middleware in your `Startup` class, as demonstrated below:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEditorControllers();
builder.Services.AddEditorSwagger();
builder.Services.AddEditor<LocalStorage>(builder.Configuration);
```

## User Interface (UI)

The UI is an Angular application built on top of the [`@groupdocs/groupdocs.editor.angular.ui-wordprocessing`](https://www.npmjs.com/package/@groupdocs/groupdocs.editor.angular.ui-wordprocessing) package.

## API Integration

The API is a critical component used to serve content, allowing users to open, view, edit, and save word-processing documents. The API can be hosted within the same application or in a separate one. Currently, the following API implementations are available:

- [GroupDocs.Editor.UI.Api](dotnet add package GroupDocs.Editor.UI.Api)

All API implementations are extensions of `IMvcBuilder`.

## Licensing

To use GroupDocs.Editor for .NET without trial limitations, you need a valid license. To request a temporary license and try the GroupDocs.Editor library before buying it, visit the [Get a Temporary License](https://purchase.groupdocs.com/temporary-license) page. 

Here's how you can set a license in the `appsettings.json` file:

```json
"LicenseOptions": {
    "Type": 1,
    "Source": "https://docs.groupdocs.com/editor/net/licensing-and-subscription/"
}
```

The `Type` field corresponds to different license sources, including local path, remote URL, and base64 string.

## Linux Dependencies

When running the API on Linux or in a Docker container, specific packages need to be installed, as shown below:

```bash
RUN apt-get update && apt-get install -y libgdiplus

RUN sed -i'.bak' 's/$/ contrib/' /etc/apt/sources.list
RUN apt-get update; apt-get install -y ttf-mscorefonts-installer fontconfig
RUN fc-cache -f -v
```

## Amazon S3 Storage Configuration

Provides instructions on configuring Amazon S3 for file storage within your application. Follow the steps below to integrate Amazon S3 Storage into your project.

### 1. Add Amazon S3 Storage Service in `Startup` Class

In your `Startup` class, add the Amazon S3 Storage service using the following code:

```csharp
builder.Services.AddEditor<AwsS3Storage>(builder.Configuration);
```

This code adds the necessary services for Amazon S3 Storage, utilizing configuration settings defined in the `appsettings.json` file.

### 2. Configure Amazon S3 Storage Options

In the `appsettings.json` file, specify the configuration options for Amazon S3 Storage under the "AWS" section:

```json
"AWS": {
    "Profile": "your-aws-profile",
    "Bucket": "your-bucket-name",
    "RootFolderName": "your-root-folder-name",
    "LinkExpiresDays": 360,
    "Region": "your-region",
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key"
}
```

Replace placeholders like "your-aws-profile," "your-bucket-name," "your-root-folder-name," "your-region," "your-access-key," and "your-secret-key" with your actual Amazon S3 Storage details.

### Configuration Options:

- **Profile:** Your AWS profile name.
- **Bucket:** The name of your S3 bucket.
- **RootFolderName:** The name of the root folder within your S3 bucket.
- **LinkExpiresDays:** The number of days before the generated links expire.
- **Region:** The AWS region where your S3 bucket is located.
- **AccessKey:** Your AWS access key.
- **SecretKey:** Your AWS secret key.

By following these steps, you will have successfully configured Amazon S3 Storage for your application. Ensure that the configuration options match your specific Amazon S3 Storage account details and adjust them accordingly.

## Azure Blob Storage Configuration

This document provides guidance on configuring Azure Blob Storage for file storage within your application. Follow the steps below to integrate Azure Blob Storage into your project.

### 1. Add Azure Blob Storage Service in `Startup` Class

In your `Startup` class, use the following code to add the Azure Blob Storage service:

```csharp
builder.Services.AddEditor<AzureBlobStorage>(builder.Configuration);
```

This line of code adds the necessary services for Azure Blob Storage, and it uses configuration settings defined in the `appsettings.json` file.

### 2. Configure Azure Blob Storage Options

In the `appsettings.json` file, specify the configuration options for Azure Blob Storage under the "AzureBlobOptions" section:

```json
"AzureBlobOptions": {
    "AccountName": "your-account-name",
    "AccountKey": "your-account-key",
    "ContainerName": "your-container-name",
    "LinkExpiresDays": 360
}
```

Make sure to replace placeholders like "your-account-name," "your-account-key," and "your-container-name" with your actual Azure Blob Storage account details.

#### Configuration Options:

- **AccountName:** Your Azure Blob Storage account name.
- **AccountKey:** Your Azure Blob Storage account key.
- **ContainerName:** The name of the container where your files will be stored.
- **LinkExpiresDays:** The number of days before the generated links expire.

## API Storage Providers

If you opt for local storage, use the following code to set up a local file storage provider:

```csharp
builder.Services.AddEditor<LocalStorage>(builder.Configuration);
```

In the `appsettings.json`, you can specify the root folder for file storage and the base URL for reading files:

```json
"LocalStorageOptions": {
    "RootFolder": "pathToStorage",
    "BaseUrl": "https://yourBaseUrl"
}
```

## Feature Management Configuration

You can manage features through the "FeatureManagement" section in the `appsettings.json` file. Enable or disable specific features according to your requirements:

```json
"FeatureManagement": {
    "WordProcessing": true,
    "LocalFile": true,
    "Pdf": false
}
```

### Feature Descriptions:

- **WordProcessing:** Enables processing of documents in the WordProcessing format family.
- **LocalFile:** Enables or disables endpoints based on whether you are using AWS S3 or Azure Blob. If set to `true`, it assumes Azure Blob Storage or AWS S3 is used; if set to `false`, it assumes local filesystem is used.
- **Pdf:** Allows processing of PDF files. Set to `true` to enable, and `false` to disable.

**Note:** Ensure that the feature configurations align with your intended usage and storage provider (Azure Blob or AWS S3).

## Contributing

Contributions are encouraged to improve the project by adding new features, making enhancements, or fixing bugs. Here are the key steps to follow when contributing:

1. Familiarize yourself with the [Don't push your pull requests](https://www.igvita.com/2011/12/19/dont-push-your-pull-requests/) guideline.
2. Adhere to the code guidelines and conventions.
3. Ensure that your pull requests are well-documented and describe the changes thoroughly.

With these guidelines in mind, you can actively contribute to the enhancement and development of the GroupDocs.Editor.UI for .NET project.

GroupDocs.Editor UI for .NET enhances document editing capabilities and provides a seamless user experience for developers and end-users alike. Whether you're building web applications, exploring storage options, or extending the capabilities of your .NET application, GroupDocs.Editor.UI offers a rich interface that simplifies document editing and processing tasks.