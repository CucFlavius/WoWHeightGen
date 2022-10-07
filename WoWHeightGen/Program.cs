using CASCLib;
using SereniaBLPLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace WoWHeightGen // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        const LocaleFlags firstInstalledLocale = LocaleFlags.enUS;
        const string OUTPUTPATH = "Output";
        const int MAP_SIZE = 64;
        const int HEIGHT_CHUNK_RES = 128;
        const int HEIGHT_MAP_RES = 8192;

        static string? installPath;
        static string? product;
        static string? versionName;
        static CASCConfig? cascConfig;
        static CASCHandler? cascHandler;
        static WowRootHandler? wowRootHandler;
        static List<int>? wdtFileIDs;

        static void Main(string[] args)
        {
            GetWowInstallInfo();
            while (true)
            {
                if (!GetWDTInfo()) return;
                if (!GetTaskInfo()) return;
            }
        }

        static void GetWowInstallInfo()
        {
            while (true)
            {
                Console.WriteLine("Type \"exit\" to quit.");
                PrintInfo("Enter WoW install path. Eg: ", "D:/Games/World of Warcraft");
                Console.WriteLine("");
                if (GetConsoleString(out installPath)) continue;

                PrintInfo("Enter product. Eg: ", "wow, wowt, wow_beta");
                if (GetConsoleString(out product)) continue;

                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Initializing CASCLib.");
                    Console.ResetColor();

                    cascConfig = CASCConfig.LoadLocalStorageConfig(installPath, product);
                    cascHandler = CASCHandler.OpenStorage(cascConfig);
                    versionName = cascConfig.VersionName;

                    wowRootHandler = cascHandler.Root as WowRootHandler;
                    if (wowRootHandler != null)
                    {
                        wowRootHandler.SetFlags(firstInstalledLocale, false);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message.ToString());
                    Console.ResetColor();
                    continue;
                }
            }
        }

        static bool GetWDTInfo()
        {
            while (true)
            {
                PrintInfo("Enter WDT fileID. Eg: ", "782779");
                PrintInfo("Or enter WDT fileIDs separated by comma. Eg: ", "782779,790112,790796");
                Console.WriteLine("");
                if (GetConsoleString(out string? inputString)) continue;
                if (inputString == null) continue;

                try
                {
                    inputString = inputString.Replace(" ", "");
                    string[] split = inputString.Split(',');

                    wdtFileIDs = new List<int>();

                    for (int i = 0; i < split.Length; i++)
                    {
                        if (int.TryParse(split[i], out int fileID))
                        {
                            wdtFileIDs.Add(fileID);
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message.ToString());
                    Console.ResetColor();
                    continue;
                }
            }
        }

        static bool GetTaskInfo()
        {
            if (wdtFileIDs == null) return true;

            while (true)
            {
                PrintInfo("Pick a task: 1 - Export Height Map, 2 - Export Minimaps. Eg: ", "1");
                Console.WriteLine("");
                if (GetConsoleString(out string? inputString)) continue;

                try
                {
                    if (int.TryParse(inputString, out int taskType))
                    {
                        if (taskType == 1)
                        {
                            foreach (var fileID in wdtFileIDs)
                            {
                                Console.WriteLine("Processing : " + fileID);
                                BuildHeight(fileID, false, false);
                                return true;
                            }
                        }
                        else if (taskType == 2)
                        {
                            foreach (var fileID in wdtFileIDs)
                            {
                                Console.WriteLine("Processing : " + fileID);
                                BuildMinimap(fileID);
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message.ToString());
                    Console.ResetColor();
                    continue;
                }
            }
        }

        static bool GetConsoleString(out string? value)
        {
            value = Console.ReadLine();

            if (value != null)
                if (value.Equals("exit")) return true;

            return false;
        }

        static void PrintInfo(string a, string b)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(a);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(b);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }

        static void BuildHeight(int wdtFileID, bool clampToAboveSea, bool clampToBelowSea)
        {
            if (!Directory.Exists(OUTPUTPATH))
                Directory.CreateDirectory(OUTPUTPATH);

            if (cascHandler == null) return;
            if (cascHandler.FileExists(wdtFileID))
            {
                using (Image<Rgba32> outputImage = new Image<Rgba32>(HEIGHT_MAP_RES, HEIGHT_MAP_RES))
                {
                    using (var wdtstr = cascHandler.OpenFile(wdtFileID))
                    {
                        Wdt wdt = new Wdt(wdtstr);
                        Adt[,] adts = new Adt[MAP_SIZE, MAP_SIZE];
                        float minAdt = float.MaxValue;
                        float maxAdt = float.MinValue;

                        if (wdt.fileInfo != null)
                        {
                            for (var y = 0; y < MAP_SIZE; y++)
                            {
                                for (var x = 0; x < MAP_SIZE; x++)
                                {
                                    var info = wdt.fileInfo[x, y];
                                    int adtFileID = (int)info.rootADT;

                                    if (cascHandler.FileExists(adtFileID))
                                    {
                                        using (var adtstr = cascHandler.OpenFile(adtFileID))
                                        {
                                            adts[x, y] = new Adt(adtstr);

                                            if (minAdt > adts[x, y].minHeight)
                                                minAdt = adts[x, y].minHeight;

                                            if (maxAdt < adts[x, y].maxHeight)
                                                maxAdt = adts[x, y].maxHeight;
                                        }
                                    }
                                }
                            }

                            if (clampToAboveSea)
                                minAdt = 0;

                            if (clampToBelowSea)
                                maxAdt = 0;

                            Console.WriteLine($"{wdtFileID} : Min Height {minAdt} Max Height {maxAdt}");

                            for (int y = 0; y < MAP_SIZE; y++)
                            {
                                for (var x = 0; x < MAP_SIZE; x++)
                                {
                                    if (adts[x, y] == null)
                                        continue;

                                    byte[] castData = new byte[HEIGHT_CHUNK_RES * HEIGHT_CHUNK_RES * 3];
                                    int idx = 0;
                                    for (int x1 = 0; x1 < HEIGHT_CHUNK_RES; x1++)
                                    {
                                        for (int y1 = 0; y1 < HEIGHT_CHUNK_RES; y1++)
                                        {
                                            float value = adts[x, y].heightmap[x1, y1];
                                            if (clampToAboveSea)
                                                if (value < 0) value = 0;
                                            if (clampToBelowSea)
                                                if (value > 0) value = 0;

                                            float normalized = (value - minAdt) / (maxAdt - minAdt);
                                            byte bValue = (byte)(normalized * 255f);
                                            castData[idx] = bValue;
                                            castData[idx + 1] = bValue;
                                            castData[idx + 2] = bValue;
                                            idx += 3;
                                        }
                                    }
                                    var img = Image.LoadPixelData<Rgb24>(castData, HEIGHT_CHUNK_RES, HEIGHT_CHUNK_RES);

                                    if (img != null)
                                    {
                                        outputImage.Mutate(o => o.DrawImage(img, new Point(HEIGHT_CHUNK_RES * x, HEIGHT_CHUNK_RES * y), 1f));
                                    }
                                }
                            }
                        }
                        outputImage.SaveAsPng($"{OUTPUTPATH}/{wdtFileID}_height_{product}_{versionName}.png");
                    }
                }
            }
        }

        static void BuildMinimap(int wdtFileID)
        {
            if (!Directory.Exists(OUTPUTPATH))
                Directory.CreateDirectory(OUTPUTPATH);

            if (cascHandler == null) return;
            if (cascHandler.FileExists(wdtFileID))
            {
                using (var wdtstr = cascHandler.OpenFile(wdtFileID))
                {
                    Wdt wdt = new Wdt(wdtstr);

                    if (wdt.fileInfo != null)
                    {
                        var resolution = GetMinimapResolution(wdt);

                        using (Image<Rgba32> outputImage = new Image<Rgba32>(resolution * MAP_SIZE, resolution * MAP_SIZE))
                        {
                            for (var y = 0; y < MAP_SIZE; y++)
                            {
                                for (var x = 0; x < MAP_SIZE; x++)
                                {
                                    var info = wdt.fileInfo[x, y];
                                    int minimapFileID = (int)info.minimapTexture;

                                    if (cascHandler.FileExists(minimapFileID))
                                    {
                                        using (var blpStr = cascHandler.OpenFile(minimapFileID))
                                        {
                                            BlpFile blp = new BlpFile(blpStr);
                                            var img = blp.GetImage(0);

                                            if (img != null)
                                            {
                                                outputImage.Mutate(o => o.DrawImage(img, new Point(resolution * x, resolution * y), 1f));
                                            }
                                        }
                                    }
                                }
                            }

                            outputImage.SaveAsPng($"{OUTPUTPATH}/{wdtFileID}_minimap_{product}_{versionName}.png");
                        }
                    }
                }
            }
        }

        static int GetMinimapResolution(Wdt wdt)
        {
            if (cascHandler == null) return 0;
            if (wdt == null) return 0;
            if (wdt.fileInfo == null) return 0;

            for (var y = 0; y < MAP_SIZE; y++)
            {
                for (var x = 0; x < MAP_SIZE; x++)
                {
                    var info = wdt.fileInfo[x, y];
                    int minimapFileID = (int)info.minimapTexture;

                    if (cascHandler.FileExists(minimapFileID))
                    {
                        using (var blpstr = cascHandler.OpenFile(minimapFileID))
                        {
                            using (BinaryReader br = new BinaryReader(blpstr, Encoding.ASCII, true))
                            {
                                br.BaseStream.Position += 12;
                                var width = br.ReadInt32();
                                var height = br.ReadInt32();

                                return width;
                            }
                        }
                    }
                }
            }

            return 0;
        }
    }
}