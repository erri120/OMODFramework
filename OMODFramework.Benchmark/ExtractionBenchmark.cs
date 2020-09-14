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
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using JetBrains.Annotations;

namespace OMODFramework.Benchmark
{
    [SimpleJob(RunStrategy.Monitoring, 1, 0, 3)]
    [MinColumn, Q1Column, Q3Column, MaxColumn]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ExtractionBenchmark
    {
        private const string kOMOD = "benchmark.omod";
        private const string kExtractionOutputDir = "benchmark-output";
        private OMOD _omod;
        
        [IterationSetup]
        public void IterationSetup()
        {
            if (Directory.Exists(kExtractionOutputDir))
                Directory.Delete(kExtractionOutputDir, true);
         
            if (File.Exists(kOMOD))
                File.Delete(kOMOD);
            
            DummyOMOD.CreateDummyOMOD(kOMOD, "benchmark-data", FileCount, 1 << 23);
            
            _omod = new OMOD(kOMOD);
            Directory.CreateDirectory(kExtractionOutputDir);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _omod.Dispose();
            
            if (File.Exists(kOMOD))
                File.Delete(kOMOD);
            
            if (Directory.Exists(kExtractionOutputDir))
                Directory.Delete(kExtractionOutputDir, true);
        }

        [Params(1, 2, 3, 4)] public byte StreamCount { get; set; }
        
        [Params(100, 200, 500, 1000)] public int FileCount { get; set; }
        

        [Benchmark]
        public void Extraction()
        {
            _omod.ExtractFilesParallel(true, kExtractionOutputDir, StreamCount);
        }
    }
}
