using System;
using System.Drawing.Imaging;
using System.IO;
using CommandLine;
using JetBrains.Annotations;

namespace OMODFramework.Example.CSharp
{
    [Verb("extract", HelpText = "Extract an OMOD")]
    [PublicAPI]
    public class ExtractOptions
    {
        [Option('i', "input", HelpText = "Input file", Required = true)]
        public string Input { get; set; } = string.Empty;

        [Option('o', "output", HelpText = "Output directory", Required = true)]
        public string Output { get; set; } = string.Empty;

        public static int Extract(ExtractOptions options)
        {
            var file = new FileInfo(options.Input);
            if (!file.Exists)
                throw new ArgumentException($"File {file} does not exist!");
            if (file.Extension != ".omod")
                throw new ArgumentException($"File {file} is not an OMOD!");

            var output = new DirectoryInfo(options.Output);
            if (!output.Exists)
                output.Create();

            //make sure you are using the "using" statement so that the variable is disposed correctly
            //more info: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-statement
            using var omod = new OMOD(file);

            Utils.Log($"Starting extraction of OMOD {omod.Config.Name} by {omod.Config.Author}");

            if (omod.HasFile(OMODEntryFileType.Readme))
            {
                //you can either use this function to get a Stream or the 
                //GetScript function to get the readme as a string.
                using var stream = omod.ExtractFile(OMODEntryFileType.Script);
                using var fileStream = File.Create(Path.Combine(output.FullName, "readme.txt"));
                stream.CopyTo(fileStream);

                //you can do the same thing as above for the script as well if you want
            }

            if (omod.HasFile(OMODEntryFileType.Image))
            {
                //the image is returned as a Bitmap, make sure you are also using the
                //"using" statement here as well so that the image is disposed correctly
                using var bitmap = omod.GetImage();

                var extension = "";

                if (bitmap.RawFormat.Equals(ImageFormat.Png))
                    extension = ".png";
                else if (bitmap.RawFormat.Equals(ImageFormat.Jpeg))
                    extension = ".jpeg";

                using var fileStream = File.Create(Path.Combine(output.FullName, $"image{extension}"));
                bitmap.Save(fileStream, bitmap.RawFormat);
            }

            //this function already checks if the file to be extracted already exists
            //it compare the length of the existing file with the expected length and
            //either continues if they are the same or deletes the file if they don't match
            omod.ExtractDataFiles(output);

            //every omod has Data files but not every omod has plugins
            if (omod.HasFile(OMODEntryFileType.PluginsCRC))
                omod.ExtractPluginFiles(output);

            Utils.Log($"Extracted to {output.FullName}");

            return 0;
        }
    }
}
