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
        const float WEB_ACCESS_RATE_LIMIT = 4.0F;

        static void Main(string[] args)
        {
            DownloadStatusData();
            Console.WriteLine("To exit application, press \"Enter\".");
            Console.ReadLine();
        }

        static void DownloadStatusData()
        {
            string jsonPath = @"resources\json\status";
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


            string imgPath = @"resources\images\status";
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

            using (WebClient webClient = new WebClient())
            {
                try
                {
                    Console.WriteLine("Donloading Status Metadata...");
                    escapedJson = webClient.DownloadString("https://api.xivdb.com/status?columns=id,icon,name,name_en,name_fr,name_de,name_ja");
                    Console.WriteLine("Success.");
                }
                catch (WebException wex)
                {
                    Console.WriteLine($"Error. {wex.Message} ({wex.Status})");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error. {ex.Message})");
                    return;
                }

            }


            List<Model.Status> status = JsonConvert.DeserializeObject<List<Model.Status>>(escapedJson);
            Console.WriteLine("Generating JSON files...");
            try
            {
                File.WriteAllText(jsonPath + @"\status.json", Newtonsoft.Json.JsonConvert.SerializeObject(status));

                List<Model.Status_En> status_en = new List<Model.Status_En>();
                List<Model.Status_Fr> status_fr = new List<Model.Status_Fr>();
                List<Model.Status_De> status_de = new List<Model.Status_De>();
                List<Model.Status_Ja> status_ja = new List<Model.Status_Ja>();

                foreach (var s in status)
                {
                    status_en.Add(new Model.Status_En { id = s.id, icon = s.icon, name = s.name, name_en = s.name_en });
                    status_fr.Add(new Model.Status_Fr { id = s.id, icon = s.icon, name = s.name, name_fr = s.name_fr });
                    status_de.Add(new Model.Status_De { id = s.id, icon = s.icon, name = s.name, name_de = s.name_de });
                    status_ja.Add(new Model.Status_Ja { id = s.id, icon = s.icon, name = s.name, name_ja = s.name_ja });
                }

                File.WriteAllText(jsonPath + @"\status_en.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_en));
                File.WriteAllText(jsonPath + @"\status_fr.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_fr));
                File.WriteAllText(jsonPath + @"\status_de.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_de));
                File.WriteAllText(jsonPath + @"\status_ja.json", Newtonsoft.Json.JsonConvert.SerializeObject(status_ja));

                Console.WriteLine("Success.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error. {ex.Message})");
                return;
            }


            Stopwatch stopwatch = new Stopwatch();
            float downloadRate = 0.0F;
            int count = 0;

            Console.WriteLine("Download Icons...");
            Console.WriteLine("Access Limit: {0:F2} per second.", WEB_ACCESS_RATE_LIMIT);

            using (WebClient webClient = new WebClient())
            {
                stopwatch.Start();

                foreach (var s in status)
                {
                    var json = webClient.DownloadString("https://api.xivdb.com/status/" + s.id);
                    count++;
                    Model.StatusDetail statusDetail = JsonConvert.DeserializeObject<Model.StatusDetail>(json);

                    if (statusDetail.icon == null) continue;


                    string fileName = s.icon + @".png";
                    string filePath = imgPath + @"\" + fileName;
                    Uri imgUri = new Uri(statusDetail.icon);

                    try
                    {
                        Console.WriteLine("Downloading: {0}   DownloadRate={1:F2}", fileName, downloadRate);
                        webClient.DownloadFile(imgUri, filePath);
                    }
                    catch (WebException wex)
                    {
                        Console.WriteLine($" => {wex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" => {ex.Message}");
                    }
                    count++;

                    downloadRate = (float)(count * 1000) / (stopwatch.ElapsedMilliseconds);
                    while (downloadRate > WEB_ACCESS_RATE_LIMIT)
                    {
                        Thread.Sleep(500);
                        downloadRate = (float)(count * 1000) / (stopwatch.ElapsedMilliseconds);
                    }
                }

                stopwatch.Stop();


            }
            Console.WriteLine("Complete. Time= {0} seconds.", stopwatch.Elapsed.Seconds);



        }
    }
}
