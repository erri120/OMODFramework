﻿using System;
using System.Collections.Generic;
using System.IO;
using Force.Crc32;

namespace OMODFramework
{
    internal static partial class Utils
    {
        internal static void Do<T>(this IEnumerable<T> col, Action<T> a)
        {
            foreach(var item in col) a(item);
        }

        internal static string ToFileString(this OMODEntryFileType entryFileType)
        {
            return entryFileType switch
            {
                OMODEntryFileType.DataCRC => "data.crc",
                OMODEntryFileType.PluginsCRC => "plugins.crc",
                OMODEntryFileType.Config => "config",
                OMODEntryFileType.Readme => "readme",
                OMODEntryFileType.Script => "script",
                OMODEntryFileType.Image => "image",
                OMODEntryFileType.Data => "data",
                OMODEntryFileType.Plugins => "plugins",
                _ => throw new ArgumentOutOfRangeException(nameof(entryFileType), entryFileType, "Should not be possible!")
            };
        }

        private static int _nextFile;

        internal static string CreateTempFile()
        {
            if (!File.Exists(Path.Combine("tmp", $"tmp_{_nextFile}.omodframework.tmp.file")))
                return Path.Combine("tmp", $"tmp_{_nextFile++}.omodframework.tmp.file");

            for (var i = 0; i < 32000; i++)
            {
                var path = Path.Combine("tmp", $"tmp_{i}.omodframework.tmp.file");
                if (!Directory.Exists("tmp"))
                    Directory.CreateDirectory("tmp");

                if (File.Exists(path))
                    continue;
                _nextFile = i+1;
                return path;
            }
            throw new Exception("Reached max amount of temp files!");
        }

        internal static uint CRC32(FileInfo file)
        {
            using var fs = file.OpenRead();
            using var crc = new Crc32CAlgorithm();

            byte[] bytes = crc.ComputeHash(fs);
            //TODO: change the CRC lib
            var crc32C = BitConverter.ToUInt32(bytes, 0);

            return crc32C;
        }
    }
}
