﻿using GroupDocs.Editor.Options;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GroupDocs.Editor.UI.Api.Controllers.RequestModels;

public class WordToPdfDownloadRequest
{
    /// <summary>
    /// Gets or sets the document code.
    /// </summary>
    /// <value>
    /// The document code.
    /// </value>
    [Required]
    [FromQuery] public Guid DocumentCode { get; set; }

    /// <summary>
    /// Gets or sets the save options.
    /// </summary>
    /// <value>
    /// The save options.
    /// </value>
    [Required]
    public PdfSaveOptions SaveOptions { get; set; }
}