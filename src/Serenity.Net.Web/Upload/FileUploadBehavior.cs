﻿using Serenity.Web;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.IO;

namespace Serenity.Services
{
    public class FileUploadBehavior : BaseSaveDeleteBehavior, IImplicitBehavior, IFieldBehavior
    {
        public Field Target { get; set; }

        private IUploadEditor editorAttr;
        private string fileNameFormat;
        private const string SplittedFormat = "{1:00000}/{0:00000000}_{2}";
        private readonly ITextLocalizer localizer;
        private readonly IExceptionLogger logger;
        private readonly IUploadStorage storage;
        private StringField originalNameField;
        private Dictionary<string, Field> replaceFields;

        public FileUploadBehavior(IUploadStorage storage,
            ITextLocalizer localizer, 
            IExceptionLogger logger = null)
        {
            this.storage = storage;
            this.localizer = localizer;
            this.logger = logger;
        }

        public bool ActivateFor(IRow row)
        {
            if (Target is null)
                return false;

            editorAttr = Target.CustomAttributes.OfType<IUploadEditor>().FirstOrDefault();
            if (editorAttr is null || editorAttr.DisableDefaultBehavior || editorAttr.IsMultiple)
                return false;

            if (Target is not StringField)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Field '{0}' on row type '{1}' has a UploadEditor attribute but it is not a String field!",
                        Target.PropertyName ?? Target.Name, row.GetType().FullName));

