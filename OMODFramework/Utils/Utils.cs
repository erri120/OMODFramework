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
        private static readonly object NextFileLockObject = new object();

        internal static TempFile GetTempFile(FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.Read, string? copyFile = null)
        {
            //using lock so we don't have funky multi-thread problems when incrementing
            lock (NextFileLockObject)
            {
                var path = "";
                if (!File.Exists(Path.Combine("tmp", $"{_nextFile}.omodFramework.tmp")))
                {
                    path = Path.Combine("tmp", $"{_nextFile++}.omodFramework.tmp");
                }
                else
                {
                    for (var i = _nextFile; i < 696969; i++)
                    {
                        if (File.Exists(Path.Combine("tmp", $"{i}.omodFramework.tmp"))) continue;
                        path = Path.Combine("tmp", $"{i}.omodFramework.tmp");
                        _nextFile = i + 1;
                    }
                }
            
                return new TempFile(path, mode, access, share, copyFile);
            }
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
