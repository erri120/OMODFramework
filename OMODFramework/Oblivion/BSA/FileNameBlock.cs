using System;
using System.IO;
using JetBrains.Annotations;

namespace OMODFramework.Oblivion.BSA
{
    [PublicAPI]
    public class FileNameBlock
    {
        public readonly Lazy<ReadOnlyMemorySlice<byte>[]> Names;

        public FileNameBlock(BSAReader bsa, long position)
        {
            Names = new Lazy<ReadOnlyMemorySlice<byte>[]>(
                mode: System.Threading.LazyThreadSafetyMode.ExecutionAndPublication,
                valueFactory: () =>
                {
                    using var stream = bsa.GetStream();
                    stream.BaseStream.Position = position;
                    ReadOnlyMemorySlice<byte> data = stream.ReadBytes(checked((int)bsa.TotalFileNameLength));
                    ReadOnlyMemorySlice<byte>[] names = new ReadOnlyMemorySlice<byte>[bsa.FileCount];
                    for (var i = 0; i < bsa.FileCount; i++)
                    {
                        var index = data.Span.IndexOf(default(byte));
                        if (index == -1)
                        {
                            throw new InvalidDataException("Did not end all of its strings in null bytes");
                        }
                        names[i] = data[..(index + 1)];
                        var str = names[i].ReadStringTerm(bsa.HeaderType);
                        data = data[(index + 1)..];
                    }
                    // Data doesn't seem to need to be fully consumed.
                    // Official BSAs have overflow of zeros
                    return names;
                });
        }
    }
}
