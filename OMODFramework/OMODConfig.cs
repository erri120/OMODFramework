// /*
//     Copyright (C) 2020  erri120
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <https://www.gnu.org/licenses/>.
// */

using System;
using System.Globalization;
using System.IO;
using JetBrains.Annotations;

namespace OMODFramework
{
    [PublicAPI]
    public class Config
    {
        /// <summary>
        /// Name of the OMOD
        /// </summary>
        public string Name { get; set; } = string.Empty;
        private int _majorVersion, _minorVersion, _buildVersion;
        /// <summary>
        /// Version of the OMOD
        /// </summary>
        public Version Version => new Version(_majorVersion, _minorVersion, _buildVersion);
        /// <summary>
        /// Description of the OMOD, can be empty
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Email of the author, can be empty
        /// </summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>
        /// Website of the OMOD, can be empty
        /// </summary>
        public string Website { get; set; } = string.Empty;
        /// <summary>
        /// Author of the OMOD, can be empty
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// <see cref="DateTime"/> of the creation
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// File Version which is checked against <see cref="FrameworkSettings.CurrentOMODVersion"/>
        /// </summary>
        public byte FileVersion { get; set; }

        /// <summary>
        /// <see cref="CompressionType"/> of the OMOD
        /// </summary>
        public CompressionType CompressionType { get; set; }

        internal static Config ParseConfig(Stream stream)
        {
            var config = new Config();
            using var br = new BinaryReader(stream);

            config.FileVersion = br.ReadByte();
            config.Name = br.ReadString();
            config._majorVersion = br.ReadInt32();
            config._minorVersion = br.ReadInt32();
            config.Author = br.ReadString();
            config.Email = br.ReadString();
            config.Website = br.ReadString();
            config.Description = br.ReadString();

            if (config.FileVersion >= 2)
                config.CreationTime = DateTime.FromBinary(br.ReadInt64());
            else
            {
                var sCreationTime = br.ReadString();
                config.CreationTime = DateTime.TryParseExact(sCreationTime, "dd/MM/yyyy HH:mm", null,
                    DateTimeStyles.None, out var creationTime)
                    ? creationTime
                    : new DateTime(2006, 1, 1);
            }

            config.CompressionType = (CompressionType) br.ReadByte();

            if (config.FileVersion >= 1)
                config._buildVersion = br.ReadInt32();
            
            if (config._majorVersion < 0)
                config._majorVersion = 0;
            if (config._minorVersion < 0)
                config._minorVersion = 0;
            if (config._buildVersion < 0)
                config._buildVersion = 0;

            return config;
        }
    }
}
