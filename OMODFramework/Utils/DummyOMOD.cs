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

using System.IO;
using System.Threading.Tasks;

namespace OMODFramework
{
    public static class DummyOMOD
    {
        private static readonly object LockObject = new object();

        public static OMODCreationOptions CreateDummyCreationOptions(string output, int fileCount, int fileSize)
        {
            var options = new OMODCreationOptions
            {
                Name = "Dummy OMOD",
                Author = "erri120",
                Description = "The best OMOD for testing!",
                Email = "gaben@valvesoftware.com",
                Website = "http://deinemutterdenktsiewaereeinhobbitundheisstfrodo.de",
                Readme = "This is the best OMOD to have ever existed. Conflicts with CTD on Death by erri120.",
                CompressionType = CompressionType.SevenZip,
                OMODCompressionLevel = CompressionLevel.High,
                DataCompressionLevel = CompressionLevel.High
            };

            if (Directory.Exists(output))
                Directory.Delete(output, true);
            
            if (!Directory.Exists(output))
                Directory.CreateDirectory(output);

            Parallel.For(0, fileCount, i =>
            {
                var path = Path.GetFullPath(Path.Combine(output, $"{i}.txt"));
                byte[] buffer = new byte[fileSize];
                File.WriteAllBytes(path, buffer);
                lock (LockObject)
                {
                    options.DataFiles.Add(new OMODCreationFile(path, $"files\\{i}.txt"));
                }
            });

            return options;
        }
        
        public static void CreateDummyOMOD(string omod, string output, int fileCount, int fileSize)
        {
            var options = CreateDummyCreationOptions(output, fileCount, fileSize);
            
            OMOD.CreateOMOD(options, omod);
        }
    }
}
