using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;

namespace ResourceDownloader
{
    class Program
    {
        const float WEB_ACCESS_RATE_LIMIT = 5.0F;

        static void Main(string[] args)
        {
            string opt = String.Empty;
            if(args.Length > 0)
            {
                opt = args[0];
            }

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

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            float downloadRate = 0.0F;
            int count = 0;

            using (WebClient webClient = new WebClient())
            {
                try
                {
                    Console.Write("Donloading Status Metadata...");
                    count++;
                    escapedJson = webClient.DownloadString("https://api.xivdb.com/status?columns=id,icon,name,name_en,name_fr,name_de,name_ja");
                    Console.WriteLine($" Success.");
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

            downloadRate = (float)(count * 1000) / (stopwatch.ElapsedMilliseconds);

            List<Model.Status> statusList = JsonConvert.DeserializeObject<List<Model.Status>>(escapedJson);
            Console.WriteLine("Generating JSON files and downloading icons...");
            try
            {
                Dictionary<int, Model.Status> status = new Dictionary<int, Model.Status>();
                Dictionary<int, Model.StatusSummary> status_en = new Dictionary<int, Model.StatusSummary>();
                Dictionary<int, Model.StatusSummary> status_fr = new Dictionary<int, Model.StatusSummary>();
                Dictionary<int, Model.StatusSummary> status_de = new Dictionary<int, Model.StatusSummary>();
                Dictionary<int, Model.StatusSummary> status_ja = new Dictionary<int, Model.StatusSummary>();

                using (WebClient webClient = new WebClient())
                {


                    foreach (var s in statusList)
                    {
                        downloadRate = (float)(count * 1000) / (stopwatch.ElapsedMilliseconds);
                        while (downloadRate > WEB_ACCESS_RATE_LIMIT)
                        {
                            Thread.Sleep(500);
                            downloadRate = (float)(count * 1000) / (stopwatch.ElapsedMilliseconds);
                        }

                        count++;
                        var json = webClient.DownloadString("https://api.xivdb.com/status/" + s.Id);
                        Model.StatusDetail statusDetail = JsonConvert.DeserializeObject<Model.StatusDetail>(json);

                        string iconFileName = String.Empty;
                        if (statusDetail.Icon != null)
                        {
                            string fileName = s.Icon + @".png";
                            string filePath = imgPath + @"\" + fileName;
                            Uri imgUri = new Uri(statusDetail.Icon);

                            downloadRate = (float)(count * 1000) / (stopwatch.ElapsedMilliseconds);
                            while (downloadRate > WEB_ACCESS_RATE_LIMIT)
                            {
                                Thread.Sleep(500);
                                downloadRate = (float)(count * 1000) / (stopwatch.ElapsedMilliseconds);
                            }

                            try
                            {
                                Console.WriteLine("Downloading: {0} (CurrentAccessRate= {1:F2} /sec)", fileName, downloadRate);
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

                Console.WriteLine("Writing JSON Files...");

                File.WriteAllText(jsonPath + @"\status.json", Newtonsoft.Json.JsonConvert.SerializeObject(status));
                File.WriteAllText(jsonPath + @"\status_en.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_en));
                File.WriteAllText(jsonPath + @"\status_fr.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_fr));
                File.WriteAllText(jsonPath + @"\status_de.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_de));
                File.WriteAllText(jsonPath + @"\status_ja.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_ja));

                File.WriteAllText(jsonPath + @"\status.js", "var statusArray = ");
                File.WriteAllText(jsonPath + @"\status_en.js", "var statusArray = ");
                File.WriteAllText(jsonPath + @"\status_fr.js", "var statusArray = ");
                File.WriteAllText(jsonPath + @"\status_de.js", "var statusArray = ");
                File.WriteAllText(jsonPath + @"\status_ja.js", "var statusArray = ");

                File.AppendAllText(jsonPath + @"\status.js", Newtonsoft.Json.JsonConvert.SerializeObject(status));
                File.AppendAllText(jsonPath + @"\status_en.js", Newtonsoft.Json.JsonConvert.SerializeObject(status_en));
                File.AppendAllText(jsonPath + @"\status_fr.js", Newtonsoft.Json.JsonConvert.SerializeObject(status_fr));
                File.AppendAllText(jsonPath + @"\status_de.js", Newtonsoft.Json.JsonConvert.SerializeObject(status_de));
                File.AppendAllText(jsonPath + @"\status_ja.js", Newtonsoft.Json.JsonConvert.SerializeObject(status_ja));

                File.AppendAllText(jsonPath + @"\status.js", ";");
                File.AppendAllText(jsonPath + @"\status_en.js", ";");
                File.AppendAllText(jsonPath + @"\status_fr.js", ";");
                File.AppendAllText(jsonPath + @"\status_de.js", ";");
                File.AppendAllText(jsonPath + @"\status_ja.js", ";");


                Console.WriteLine("Success.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error. {ex.Message})");
                return;
            }

            stopwatch.Stop();
            Console.WriteLine("Complete. Time= {0} seconds.", (float)(stopwatch.ElapsedMilliseconds/1000F));

        }
    }
}