            if (row is not IIdRow)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Field '{0}' on row type '{1}' has a UploadEditor attribute but Row type doesn't implement IIdRow!",
                        Target.PropertyName ?? Target.Name, row.GetType().FullName));

            var originalNameProperty = (editorAttr as IUploadFileOptions)?.OriginalNameProperty;
            if (!string.IsNullOrEmpty(originalNameProperty))
            {
                var nameField = row.FindFieldByPropertyName(originalNameProperty) ??
                    row.FindField(originalNameProperty);

                originalNameField = (StringField)nameField ?? throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Field '{0}' on row type '{1}' has a UploadEditor attribute but " +
                    "a field with OriginalNameProperty '{2}' is not found!",
                    Target.PropertyName ?? Target.Name, 
                    row.GetType().FullName,
                    originalNameProperty));
            }

            var format = (editorAttr as IUploadFileOptions)?.FilenameFormat;
            if (format == null)
            {
                format = row.GetType().Name;
                if (format.EndsWith("Row", StringComparison.Ordinal))
                    format = format[..^3];
                format += "/~";
            }

            fileNameFormat = format.Replace("~", SplittedFormat, StringComparison.Ordinal);
            replaceFields = ParseReplaceFields(fileNameFormat, row, Target);

            return true;
        }

        internal static Dictionary<string, Field> ParseReplaceFields(string fileNameFormat, IRow row, Field target)
        {
            if (fileNameFormat.IndexOf('|', StringComparison.Ordinal) < 0)
                return null;

            var replaceFields = new Dictionary<string, Field>();

            int start = 0;
            while ((start = fileNameFormat.IndexOf('|', start)) >= 0)
            {
                var end = fileNameFormat.IndexOf('|', start + 1);
                if (end <= start + 1)
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                        "Field '{0}' on row type '{1}' has a UploadEditor attribute " +
                        "with invalid format string '{2}'!",
                            target.PropertyName ?? target.Name,
                            row.GetType().FullName,
                            fileNameFormat));

                var fieldName = fileNameFormat.Substring(start + 1, end - start - 1);
                var actualName = fieldName;
                var colon = fieldName.IndexOf(":", StringComparison.Ordinal);
                if (colon >= 0)
                    actualName = fieldName.Substring(0, colon);

                var replaceField = row.FindFieldByPropertyName(actualName) ??
                    row.FindField(actualName);

                if (replaceField is null)
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                        "Field '{0}' on row type '{1}' has a UploadEditor attribute that " +
                        "references field '{2}', but no such field is found!'",
                            target.PropertyName ?? target.Name,
                            row.GetType().FullName,
                            actualName));
                }

                replaceFields['|' + fieldName + '|'] = replaceField;

                start = end + 1;
            }

            return replaceFields;
        }

        internal static string ProcessReplaceFields(string s, Dictionary<string, Field> replaceFields, ISaveRequestHandler handler)
        {
            if (replaceFields == null)
                return s;

            var row = handler.Row;

            // foreign / calculated fields might not be available yet in new row
            // so load them from database 

            // TODO: if referenced foreign fields changed on update, 
            // values might be wrong in before update where we set filename
            // so need to handle update in AfterSave just like create

            if (handler.IsCreate &&
                replaceFields.Values.Any(x => !x.IsTableField()))
            {
                var idField = ((IIdRow)handler.Row).IdField;

                row = handler.Row.Clone();
                var query = new SqlQuery()
                    .From(row);

                foreach (var field in replaceFields.Values)
                    query.Select(field);

                query.Where(idField == new ValueCriteria(idField.AsObject(row)));

                query.GetFirst(handler.Connection);
            }

            foreach (var p in replaceFields)
            {
                var val = p.Value.AsObject(row);
                string str;

                var colon = p.Key.IndexOf(":", StringComparison.Ordinal);
                if (colon >= 0)
                    str = string.Format(CultureInfo.InvariantCulture, "{0:" + p.Key.Substring(colon + 1, p.Key.Length - colon - 2) + "}", val);
                else
                    str = Convert.ToString(val ?? "", CultureInfo.InvariantCulture);

                str = StringHelper.SanitizeFilename(str).Replace('\\', '_').Replace("..", "_", StringComparison.Ordinal);
                if (string.IsNullOrWhiteSpace(str))
                    str = "_";

                while (str.EndsWith(".", StringComparison.Ordinal))
                    str = str[..^1] + "_";

                s = s.Replace(p.Key, str, StringComparison.Ordinal);
            }

            while (s.IndexOf("//", StringComparison.Ordinal) > 0)
                s = s.Replace("//", "/_/", StringComparison.Ordinal);

            return s;
        }

        public override void OnPrepareQuery(ISaveRequestHandler handler, SqlQuery query)
        {
            base.OnPrepareQuery(handler, query);

            if (replaceFields != null)
            {
                foreach (var field in replaceFields.Values)
                {
                    if (!field.IsTableField() &&
                        (query is not ISqlQueryExtensible ex ||
                          ex.GetSelectIntoIndex(field) <= 0))
                        query.Select(field);
                }
            }
        }

        public override void OnBeforeSave(ISaveRequestHandler handler)
        {
            var filesToDelete = new FilesToDelete(storage);
            handler.UnitOfWork.RegisterFilesToDelete(filesToDelete);
            handler.StateBag[GetType().FullName + "_" + Target.Name + "_FilesToDelete"] = filesToDelete;

            var filename = (StringField)Target;
            var oldFilename = handler.IsCreate ? null : filename[handler.Old];
            var newFilename = filename[handler.Row] = filename[handler.Row].TrimToNull();

            if (oldFilename.IsTrimmedSame(newFilename))
            {
                filename[handler.Row] = oldFilename;
                return;
            }

            DeleteOldFile(storage, filesToDelete, oldFilename, 
                copyToHistory: (editorAttr as IUploadFileOptions)?.CopyToHistory == true);

            if (newFilename == null)
            {
                if (oldFilename.IsTrimmedEmpty())
                    return;

                filename[handler.Row] = null;

                if (!(originalNameField is null))
                    originalNameField[handler.Row] = null;

                return;
            }

            if (!newFilename.StartsWith("temporary/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("For security reasons, only temporary files can be used in uploads!");

            if (originalNameField is not null)
            {
                var originalName = storage.GetOriginalName(newFilename).TrimToNull();

                originalNameField[handler.Row] = originalName;
            }

            if (handler.IsUpdate)
            {
                var copyResult = CopyTemporaryFile(handler, filesToDelete);
                filename[handler.Row] = copyResult.Path;
            }
        }

        internal static void DeleteOldFile(IUploadStorage storage, FilesToDelete filesToDelete, string oldFilename, bool copyToHistory)
        {
            if (!oldFilename.IsEmptyOrNull())
            {
                filesToDelete.RegisterOldFile(oldFilename);

                if (copyToHistory)
                {
                    if (storage.FileExists(oldFilename))
                        storage.ArchiveFile(oldFilename);
                }
            }
        }

        public override void OnAfterDelete(IDeleteRequestHandler handler)
        {
            if (handler.Row is IIsActiveDeletedRow or IIsDeletedRow or IDeleteLogRow)
                return;

            var filename = (StringField)Target;
            var oldFilename = filename[handler.Row];
            var filesToDelete = new FilesToDelete(storage);
            handler.UnitOfWork.RegisterFilesToDelete(filesToDelete);

            DeleteOldFile(storage, filesToDelete, oldFilename, 
                copyToHistory: (editorAttr as IUploadFileOptions)?.CopyToHistory == true);
        }

        private CopyTemporaryFileResult CopyTemporaryFile(ISaveRequestHandler handler, IFilesToDelete filesToDelete)
        {
            var fileName = (StringField)Target;
            var newFilename = fileName[handler.Row] = fileName[handler.Row].TrimToNull();
            CheckUploadedImageAndCreateThumbs(editorAttr, localizer, storage, ref newFilename, logger);

            var idField = ((IIdRow)handler.Row).IdField;
            var originalName = storage.GetOriginalName(newFilename);
            if (string.IsNullOrEmpty(originalName))
                originalName = Path.GetFileName(newFilename);

            var copyResult = storage.CopyTemporaryFile(new CopyTemporaryFileOptions
            {
                Format = fileNameFormat,
                PostFormat = s => ProcessReplaceFields(s, replaceFields, handler),
                TemporaryFile = newFilename,
                EntityId = idField.AsObject(handler.Row),
                FilesToDelete = filesToDelete,
                OriginalName = originalName 
            });

            return copyResult;
        }

        public override void OnAfterSave(ISaveRequestHandler handler)
        {
            var filename = (StringField)Target;

            if (handler.IsUpdate)
                return;

            var newFilename = filename[handler.Row] = filename[handler.Row].TrimToNull();
            if (newFilename == null)
                return;

            var filesToDelete = handler.StateBag[GetType().FullName + "_" + Target.Name + "_FilesToDelete"] as FilesToDelete;
            var copyResult = CopyTemporaryFile(handler, filesToDelete);
            var idField = ((IIdRow)handler.Row).IdField;

            new SqlUpdate(handler.Row.Table)
                .Set(filename, copyResult.Path)
                .Where(idField == new ValueCriteria(idField.AsObject(handler.Row)))
                .Execute(handler.UnitOfWork.Connection);

            filename[handler.Row] = copyResult.Path;
        }

        public static void CheckUploadedImageAndCreateThumbs(
            IUploadEditor attr, ITextLocalizer localizer,
            IUploadStorage storage, ref string temporaryFile, 
            IExceptionLogger logger = null)
        {
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));

            UploadPathHelper.CheckFileNameSecurity(temporaryFile);

            var fileConstraints = attr as IUploadFileConstraints;
            using var fs = storage.OpenFile(temporaryFile);

            storage.PurgeTemporaryFiles();

            var minSize = fileConstraints?.MinSize ?? 0;
            if (minSize != 0 && fs.Length < minSize)
                throw new ValidationError(string.Format(CultureInfo.CurrentCulture,
                    Texts.Controls.ImageUpload.UploadFileTooSmall.ToString(localizer),
                    UploadFormatting.FileSizeDisplay(minSize)));

            var maxSize = fileConstraints?.MaxSize ?? 0;
            if (maxSize != 0 && fs.Length > maxSize)
                throw new ValidationError(string.Format(CultureInfo.CurrentCulture,
                    Texts.Controls.ImageUpload.UploadFileTooBig.ToString(localizer),
                    UploadFormatting.FileSizeDisplay(maxSize)));

            var extension = Path.GetExtension(temporaryFile)?.ToLowerInvariant();
            var allowedExtensions = fileConstraints?.AllowedExtensions?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(allowedExtensions))
            {
                if (!allowedExtensions.Split(',', ';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Any(x => x == extension))
                {
                    throw new ValidationError(string.Format(CultureInfo.CurrentCulture,
                        Texts.Controls.ImageUpload.ExtensionNotAllowed.ToString(localizer),
                        Path.GetExtension(temporaryFile), fileConstraints.AllowedExtensions));
                }
            }

            var imageExtensions = fileConstraints?.ImageExtensions ?? ImageUploadEditorAttribute.DefaultImageExtensions;
            if (string.IsNullOrEmpty(imageExtensions) ||
                !imageExtensions.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Any(x => string.Equals(x, extension, StringComparison.OrdinalIgnoreCase)))
            {
                if (fileConstraints?.AllowNonImage == true)
                    return;

                if (string.IsNullOrEmpty(imageExtensions))
                    throw new ValidationError(
                        Texts.Controls.ImageUpload.NotAnImageFile.ToString(localizer));

                throw new ValidationError(string.Format(CultureInfo.CurrentCulture,
                    Texts.Controls.ImageUpload.NotAnImageWithExtensions.ToString(localizer),
                    Path.GetExtension(temporaryFile), fileConstraints.ImageExtensions));
            }

            var imageConstraints = attr as IUploadImageContrains;

            Image image = null;
            try
            {
                var checker = new ImageChecker
                {
                    MinWidth = imageConstraints?.MinWidth ?? 0,
                    MaxWidth = imageConstraints?.MaxWidth ?? 0,
                    MinHeight = imageConstraints?.MinHeight ?? 0,
                    MaxHeight = imageConstraints?.MaxHeight ?? 0,
                    MaxDataSize = fileConstraints?.MaxSize ?? 0
                };

                ImageCheckResult result = checker.CheckStream(fs, true, out image, 
                    out var mimeType, out var fileExtensions, logger);

                if (result != ImageCheckResult.Valid)
                {
                    if (fileConstraints?.IgnoreInvalidImage == true &&
                        result == ImageCheckResult.InvalidImage)
                        return;

                    if (fileConstraints?.IgnoreEmptyImage == true &&
                        result == ImageCheckResult.ImageIsEmpty)
                        return;

                    var error = checker.FormatErrorMessage(result, localizer);
                    throw new ValidationError(error);
                }

                if (fileConstraints?.IgnoreExtensionMismatch != true &&
                    !fileExtensions.Any(x => string.Equals(x, extension,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ValidationError(string.Format(CultureInfo.CurrentCulture,
                        Texts.Controls.ImageUpload.ImageExtensionMismatch.ToString(localizer),
                        Path.GetExtension(temporaryFile), mimeType));
                }

                var uploadImageOptions = attr as IUploadImageOptions;

                var baseFile = Path.ChangeExtension(temporaryFile, null);

                if ((uploadImageOptions.ScaleWidth > 0 || uploadImageOptions.ScaleHeight > 0) &&
                    ((uploadImageOptions.ScaleWidth > 0 && (uploadImageOptions.ScaleSmaller || checker.Width > uploadImageOptions.ScaleWidth)) ||
                        (uploadImageOptions.ScaleHeight > 0 && (uploadImageOptions.ScaleSmaller || checker.Height > uploadImageOptions.ScaleHeight))))
                {
                    var originalName = storage.GetOriginalName(temporaryFile);
                    var scaleBackColor = !string.IsNullOrEmpty(uploadImageOptions.ScaleBackColor) ?
                        Color.Parse(uploadImageOptions.ScaleBackColor) : (Color?)null;
                    
                    using var scaledImage = ThumbnailGenerator.Generate(
                        image, uploadImageOptions.ScaleWidth, uploadImageOptions.ScaleHeight, uploadImageOptions.ScaleMode, backgroundColor: scaleBackColor);
                    temporaryFile = baseFile + ".jpg";
                    fs.Close();
                    using var ms = new MemoryStream();
                    scaledImage.Save(ms, new JpegEncoder { Quality = uploadImageOptions.ScaleQuality == 0 ? null : uploadImageOptions.ScaleQuality });
                    ms.Seek(0, SeekOrigin.Begin);
                    temporaryFile = storage.WriteFile(temporaryFile, ms, autoRename: null); // overwrite
                    if (!string.IsNullOrEmpty(originalName))
                        storage.SetOriginalName(temporaryFile, Path.ChangeExtension(originalName, ".jpg"));
                }

                var thumbSizes = uploadImageOptions.ThumbSizes.TrimToNull();
                if (thumbSizes == null)
                    return;
                
                var thumbBackColor = !string.IsNullOrEmpty(uploadImageOptions.ThumbBackColor) ?
                    Color.Parse(uploadImageOptions.ThumbBackColor) : (Color?)null;

                foreach (var sizeStr in thumbSizes.Replace(";", ",", StringComparison.Ordinal).Split(new[] { ',' }))
                {
                    var dims = sizeStr.ToUpperInvariant().Split(new[] { 'X' });
                    if (dims.Length != 2 ||
                        !int.TryParse(dims[0], out int w) ||
                        !int.TryParse(dims[1], out int h) ||
                        w < 0 ||
                        h < 0 ||
                        (w == 0 && h == 0))
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                        throw new ArgumentOutOfRangeException(nameof(uploadImageOptions.ThumbSizes));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

                    using var thumbImage = ThumbnailGenerator.Generate(image, w, h, uploadImageOptions.ThumbMode, backgroundColor: thumbBackColor);
                    var thumbFile = baseFile + "_t" + w.ToInvariant() + "x" + h.ToInvariant() + ".jpg";
                    using var ms = new MemoryStream();
                    thumbImage.Save(ms, new JpegEncoder { Quality = uploadImageOptions.ThumbQuality == 0 ? null : uploadImageOptions.ThumbQuality });
                    ms.Seek(0, SeekOrigin.Begin);
                    storage.WriteFile(thumbFile, ms, autoRename: null);
                }
            }
            finally
            {
                if (image != null)
                    image.Dispose();
            }
        }
    }
}