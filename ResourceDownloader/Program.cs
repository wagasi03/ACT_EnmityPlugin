using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace ResourceDownloader
{
    class Program
    {
        const float WEB_ACCESS_RATE_LIMIT_BASIC = 0.5F;
        const float WEB_ACCESS_RATE_LIMIT_ICONS = 20.0F;

        static void Main(string[] args)
        {
            string opt = String.Empty;
            if (args.Length > 0)
            {
                opt = args[0];
            }

            Console.WriteLine("===========================================");
            Console.WriteLine("==           ResouceDownloader           ==");
            Console.WriteLine("===========================================");
            Console.WriteLine("");


            DownloadStatusData();

            if (opt == "/y" || opt == "/Y")
            {
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("To exit application, press \"Enter\".");
                Console.ReadLine();
            }
        }

        static void DownloadStatusData()
        {
            string jsonPath = @"resources\EnmityPlugin\json\status";
            if (!Directory.Exists(jsonPath))
            {
                try
                {
                    Directory.CreateDirectory(jsonPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
            }


            string imgPath = @"resources\EnmityPlugin\images\status";
            if (!Directory.Exists(imgPath))
            {
                try
                {
                    Directory.CreateDirectory(imgPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
            }

            string escapedJson = String.Empty;
            List<Model.Status> statusList = new List<Model.Status>();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            float downloadRate = 0.0F;
            int count = 0;

            int pageTarget = 1;
            int pageTotal = 1;

            Regex iconIdRegex = new Regex(@"(?<id>[0-9]*)\.png$");

            Console.Write("Enter Your API Key for xivapi: ");
            var apikey = Console.ReadLine();
            Console.WriteLine("");
            Console.WriteLine(" Donloading Status Metadata...");
            Console.WriteLine("-------------------------------------");

            while (pageTarget <= pageTotal)
            {
                // レート制限
                downloadRate = (count == 0) ? 0.0F : (float)(count * 1000) / (stopwatch.ElapsedMilliseconds);
                Console.WriteLine("Downloading: Page {0}/{1} (Rate= {2:F2} /sec)", pageTarget, pageTotal, downloadRate);
                Thread.Sleep(1000);
                while (downloadRate > WEB_ACCESS_RATE_LIMIT_BASIC)
                {
                    Thread.Sleep(500);
                    downloadRate = (float)(count * 1000) / (stopwatch.ElapsedMilliseconds);
                }

                using (WebClient webClient = new WebClient())
                {
                    try
                    {
                        count++;
                        escapedJson = webClient.DownloadString("https://xivapi.com/Status?columns=ID,Icon,Name_de,Name_en,Name_fr,Name_ja" + @"&page=" + pageTarget + @"&key=" + apikey);
                    }
                    catch (WebException wex)
                    {
                        Console.WriteLine($" Error. {wex.Message} ({wex.Status})");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" Error. {ex.Message})");
                        return;
                    }

                }

                Model.XIVAPI.StatusResultSet statusResultSet = JsonConvert.DeserializeObject<Model.XIVAPI.StatusResultSet>(escapedJson);

                foreach (var data in statusResultSet.Results)
                {
                    Match iconIdMatch = iconIdRegex.Match(data.Icon);

                    statusList.Add(new Model.Status()
                    {
                        Id = data.ID,
                        IconURI = data.Icon,
                        Icon = iconIdMatch.Success ? int.Parse(iconIdMatch.Groups["id"].Value) : 0,
                        Name = data.Name_en,
                        Name_de = data.Name_de,
                        Name_en = data.Name_en,
                        Name_fr = data.Name_fr,
                        Name_ja = data.Name_ja,
                    });
                }


                // 次ステップ
                pageTarget++;
                pageTotal = statusResultSet.Pagination.PageTotal;

            }

            try
            {
                Dictionary<int, Model.Status> status = new Dictionary<int, Model.Status>();
                Dictionary<int, Model.StatusSummary> status_en = new Dictionary<int, Model.StatusSummary>();
                Dictionary<int, Model.StatusSummary> status_fr = new Dictionary<int, Model.StatusSummary>();
                Dictionary<int, Model.StatusSummary> status_de = new Dictionary<int, Model.StatusSummary>();
                Dictionary<int, Model.StatusSummary> status_ja = new Dictionary<int, Model.StatusSummary>();

                using (WebClient webClient = new WebClient())
                {
                    Console.WriteLine("");
                    Console.WriteLine("Downloading Icons ...");
                    Console.WriteLine("-------------------------------------");

                    // Donwload 0.png
                    Uri zeroImgUri = new Uri("http://xivapi.com/i/000000/000000.png");
                    string zeroImgFileName = "0.png";
                    string zeroImgFilePath = imgPath + @"\" + zeroImgFileName;

                    try
                    {
                        Console.WriteLine("Downloading: {0} (CurrentAccessRate= {1:F2} /sec)", zeroImgFileName, downloadRate);
                        count++;
                        webClient.DownloadFile(zeroImgUri, zeroImgFilePath);
                    }
                    catch (WebException wex)
                    {
                        Console.WriteLine($" => {wex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" => {ex.Message}");
                    }


                    // Download Status Icons
                    foreach (var s in statusList)
                    {
                        downloadRate = (float)(count * 1000) / (stopwatch.ElapsedMilliseconds);
                        while (downloadRate > WEB_ACCESS_RATE_LIMIT_ICONS)
                        {
                            Thread.Sleep(500);
                            downloadRate = (float)(count * 1000) / (stopwatch.ElapsedMilliseconds);
                        }

                        string iconFileName = String.Empty;
                        if (s.Icon > 0 && !string.IsNullOrEmpty(s.IconURI))
                        {
                            string fileName = s.Icon + @".png";
                            string filePath = imgPath + @"\" + fileName;
                            Uri imgUri = new Uri("https://xivapi.com" + s.IconURI);

                            try
                            {
                                Console.WriteLine("Downloading: {0} (Rate= {1:F2} /sec)", fileName, downloadRate);
                                count++;
                                webClient.DownloadFile(imgUri, filePath);
                                iconFileName = fileName;
                            }
                            catch (WebException wex)
                            {
                                iconFileName = null;
                                Console.WriteLine($" => {wex.Message}");
                            }
                            catch (Exception ex)
                            {
                                iconFileName = null;
                                Console.WriteLine($" => {ex.Message}");
                            }
                        }
                        else
                        {
                            iconFileName = "0.png";
                        }

                        status.Add(s.Id, new Model.Status
                        {
                            Id = s.Id,
                            Icon = s.Icon,
                            IconFileName = iconFileName,
                            Name = s.Name,
                            Name_en = s.Name_en,
                            Name_fr = s.Name_fr,
                            Name_de = s.Name_de,
                            Name_ja = s.Name_ja
                        });
                        status_en.Add(s.Id, new Model.StatusSummary { IconFileName = iconFileName, Name = s.Name_en });
                        status_fr.Add(s.Id, new Model.StatusSummary { IconFileName = iconFileName, Name = s.Name_fr });
                        status_de.Add(s.Id, new Model.StatusSummary { IconFileName = iconFileName, Name = s.Name_de });
                        status_ja.Add(s.Id, new Model.StatusSummary { IconFileName = iconFileName, Name = s.Name_ja });
                    }
                }

                Console.WriteLine("");
                Console.WriteLine("Writing JSON Files...");
                Console.WriteLine("-------------------------------------");

                Console.WriteLine("Writing: " + @"status.json");
                File.WriteAllText(jsonPath + @"\status.json", Newtonsoft.Json.JsonConvert.SerializeObject(status));

                Console.WriteLine("Writing: " + @"status_en.json");
                File.WriteAllText(jsonPath + @"\status_en.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_en));

                Console.WriteLine("Writing: " + @"status_fr.json");
                File.WriteAllText(jsonPath + @"\status_fr.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_fr));

                Console.WriteLine("Writing: " + @"status_de.json");
                File.WriteAllText(jsonPath + @"\status_de.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_de));

                Console.WriteLine("Writing: " + @"status_ja.json");
                File.WriteAllText(jsonPath + @"\status_ja.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_ja));


                Console.WriteLine("");
                Console.WriteLine("Writing JS Files...");
                Console.WriteLine("-------------------------------------");


                Console.WriteLine("Writing: " + @"status.js");
                File.WriteAllText(jsonPath + @"\status.js", "var statusArray = ");
                File.AppendAllText(jsonPath + @"\status.js", Newtonsoft.Json.JsonConvert.SerializeObject(status));
                File.AppendAllText(jsonPath + @"\status.js", ";");

                Console.WriteLine("Writing: " + @"status_en.js");
                File.WriteAllText(jsonPath + @"\status_en.js", "var statusArray = ");
                File.AppendAllText(jsonPath + @"\status_en.js", Newtonsoft.Json.JsonConvert.SerializeObject(status_en));
                File.AppendAllText(jsonPath + @"\status_en.js", ";");

                Console.WriteLine("Writing: " + @"status_fr.js");
                File.WriteAllText(jsonPath + @"\status_fr.js", "var statusArray = ");
                File.AppendAllText(jsonPath + @"\status_fr.js", Newtonsoft.Json.JsonConvert.SerializeObject(status_fr));
                File.AppendAllText(jsonPath + @"\status_fr.js", ";");

                Console.WriteLine("Writing: " + @"status_de.js");
                File.WriteAllText(jsonPath + @"\status_de.js", "var statusArray = ");
                File.AppendAllText(jsonPath + @"\status_de.js", Newtonsoft.Json.JsonConvert.SerializeObject(status_de));
                File.AppendAllText(jsonPath + @"\status_de.js", ";");

                Console.WriteLine("Writing: " + @"status_ja.js");
                File.WriteAllText(jsonPath + @"\status_ja.js", "var statusArray = ");
                File.AppendAllText(jsonPath + @"\status_ja.js", Newtonsoft.Json.JsonConvert.SerializeObject(status_ja));
                File.AppendAllText(jsonPath + @"\status_ja.js", ";");


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error. {ex.Message})");
                return;
            }

            stopwatch.Stop();
            Console.WriteLine("");
            Console.WriteLine("-------------------------------------");
            Console.WriteLine("Complete. Time= {0} seconds.", (float)(stopwatch.ElapsedMilliseconds / 1000F));
            Console.WriteLine("-------------------------------------");
            Console.WriteLine("");

        }
    }
}
