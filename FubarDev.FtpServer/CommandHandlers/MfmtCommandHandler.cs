//-----------------------------------------------------------------------
// <copyright file="MfmtCommandHandler.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.FileSystem;
using FubarDev.FtpServer.ListFormatters.Facts;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// Implements the <code>MFMT</code> command.
    /// </summary>
    public class MfmtCommandHandler : FtpCommandHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MfmtCommandHandler"/> class.
        /// </summary>
        /// <param name="connection">The connection to create this command handler for.</param>
        public MfmtCommandHandler(IFtpConnection connection)
            : base(connection, "MFMT")
        {
        }

        /// <inheritdoc/>
        public override IEnumerable<IFeatureInfo> GetSupportedFeatures()
        {
            yield return new GenericFeatureInfo("MFMT");
        }

        /// <inheritdoc/>
        public override async Task<FtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            var parts = command.Argument.Split(new[] { ' ' }, 2);
            if (parts.Length != 2)
            {
                return new FtpResponse(551, "Timestamp or file name missing.");
            }

            if (!parts[0].TryParseTimestamp("UTC", out var modificationTime))
            {
                return new FtpResponse(551, "Invalid timestamp.");
            }

            var path = parts[1];
            var currentPath = Data.Path.Clone();
            var fileInfo = await Data.FileSystem.SearchFileAsync(currentPath, path, cancellationToken).ConfigureAwait(false);
            if (fileInfo?.Entry == null)
            {
                return new FtpResponse(550, "File not found.");
            }

            await Data.FileSystem.SetMacTimeAsync(fileInfo.Entry, modificationTime, null, null, cancellationToken).ConfigureAwait(false);

            var fact = new ModifyFact(modificationTime);
            var fullName = currentPath.GetFullPath() + fileInfo.FileName;

            return new FtpResponse(213, $"{fact.Name}={fact.Value}; {fullName}");
        }
    }
}
