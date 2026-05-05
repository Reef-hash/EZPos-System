using System;

namespace EZPos.Models.Domain
{
    /// <summary>
    /// Represents the update manifest fetched from the hosted version endpoint.
    /// Format matches latest.json schema (GitHub Releases or custom server).
    /// </summary>
    public class UpdateManifest
    {
        public string? Version { get; set; }
        public string? Name { get; set; }
        public DateTime PublishedDate { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? DownloadUrl { get; set; }
        
        /// <summary>
        /// Checksum verification object
        /// </summary>
        public ChecksumInfo? Checksum { get; set; }
        
        /// <summary>
        /// If true, older versions MUST update and cannot skip
        /// </summary>
        public bool Mandatory { get; set; }
        
        /// <summary>
        /// Minimum version that is allowed to run
        /// If local version &lt; minimumVersion, update is mandatory
        /// </summary>
        public string? MinimumVersion { get; set; }
        
        public string? TargetFramework { get; set; }
        
        /// <summary>
        /// Indicates what components are being updated
        /// </summary>
        public UpdatedComponents? UpdatedComponents { get; set; }
    }

    public class ChecksumInfo
    {
        public string? Algorithm { get; set; }  // e.g., "sha256"
        public string? Value { get; set; }
    }

    public class UpdatedComponents
    {
        public bool Binaries { get; set; }
        public bool Schema { get; set; }
    }
}
