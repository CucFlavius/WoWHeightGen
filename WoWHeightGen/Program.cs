using CASCLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SereniaBLPLib;

namespace WoWHeightGen // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        public static LocaleFlags firstInstalledLocale = LocaleFlags.enUS;
        const string OUTPUTPATH = "Output";
        static CASCConfig? cascConfig;
        static CASCHandler? cascHandler;
        static WowRootHandler? wowRootHandler;
        static List<int> wdtFileIDs;

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
                string? installPath = Console.ReadLine();
                if (installPath != null)
                {
                    if (installPath.Equals("exit")) return;
                }
                else continue;

                PrintInfo("Enter product. Eg: ", "wow, wowt, wowb");
                string? product = Console.ReadLine();
                if (product != null)
                {
                    if (product.Equals("exit")) return;
                }
                else continue;

                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Initializing CASCLib.");
                    Console.ResetColor();

                    cascConfig = CASCConfig.LoadLocalStorageConfig(installPath, product);
                    cascHandler = CASCHandler.OpenStorage(cascConfig);
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
                string? inputString = Console.ReadLine();
                if (inputString != null)
                {
                    if (inputString.ToLower().Equals("exit")) return false;
                }
                else continue;

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
                string? inputString = Console.ReadLine();
                if (inputString != null)
                {
                    if (inputString.ToLower().Equals("exit")) return false;
                }
                else continue;

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
                using (Image<Rgba32> outputImage = new Image<Rgba32>(8192, 8192))
                {
                    using (var wdtstr = cascHandler.OpenFile(wdtFileID))
                    {
                        Wdt wdt = new Wdt(wdtstr);
                        Adt[,] adts = new Adt[64, 64];
                        float minAdt = float.MaxValue;
                        float maxAdt = float.MinValue;

                        if (wdt.fileInfo != null)
                        {
                            for (var y = 0; y < 64; y++)
                            {
                                for (var x = 0; x < 64; x++)
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

                            for (int y = 0; y < 64; y++)
                            {
                                for (var x = 0; x < 64; x++)
                                {
                                    if (adts[x, y] == null)
                                        continue;

                                    byte[] castData = new byte[128 * 128 * 3];
                                    int idx = 0;
                                    for (int x1 = 0; x1 < 128; x1++)
                                    {
                                        for (int y1 = 0; y1 < 128; y1++)
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
                                    var img = Image.LoadPixelData<Rgb24>(castData, 128, 128);

                                    if (img != null)
                                    {
                                        outputImage.Mutate(o => o.DrawImage(img, new Point(128 * x, 128 * y), 1f));
                                    }
                                }
                            }
                        }
                        outputImage.SaveAsPng($"{OUTPUTPATH}/{wdtFileID}_height.png");
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
                using (Image<Rgba32> outputImage = new Image<Rgba32>(32768, 32768))
                {
                    using (var wdtstr = cascHandler.OpenFile(wdtFileID))
                    {
                        Wdt wdt = new Wdt(wdtstr);

                        if (wdt.fileInfo != null)
                        {
                            for (var y = 0; y < 64; y++)
                            {
                                for (var x = 0; x < 64; x++)
                                {
                                    var info = wdt.fileInfo[x, y];
                                    int minimapFileID = (int)info.minimapTexture;

                                    if (cascHandler.FileExists(minimapFileID))
                                    {
                                        using (var adtstr = cascHandler.OpenFile(minimapFileID))
                                        {
                                            BlpFile blp = new BlpFile(adtstr);
                                            var img = blp.GetImage(0);

                                            if (img != null)
                                            {
                                                outputImage.Mutate(o => o.DrawImage(img, new Point(512 * x, 512 * y), 1f));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        outputImage.SaveAsPng($"{OUTPUTPATH}/{wdtFileID}_minimap.png");
                    }
                }
            }
        }
    }
}