using System;
using System.Drawing;
using System.IO;
using CommandLine;
using CommandLine.Text;
using DereTore.Exchange.UnityEngine;
using DereTore.Exchange.UnityEngine.Serialization;
using DereTore.Interop.D3DX9;
using DereTore.Interop.PVRTexLib;

namespace DereTore.Apps.JacketCreator {
    internal static class Program {

        private static int Main(string[] args) {
            const int defaultReturnCodeFail = -1;

            var parser = new Parser(settings => {
                settings.IgnoreUnknownArguments = true;
            });

            var parsedResult = parser.ParseArguments<Options>(args);
            var isOptionsValid = parsedResult.Tag == ParserResultType.Parsed;

            if (!isOptionsValid) {
                var helpText = HelpText.AutoBuild(parsedResult);
                HelpText.DefaultParsingErrorsHandler(parsedResult, helpText);
                Console.WriteLine(helpText);
                return defaultReturnCodeFail;
            }

            var options = ((Parsed<Options>)parsedResult).Value;

            options.SongID = Math.Abs(options.SongID) % 10000;
            if (string.IsNullOrEmpty(options.ImageFileName) && !File.Exists(options.ImageFileName)) {
                Console.WriteLine($"ERROR: image file '{options.ImageFileName}' is not found.");
                return defaultReturnCodeFail;
            }
            var fullDirectoryName = (new DirectoryInfo(options.OutputDirectory)).FullName;
            if (!Directory.Exists(fullDirectoryName)) {
                try {
                    Directory.CreateDirectory(fullDirectoryName);
                } catch (Exception ex) {
                    Console.WriteLine($"ERROR: Tried to create directory '{fullDirectoryName}' but failed.\n{ex.Message}");
                    return defaultReturnCodeFail;
                }
            }

            Bitmap bitmap;
            try {
                bitmap = (Bitmap)Image.FromFile(options.ImageFileName);
            } catch (Exception ex) {
                Console.WriteLine($"ERROR: Cannot read image file '{options.ImageFileName}'.\n{ex.Message}");
                return defaultReturnCodeFail;
            }

            // Magic begins!
            byte[] pvr, dds;
            using (var smallImage = new Bitmap(bitmap, BundleOptions.SmallImageSize, BundleOptions.SmallImageSize)) {
                pvr = PvrUtilities.GetPvrTextureFromImage(smallImage);
            }
            using (var mediumImage = new Bitmap(bitmap, BundleOptions.MediumImageSize, BundleOptions.MediumImageSize)) {
                dds = DdsUtilities.GetDdsTextureFromImage(mediumImage);
            }
            bitmap.Dispose();

            var bundleOptions = new BundleOptions();
            bundleOptions.PvrImage = pvr;
            bundleOptions.DdsImage = dds;
            bundleOptions.PvrPathID = options.PvrPathID;
            bundleOptions.DdsPathID = options.DdsPathID;
            bundleOptions.SongID = options.SongID;

            var fileName = Path.Combine(fullDirectoryName, $"jacket_{options.SongID}_android.unity3d");
            using (var fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write)) {
                bundleOptions.Platform = UnityPlatformID.Android;
                JacketBundle.Serialize(bundleOptions, fileStream);
            }
            fileName = Path.Combine(fullDirectoryName, $"jacket_{options.SongID}_ios.unity3d");
            using (var fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write)) {
                bundleOptions.Platform = UnityPlatformID.iOS;
                JacketBundle.Serialize(bundleOptions, fileStream);
            }
            Console.WriteLine($"Building complete. Files are written to '{fullDirectoryName}', song ID = {options.SongID}.");

            return 0;
        }

    }
}
