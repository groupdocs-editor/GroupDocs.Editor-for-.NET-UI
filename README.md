# Exploring GroupDocs.Editor.UI for .NET: A Rich UI Interface for Document Editing

![Build Packages](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/actions/workflows/build_packages.yml/badge.svg)
![Test ubuntu-latest](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/actions/workflows/Test_linux.yml/badge.svg)
![Test windows-latest](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/actions/workflows/Test_windows-latest.yml/badge.svg)
![Nuget](https://img.shields.io/nuget/v/GroupDocs.Editor.UI.Api?label=GroupDocs.Editor.UI)
![Nuget](https://img.shields.io/nuget/dt/GroupDocs.Editor.UI.Api?label=GroupDocs.Editor.UI)

# GroupDocs.Editor.UI for .NET

Welcome to the GroupDocs.Editor.UI for .NET repository! This project provides a comprehensive solution for integrating advanced document editing capabilities into your .NET applications. It supports a wide range of document formats, offering a robust and intuitive user interface for document manipulation.

## Overview

GroupDocs.Editor.UI for .NET allows developers to build web applications with powerful document editing features. It includes a RESTful API for backend services and customizable UI components for seamless integration.

### Key Features

- **Document Format Support**: Edit various document formats including Word, Excel, PowerPoint, PDF, and more.
- **RESTful API**: Efficient backend processes for loading, viewing, editing, and saving documents.
- **UI Components**: Customizable components for Angular and React frameworks.
- **High Performance**: Handles large documents efficiently with scalable architecture.
- **Security**: Supports authentication and data encryption for secure document processing.
- **Customization**: Extensive customization options for UI and API workflows.

## Samples

Explore the following samples to understand how to integrate and use GroupDocs.Editor.UI for .NET in different scenarios:

### 1. RESTful API Sample

- **[GroupDocs.Editor.UI.RestFulApi](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/tree/master/samples/GroupDocs.Editor.UI.RestFulApi)**

### 2. SPA Samples

- **[GroupDocs.Editor.UI.Email.SpaSample](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/tree/master/samples/GroupDocs.Editor.UI.Email.SpaSample)**
- **[GroupDocs.Editor.UI.Presentation.SpaSample](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/tree/master/samples/GroupDocs.Editor.UI.Presentation.SpaSample)**
- **[GroupDocs.Editor.UI.Spreadsheet.SpaSample](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/tree/master/samples/GroupDocs.Editor.UI.Spreadsheet.SpaSample)**
- **[GroupDocs.Editor.UI.SpaSample - Word Processing Example](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/tree/master/samples/GroupDocs.Editor.UI.SpaSample)**

### 3. React SPA Sample

- **[GroupDocs.Editor.UI.ReactSpaSample - React and 3rd-party WYSIWYG Editor](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/tree/master/samples/GroupDocs.Editor.UI.ReactSpaSample)**

### 4. Document Editor Angular Apps

- **[groupdocs-editor-ui-email-app](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/tree/master/samples/DocumentEditors/groupdocs-editor-ui-email-app)**
- **[groupdocs-editor-ui-presentation-app](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/tree/master/samples/DocumentEditors/groupdocs-editor-ui-presentation-app)**
- **[groupdocs-editor-ui-spreadsheet-app](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/tree/master/samples/DocumentEditors/groupdocs-editor-ui-spreadsheet-app)**
- **[groupdocs-editor-ui-wordprocessing-app](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/tree/master/samples/DocumentEditors/groupdocs-editor-ui-wordprocessing-app)**

## Getting Started

### Prerequisites

- .NET Framework or .NET Core
- Node.js and npm (for Angular/React UI components)
- Visual Studio or any other preferred development environment

### Installation

1. **Clone the Repository**:
    ```bash
    git clone https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI.git
    ```

2. **Configuration**: Configure the necessary settings by referring to the details provided in the [GroupDocs.Editor-for-.NET-UI Wiki](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/wiki). These settings include specifying the licensing type and source for the GroupDocs.Editor for .NET library.

3. **Install Dependencies**: The Angular part of the application is placed in the `ClientApp\` directory. To start, you need to run the C# project. Navigate to the project directory and run the C# project using your preferred development environment, such as Visual Studio:

   ```bash
   dotnet run
   ```

The application should now be accessible, allowing you to perform document editing with the integrated **groupdocs.editor.angular.ui-spreadsheet** as UI and the **GroupDocs.Editor.UI.Api** as underlying API.

## Features and Functionality

The **GroupDocs.Editor.UI.ReactSpaSample** offers a rich set of features and functionalities:

- **Document Editing**: Users can open, view, and edit various document formats with the feature-rich Document Editor.
- **RESTful API**: The API serves content and provides the necessary endpoints for document editing operations.
- **Configuration**: The application is highly configurable, allowing you to specify licensing and other settings as per your requirements.

## Supported Document Formats

The following formats are supported by GroupDocs.Editor with capabilities to create, import, and export:

### WordProcessing Formats

- **DOC**: MS Word 97-2007 Binary File Format
- **DOCX**: Office Open XML WordProcessingML Macro-Free Document
- **DOCM**: Office Open XML WordProcessingML Macro-Enabled Document
- **DOT**: MS Word 97-2007 Template
- **DOTX**: Office Open XML WordprocessingML Macro-Free Template
- **DOTM**: Office Open XML WordprocessingML Macro-Enabled Template
- **FlatOPC**: Office Open XML WordprocessingML stored in a flat XML file
- **ODT**: Open Document Format Text Document
- **OTT**: Open Document Format Text Document Template
- **RTF**: Rich Text Format
- **WordML**: Microsoft Office Word 2003 XML Format — WordProcessingML or WordML

### Spreadsheet Formats

- **XLS**: Excel 97-2003 Binary File Format
- **XLT**: Excel 97-2003 Template
- **XLSX**: Office Open XML Workbook Macro-Free
- **XLSM**: Office Open XML Workbook Macro-Enabled
- **XLTX**: Office Open XML Template Macro-Free
- **XLTM**: Office Open XML Template Macro-Enabled
- **XLSB**: Excel Binary Workbook
- **XLAM**: Excel Add-in
- **SpreadsheetML**: Microsoft Office Excel 2002 and Excel 2003 XML Format
- **ODS**: OpenDocument Spreadsheet
- **FODS**: Flat OpenDocument Spreadsheet — stored as a single XML document
- **SXC**: StarOffice or OpenOffice.org Calc XML Spreadsheet
- **CSV**: Comma Separated Values document
- **TSV**: Tab Separated Values document

### Presentation Formats

- **PPT**: Microsoft PowerPoint 95 Presentation
- **PPT**: Microsoft PowerPoint 97-2003 Presentation
- **PPTX**: Microsoft Office Open XML PresentationML Macro-Free Document
- **PPTM**: Microsoft Office Open XML PresentationML Macro-Enabled Document
- **PPS**: Microsoft PowerPoint 97-2003 SlideShow
- **PPSX**: Microsoft Office Open XML PresentationML Macro-Free SlideShow
- **PPSM**: Microsoft Office Open XML PresentationML Macro-Enabled SlideShow
- **POT**: Microsoft PowerPoint 97-2003 Presentation Template
- **POTX**: Microsoft Office Open XML PresentationML Macro-Free Template
- **POTM**: Microsoft Office Open XML PresentationML Macro-Enabled Template
- **ODP**: OpenDocument Presentation
- **OTP**: OpenDocument Presentation template

### Fixed-layout Formats

- **PDF**: Portable Document Format

### Email Formats

- **EML**: RFC-822 Internet Message Format Standard
- **EMLX**: Apple Mail App format
- **MSG**: Microsoft Outlook and Exchange email format
- **MBOX**: Container for collection of electronic mail messages
- **TNEF**: Transport Neutral Encapsulation Format
- **MHT**: MIME encapsulation of aggregate HTML documents
- **PST**: Personal Storage Table
- **OFT**: Outlook MSG file format for message template
- **OST**: Offline Storage Table

### eBook Formats

- **ePub**: Electronic Publication
- **MOBI**: MobiPocket
- **AZW3**: AZW3, also known as Kindle Format 8 (KF8)

### Markup Formats

- **MD**: Markdown

### Other Formats

- **TXT**: Plain Text

## Contributing

Contributions to this project are welcome. To contribute:

1. Fork the repository.
2. Create a new branch for your feature or bugfix.
3. Make your changes and commit them with clear messages.
4. Open a pull request to the main repository.

When contributing, follow these key steps:

1. Adhere to the code guidelines and conventions.
2. Ensure your pull requests are well-documented, describing the changes you've made and the problems you're addressing.

## Resources

To make the most of GroupDocs.Editor.UI for .NET and its associated resources, here are some useful links:

- **GitHub Repository**: [GroupDocs.Editor-for-.NET-UI](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI)
- **GroupDocs Documentation**: [Detailed documentation and guides](https://docs.groupdocs.com/editor/net/) for integrating and using GroupDocs.Editor for .NET.
- **NuGet Package**: Find the latest NuGet package for [GroupDocs.Editor.UI.Api](https://www.nuget.org/packages/GroupDocs.Editor.UI.Api) to enhance your document editing capabilities.
- **Examples and Samples**: Explore the provided [examples](https://github.com/groupdocs-editor/GroupDocs.Editor-for-.NET-UI/tree/master/samples) to kickstart your document editing projects.
- **Website**: [www.groupdocs.com](http://www.groupdocs.com)
- **Product Home**: [GroupDocs.Editor](https://products.groupdocs.com/editor)
- **Download**: [Download GroupDocs.Editor](http://downloads.groupdocs.com/editor)
- **Free Support Forum**: [GroupDocs.Editor Free Support Forum](https://forum.groupdocs.com/c/editor)
- **Paid Support Helpdesk**: [GroupDocs.Editor Paid Support Helpdesk](https://helpdesk.groupdocs.com)
- **Blog**: [GroupDocs.Editor Blog](https://blog.groupdocs.com/category/groupdocs-editor-product-family/)

