﻿using System.Threading;
#if !ISSOURCEGENERATOR
using Serenity.CodeGeneration;
#endif

namespace Serenity.CodeGenerator
{
    public class TSTypeLister
    {
        private readonly IGeneratorFileSystem fileSystem;
        private readonly CancellationToken cancellationToken;
        private readonly string tsConfigPath;

        public TSTypeLister(IGeneratorFileSystem fileSystem, string tsConfigPath,
            CancellationToken cancellationToken = default)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.cancellationToken = cancellationToken;
            if (tsConfigPath is null)
                throw new ArgumentNullException(nameof(tsConfigPath));

            this.tsConfigPath = fileSystem.GetFullPath(tsConfigPath);
        }

        public List<ExternalType> List()
        {
            cancellationToken.ThrowIfCancellationRequested();

            var files = TSConfigHelper.ListFiles(fileSystem, tsConfigPath, cancellationToken);

            if (files == null || !files.Any())
            {
                // legacy apps
                var projectDir = fileSystem.GetDirectoryName(tsConfigPath);
                var directories = new[]
                {
                    fileSystem.Combine(projectDir, @"Modules"),
                    fileSystem.Combine(projectDir, @"Imports"),
                    fileSystem.Combine(projectDir, @"typings", "serenity"),
                    fileSystem.Combine(projectDir, @"wwwroot", "Scripts", "serenity")
                }.Where(x => fileSystem.DirectoryExists(x));

                cancellationToken.ThrowIfCancellationRequested();

                files = directories.SelectMany(x =>
                    fileSystem.GetFiles(x, "*.ts", recursive: true))
                    .Where(x => !x.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase) ||
                        fileSystem.GetFileName(x).StartsWith("Serenity.", StringComparison.OrdinalIgnoreCase) ||
                        fileSystem.GetFileName(x).StartsWith("Serenity-", StringComparison.OrdinalIgnoreCase));

                cancellationToken.ThrowIfCancellationRequested();

                var corelib = files.Where(x => string.Equals(fileSystem.GetFileName(x),
                    "Serenity.CoreLib.d.ts", StringComparison.OrdinalIgnoreCase));

                static bool corelibUnderTypings(string x) =>
                    x.Replace('\\', '/').EndsWith("/typings/serenity/Serenity.CoreLib.d.ts",
                        StringComparison.OrdinalIgnoreCase);

                if (corelib.Count() > 1 &&
                    corelib.Any(x => !corelibUnderTypings(x)))
                {
                    files = files.Where(x => !corelibUnderTypings(x));
                }

                files = files.OrderBy(x => x);
            }

            TSTypeListerAST typeListerAST = new(fileSystem, cancellationToken);
            foreach (var file in files)
                typeListerAST.AddInputFile(file);

            var externalTypes = typeListerAST.ExtractTypes();
            return externalTypes;
        }
    }
}