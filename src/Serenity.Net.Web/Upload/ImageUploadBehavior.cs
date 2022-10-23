﻿using Serenity.Web;

namespace Serenity.Services;

[Obsolete("Use Serenity.Services.FileUploadBehavior")]
public abstract class ImageUploadBehavior : FileUploadBehavior
{
    public ImageUploadBehavior(IUploadStorage storage, ITextLocalizer localizer, IExceptionLogger logger = null)
        : base(storage, localizer, logger)
    {
    }
}