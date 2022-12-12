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
        public ImageSource cancelImageSource { get => ImageSource.FromResource("TikTokDownloader.Assets.cancel.png"); }
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

                SortedSet<string> url_music_list = new SortedSet<string>();
                string title = "";
                var music = target_aweme["music"];
                if (music != null)
                {
                    title = music["title"].ToString();
                    url_music_list.Add(music["play_url"]["uri"].ToString());
                    foreach (var play_url in music["play_url"]["url_list"] as JArray)
                    {
                        url_music_list.Add(play_url.ToString());
                    }
                }

                List<UrlDescription> url_video_list = new List<UrlDescription>();
                var video = target_aweme["video"];
                if (video != null)
                {
                    if ((video["download_addr"]["url_list"] as JArray).Count > 0)
                    {
                        var download_addr = video["download_addr"]["url_list"][0].ToString();
                        url_video_list.Add(new UrlDescription()
                        {
                            url = download_addr,
                            description = "with watermark",
                            fileName = $"watermark_{aweme_id}.mp4"
                        });
                    }

                    var bit_rate = video["bit_rate"] as JArray;
                    foreach (var url_video in bit_rate)
                    {
                        var gear_name = url_video["gear_name"].ToString();
                        var play_addr = url_video["play_addr"]["url_list"][0].ToString();
                        url_video_list.Add(new UrlDescription()
                        {
                            url = play_addr,
                            description = $"with no watermark {gear_name}",
                            fileName = $"no_watermark_{gear_name}_{aweme_id}.mp4"
                        });
                    }
                }

                List<UrlDescription> url_display_image_list = new List<UrlDescription>();
                List<UrlDescription> url_owner_watermark_image_list = new List<UrlDescription>();
                List<UrlDescription> url_user_watermark_image_list = new List<UrlDescription>();
                List<UrlDescription> url_thumbnail_list = new List<UrlDescription>();
                var image_post_info = target_aweme["image_post_info"];
                if (image_post_info != null)
                {
                    var images = image_post_info["images"] as JArray;

                    Func<JArray, string> extractImageUrl = (image_post_info_obj_url_list) =>
                    {
                        if (image_post_info_obj_url_list.Count > 0)
                        {
                            var extractedUrlList = image_post_info_obj_url_list.Where(x => x.ToString().Contains(".jpeg")).ToList();
                            return (extractedUrlList.Count > 0 ? extractedUrlList[0] : image_post_info_obj_url_list[0]).ToString();
                        }
                        return null;
                    };

                    int imageObjIdx = 1;
                    foreach (var imageObj in images)
                    {
                        var display_image = extractImageUrl(imageObj["display_image"]["url_list"] as JArray);
                        if (display_image != null)
                        {
                            url_display_image_list.Add(new UrlDescription
                            {
                                url = display_image,
                                description = $"{imageObjIdx}: no watermark images",
                                fileName = $"{imageObjIdx}_no_watermark_{aweme_id}.jpeg"
                            });
                        }
                        var owner_watermark_image = extractImageUrl(imageObj["owner_watermark_image"]["url_list"] as JArray);
                        if (owner_watermark_image != null)
                        {
                            url_owner_watermark_image_list.Add(new UrlDescription
                            {
                                url = owner_watermark_image,
                                description = $"{imageObjIdx}: watermark images",
                                fileName = $"{imageObjIdx}_watermark_{aweme_id}.jpeg"
                            });
                        }
                        var user_watermark_image = extractImageUrl(imageObj["user_watermark_image"]["url_list"] as JArray);
                        if (user_watermark_image != null)
                        {
                            url_user_watermark_image_list.Add(new UrlDescription
                            {
                                url = user_watermark_image,
                                description = $"{imageObjIdx}: watermark images with user sign",
                                fileName = $"{imageObjIdx}_user_watermark_{aweme_id}.jpeg"
                            });
                        }
                        var thumbnail = extractImageUrl(imageObj["thumbnail"]["url_list"] as JArray);
                        if (thumbnail != null)
                        {
                            url_thumbnail_list.Add(new UrlDescription
                            {
                                url = thumbnail,
                                description = $"{imageObjIdx}: thumbnail no watermark images",
                                fileName = $"{imageObjIdx}_thumbnail_no_watermark_{aweme_id}.jpeg"
                            });
                        }
                        imageObjIdx++;
                    }
                }
                _ = Navigation.PushAsync(new DownloadPage(new DownloadData
                {
                    video_description = desc,
                    video_list = url_video_list,
                    music_description = title,
                    music_list = url_music_list.Select(x => new UrlDescription {
                        url = x,
                        fileName = $"music_{aweme_id}.mp3"
                    }).ToList(),
                    url_display_image_list = url_display_image_list,
                    url_owner_watermark_image_list = url_owner_watermark_image_list,
                    url_user_watermark_image_list = url_user_watermark_image_list,
                    url_thumbnail_list = url_thumbnail_list
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

                SortedSet<string> url_list = new SortedSet<string>();
                var title = "";
                var music = obj["music"];
                if (music != null)
                {
                    title = music["title"].ToString();
                    url_list.Add(music["play_url"]["uri"].ToString());
                    foreach (var play_url in music["play_url"]["url_list"] as JArray)
                    {
                        url_list.Add(play_url.ToString());
                    }
                }

                var video_list = new List<UrlDescription>();
                var video_data = obj["video_data"];
                if (video_data != null)
                {
                    var wm_video_url = video_data["wm_video_url"].ToString();
                    var wm_video_url_HQ = video_data["wm_video_url_HQ"].ToString();
                    var nwm_video_url = video_data["nwm_video_url"].ToString();
                    var nwm_video_url_HQ = video_data["nwm_video_url_HQ"].ToString();

                    video_list = new List<UrlDescription>{
                        new UrlDescription{
                            url = wm_video_url,
                            description = "with watermark",
                            fileName = $"watermark_{aweme_id}.mp4"
                        },
                        new UrlDescription{
                            url = wm_video_url_HQ,
                            description = "with watermark high quality",
                            fileName = $"watermark_hq_{aweme_id}.mp4"
                            },
                        new UrlDescription{
                            url = nwm_video_url,
                            description = "with no watermark",
                            fileName = $"no_watermark_{aweme_id}.mp4"
                            },
                        new UrlDescription{
                            url = nwm_video_url_HQ,
                            description = "with no watermark high quality",
                            fileName = $"no_watermark_hq_{aweme_id}.mp4"
                            }
                    };
                }


                var no_watermark_image_list = new List<UrlDescription>();
                var watermark_image_list = new List<UrlDescription>();
                var image_data = obj["image_data"];
                if (image_data != null)
                {
                    {
                        int idx = 1;
                        no_watermark_image_list = (image_data["no_watermark_image_list"] as JArray)
                            .Select(x => new UrlDescription { url = x.ToString(), fileName = $"{idx++}_no_watermark_{aweme_id}.jpeg" }).ToList();
                    }
                    {
                        int idx = 1;
                        watermark_image_list = (image_data["watermark_image_list"] as JArray)
                            .Select(x => new UrlDescription { url = x.ToString(), fileName = $"{idx++}_watermark_{aweme_id}.jpeg" }).ToList();
                    }
                }

                _ = Navigation.PushAsync(new DownloadPage(new DownloadData
                {
                    video_description = desc,
                    video_list = video_list,
                    music_description = title,
                    music_list = url_list.Select(x => new UrlDescription { 
                        url = x,
                        fileName = $"music_{aweme_id}.mp3"
                    }).ToList(),
                    url_display_image_list = no_watermark_image_list,
                    url_user_watermark_image_list = watermark_image_list,
                    url_owner_watermark_image_list = new List<UrlDescription>(),
                    url_thumbnail_list = new List<UrlDescription>()
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

        private void ImageButton_Clicked(object sender, EventArgs e)
        {
            urlEditor.Text = "";
            videoURL = "";
        }
    }
}
