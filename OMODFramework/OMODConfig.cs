using System;
using System.Globalization;
using System.IO;
using JetBrains.Annotations;
using OMODFramework.Compression;

namespace OMODFramework
{
    /// <summary>
    /// Represents the configuration of an OMOD.
    /// </summary>
    [PublicAPI]
    public class OMODConfig
    {
        /// <summary>
        /// Name of the OMOD.
        /// </summary>
        public string Name { get; set; }

        private int _majorVersion, _minorVersion, _buildVersion;
        
        /// <summary>
        /// Version of the OMOD.
        /// </summary>
        public Version Version => new Version(_majorVersion, _minorVersion, _buildVersion);

        /// <summary>
        /// Description of the OMOD.
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Email of the Author.
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// Website of the Author.
        /// </summary>
        public string Website { get; set; }
        
        /// <summary>
        /// Name of the Author.
        /// </summary>
        public string Author { get; set; }
        
        /// <summary>
        /// Creation-time of the OMOD.
        /// </summary>
        public DateTime CreationTime { get; set; }
        
        /// <summary>
        /// File Version of the OMOD.
        /// </summary>
        public byte FileVersion { get; set; }
        
        /// <summary>
        /// Compression Type of the OMOD.
        /// </summary>
        public CompressionType CompressionType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OMODConfig"/> class.
        /// </summary>
        /// <param name="name"><see cref="Name"/></param>
        /// <param name="version"><see cref="Version"/></param>
        /// <param name="description"><see cref="Description"/></param>
        /// <param name="email"><see cref="Email"/></param>
        /// <param name="website"><see cref="Website"/></param>
        /// <param name="author"><see cref="Author"/></param>
        /// <param name="creationTime"><see cref="CreationTime"/></param>
        /// <param name="fileVersion"><see cref="FileVersion"/></param>
        /// <param name="compressionType"><see cref="CompressionType"/></param>
        public OMODConfig(string name, Version version, string description, string email, string website, string author,
            DateTime creationTime, byte fileVersion, CompressionType compressionType)
        {
            Name = name;
            _majorVersion = version.Major;
            _minorVersion = version.Minor;
            _buildVersion = version.Build;
            Description = description;
            Email = email;
            Website = website;
            Author = author;
            CreationTime = creationTime;
            FileVersion = fileVersion;
            CompressionType = compressionType;
        }
        
        internal static OMODConfig ParseConfig(Stream configStream)
        {
            using var br = new BinaryReader(configStream);

            var fileVersion = br.ReadByte();
            var name = br.ReadString();
            var majorVersion = br.ReadInt32();
            var minorVersion = br.ReadInt32();
            var buildVersion = 0;
            var author = br.ReadString();
            var email = br.ReadString();
            var website = br.ReadString();
            var description = br.ReadString();

            DateTime creationTime;
            if (fileVersion >= 2)
            {
                creationTime = DateTime.FromBinary(br.ReadInt64());
            }
            else
            {
                var sCreationTime = br.ReadString();
                creationTime = DateTime.TryParseExact(sCreationTime, "dd/MM/yyyy HH:mm", null, DateTimeStyles.None, out var time)
                    ? time
                    : new DateTime(2006, 1, 1);
            }

            var compressionType = (CompressionType) br.ReadByte();

            if (fileVersion >= 1)
                buildVersion = br.ReadInt32();

            majorVersion = majorVersion < 0 ? 0 : majorVersion;
            minorVersion = minorVersion < 0 ? 0 : minorVersion;
            buildVersion = buildVersion < 0 ? 0 : buildVersion;
            
            return new OMODConfig(name, new Version(majorVersion, minorVersion, buildVersion), description, email,
                website, author, creationTime, fileVersion, compressionType);
        }
    }
}
