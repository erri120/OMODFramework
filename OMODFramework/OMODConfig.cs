using System;
using System.Globalization;
using System.IO;
using JetBrains.Annotations;
using OMODFramework.Compression;

namespace OMODFramework
{
    [PublicAPI]
    public class OMODConfig
    {
        public string Name { get; set; }

        private int _majorVersion, _minorVersion, _buildVersion;
        public Version Version => new Version(_majorVersion, _minorVersion, _buildVersion);

        public string Description { get; set; }
        
        public string Email { get; set; }
        
        public string Website { get; set; }
        
        public string Author { get; set; }
        
        public DateTime CreationTime { get; set; }
        
        public byte FileVersion { get; set; }
        
        public CompressionType CompressionType { get; set; }

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
