using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TikTokDownloader
{

    public partial class MainPage : ContentPage
    {
        public string videoURL { get; set; }
        public string debugText { get; set; }
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        private async Task getContentFromTikTok(string url)
        {
            string aweme_id = null;
            {
                HttpClient client = new HttpClient();
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                var splittedContent = content.Split(new[] { "\"aweme_id\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                if (splittedContent.Length > 1)
                {
                    aweme_id = splittedContent[1].Split(new[] { "\"" }, 2, StringSplitOptions.RemoveEmptyEntries)[0];
                }
            }
            if (aweme_id != null)
            {
                HttpClient client = new HttpClient();
                var response = await client.GetAsync($"https://api16-normal-c-useast1a.tiktokv.com/aweme/v1/feed/?aweme_id={aweme_id}&iid=6165993682518218889&device_id=60318820105&aid=1180");
                var json = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<JObject>(json);
                var aweme_list = obj["aweme_list"] as JArray;
                var target_aweme = aweme_list[0];

                var desc = target_aweme["desc"].ToString();

                var music = target_aweme["music"];
                var title = music["title"].ToString();
                SortedSet<string> url_music_list = new SortedSet<string>();
                url_music_list.Add(music["play_url"]["uri"].ToString());
                foreach (var play_url in music["play_url"]["url_list"] as JArray)
                {
                    url_music_list.Add(play_url.ToString());
                }

                List<UrlDescription> url_video_list = new List<UrlDescription>();
                var video = target_aweme["video"];
                var download_addr = video["download_addr"]["url_list"][0].ToString();
                url_video_list.Add(new UrlDescription() { url = download_addr, description = "with watermark" });
                
                var bit_rate = video["bit_rate"] as JArray;
                foreach (var url_video in bit_rate)
                {
                    var gear_name = url_video["gear_name"].ToString();
                    var play_addr = url_video["play_addr"]["url_list"][0].ToString();
                    url_video_list.Add(new UrlDescription() { url = play_addr, description = $"with no watermark {gear_name}" });
                }
                _ = Navigation.PushAsync(new DownloadPage(new DownloadData
                {
                    video_description = desc,
                    video_list = url_video_list,
                    music_description = title,
                    music_list = url_music_list.Select(x => new UrlDescription { url = x }).ToList()
                }));
            }
        }

        private async Task getContentFromDouyin(string url)
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync($"https://api.douyin.wtf/api?url={url}&minimal=false");
            var json = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<JObject>(json);

            var status = obj["status"].ToString();

            if (status == "failed")
            {
                var message = obj["message"].ToString();
                var messageSplitted = message.Split('/');
                if (messageSplitted.Length > 1)
                {
                    throw new Exception(messageSplitted[1]);
                }
                else
                {
                    throw new Exception(message);
                }
            }
            else if (status == "success")
            {
                var aweme_id = obj["aweme_id"].ToString();
                var desc = obj["desc"].ToString();

                var music = obj["music"];
                var title = music["title"].ToString();
                SortedSet<string> url_list = new SortedSet<string>();
                url_list.Add(music["play_url"]["uri"].ToString());
                foreach (var play_url in music["play_url"]["url_list"] as JArray)
                {
                    url_list.Add(play_url.ToString());
                }

                var video_data = obj["video_data"];
                var wm_video_url = video_data["wm_video_url"].ToString();
                var wm_video_url_HQ = video_data["wm_video_url_HQ"].ToString();
                var nwm_video_url = video_data["nwm_video_url"].ToString();
                var nwm_video_url_HQ = video_data["nwm_video_url_HQ"].ToString();

                _ = Navigation.PushAsync(new DownloadPage(new DownloadData
                {
                    video_description = desc,
                    video_list = new List<UrlDescription>{
                            new UrlDescription{ url = wm_video_url, description = "with watermark" },
                            new UrlDescription{ url = wm_video_url_HQ, description = "with watermark high quality" },
                            new UrlDescription{ url = nwm_video_url, description = "with no watermark" },
                            new UrlDescription{ url = nwm_video_url_HQ, description = "with no watermark high quality" }
                        },
                    music_description = title,
                    music_list = url_list.Select(x => new UrlDescription { url = x }).ToList()
                }));
            }
            else
            {
                throw new Exception("Unknown Douyin API status");
            }
        }

        private async void Button_ClickedAsync(object sender, EventArgs e)
        {
            bool completed = false;
            string errors = "";
            await Navigation.PushModalAsync(new DownloadBanner());
            Func<Func<string, Task>, Task> downloadLauncher = async (Func<string, Task> task) =>
            {
                if (!completed)
                {
                    try
                    {
                        await task(videoURL);
                        completed = true;
                    }
                    catch (Exception ex)
                    {
                        errors += $"{ex.Message}\n";
                    }
                }
            };
            
            await downloadLauncher(getContentFromTikTok);
            await downloadLauncher(getContentFromDouyin);
            
            await Navigation.PopModalAsync();
            if (!completed)
            {
                await DisplayAlert("Something went wrong", errors, "OK");
            }
        }
    }
}
