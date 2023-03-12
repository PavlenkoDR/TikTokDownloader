﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;

namespace TikTokDownloader
{

    public partial class MainPage : ContentPage
    {
        public string videoURL { get; set; } = "";
        public string debugText { get; set; } = "";
        public ImageSource linkImageSource { get => ImageSource.FromResource("TikTokDownloader.Assets.link.jpg"); }
        public ImageSource shareImageSource { get => ImageSource.FromResource("TikTokDownloader.Assets.share.jpg"); }
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }
        private bool oneShotPaste = false;
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (!oneShotPaste)
            {
                FirebaseCrashlyticsServiceInstance.Log("oneShotPaste");
                oneShotPaste = true;
                var text = DependencyService.Get<IClipBoardService>().Get();
                if (MatchTikTokUrl(text))
                {
                    FirebaseCrashlyticsServiceInstance.Log("oneShotPaste url matched");
                    urlEditor.Text = text;
                    videoURL = text;
                }
            }
        }

        private DownloadData getContentFromTikTok(string url)
        {
            FirebaseCrashlyticsServiceInstance.Log("getContentFromTikTok");
            string aweme_id = null;
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 5000;
                req.AllowAutoRedirect = false;
                var resp = req.GetResponse();
                string realUrl = resp.Headers["Location"];
                var match = Regex.Match(realUrl ?? "", ".*\\/(\\d*).*");
                if (match.Groups.Count > 1)
                {
                    aweme_id = match.Groups[1].Value;
                }
            }
            if (aweme_id == null)
            {
                HttpClient client = new HttpClient();
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
                var response = client.GetAsync(url, cancellationTokenSource.Token).Result;
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    FirebaseCrashlyticsServiceInstance.Log("getContentFromTikTok timeout");
                    return null;
                }
                var content = response.Content.ReadAsStringAsync().Result;
                var splittedContent = content.Split(new[] { "\"aweme_id\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                if (splittedContent.Length > 1)
                {
                    aweme_id = splittedContent[1].Split(new[] { "\"" }, 2, StringSplitOptions.RemoveEmptyEntries)[0];
                }
            }
            if (aweme_id != null)
            {
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage();
                request.RequestUri = new Uri($"https://api16-normal-c-useast1a.tiktokv.com/aweme/v1/feed/?aweme_id={aweme_id}");
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
                var response = client.SendAsync(request, cancellationTokenSource.Token).Result;
                var json = response.Content.ReadAsStringAsync().Result;
                JObject obj = null;
                try
                {
                    obj = JsonConvert.DeserializeObject<JObject>(json);
                }
                catch
                {
                    FirebaseCrashlyticsServiceInstance.Log("getContentFromTikTok parse json failed");
                    return null;
                }
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
                string dynamic_cover_description = null;
                string cover_description = null;
                var video = target_aweme["video"];
                if (video != null)
                {
                    if ((video["download_addr"]["url_list"] as JArray).Count > 0)
                    {
                        var download_addr = video["download_addr"]["url_list"][0].ToString();
                        url_video_list.Add(new UrlDescription()
                        {
                            url = download_addr,
                            description = "с водяным знаком",
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
                            description = $"без водяного знака",
                            fileName = $"no_watermark_{gear_name}_{aweme_id}.mp4"
                        });
                        break;
                    }
                    var cover_data = video;
                    if (cover_data != null)
                    {
                        {
                            var cover = cover_data["dynamic_cover"];
                            if (cover != null)
                            {
                                var url_list = cover["url_list"];
                                foreach (var cover_url in url_list as JArray)
                                {
                                    dynamic_cover_description = cover_url.ToString();
                                    break;
                                }
                            }
                        }
                        {
                            var cover = cover_data["origin_cover"];
                            if (cover != null)
                            {
                                var url_list = cover["url_list"];
                                foreach (var cover_url in url_list as JArray)
                                {
                                    cover_description = cover_url.ToString();
                                    break;
                                }
                            }
                        }
                        if (cover_description != null)
                        {
                            var cover = cover_data["cover"];
                            if (cover != null)
                            {
                                var url_list = cover["url_list"];
                                foreach (var cover_url in url_list as JArray)
                                {
                                    cover_description = cover_url.ToString();
                                    break;
                                }
                            }
                        }
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
                                description = $"{imageObjIdx}: изображения без водяного знака",
                                fileName = $"{imageObjIdx}_no_watermark_{aweme_id}.jpeg"
                            });
                        }
                        var owner_watermark_image = extractImageUrl(imageObj["owner_watermark_image"]["url_list"] as JArray);
                        if (owner_watermark_image != null)
                        {
                            url_owner_watermark_image_list.Add(new UrlDescription
                            {
                                url = owner_watermark_image,
                                description = $"{imageObjIdx}: изображения с водяным знаком",
                                fileName = $"{imageObjIdx}_watermark_{aweme_id}.jpeg"
                            });
                        }
                        var user_watermark_image = extractImageUrl(imageObj["user_watermark_image"]["url_list"] as JArray);
                        if (owner_watermark_image == null && user_watermark_image != null)
                        {
                            url_user_watermark_image_list.Add(new UrlDescription
                            {
                                url = user_watermark_image,
                                description = $"{imageObjIdx}: изображения с водяным знаком и значком пользователя",
                                fileName = $"{imageObjIdx}_user_watermark_{aweme_id}.jpeg"
                            });
                        }
                        var thumbnail = extractImageUrl(imageObj["thumbnail"]["url_list"] as JArray);
                        if (display_image == null && thumbnail != null)
                        {
                            url_thumbnail_list.Add(new UrlDescription
                            {
                                url = thumbnail,
                                description = $"{imageObjIdx}: миниатюры без водяного знака",
                                fileName = $"{imageObjIdx}_thumbnail_no_watermark_{aweme_id}.jpeg"
                            });
                        }
                        imageObjIdx++;
                    }
                }
                FirebaseCrashlyticsServiceInstance.Log("getContentFromTikTok success");
                return new DownloadData
                {
                    dynamic_cover = dynamic_cover_description,
                    cover = cover_description,
                    video_description = desc,
                    video_list = url_video_list,
                    music_description = title,
                    music_list = url_music_list.Select(x => new UrlDescription
                    {
                        url = x,
                        fileName = $"music_{aweme_id}.mp3"
                    }).ToList(),
                    url_display_image_list = url_display_image_list,
                    url_owner_watermark_image_list = url_owner_watermark_image_list,
                    url_user_watermark_image_list = url_user_watermark_image_list,
                    url_thumbnail_list = url_thumbnail_list
                };
            }
            FirebaseCrashlyticsServiceInstance.Log("getContentFromTikTok null");
            return null;
        }

        private DownloadData getContentFromDouyin(string url)
        {
            FirebaseCrashlyticsServiceInstance.Log("getContentFromDouyin");
            HttpClient client = new HttpClient();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));
            var response = client.GetAsync($"https://api.douyin.wtf/api?url={url}&minimal=false", cancellationTokenSource.Token).Result;
            if (cancellationTokenSource.IsCancellationRequested)
            {
                FirebaseCrashlyticsServiceInstance.Log("getContentFromDouyin timeout");
                return null;
            }
            var json = response.Content.ReadAsStringAsync().Result;
            JObject obj = null;
            try
            {
                obj = JsonConvert.DeserializeObject<JObject>(json);
            }
            catch
            {
                FirebaseCrashlyticsServiceInstance.Log("getContentFromDouyin parse json failed");
            }
            if (obj == null)
            {
                return null;
            }

            var status = obj["status"].ToString();

            if (status == "failed")
            {
                FirebaseCrashlyticsServiceInstance.Log("getContentFromDouyin failed");
                return null;
            }
            else if (status == "success")
            {
                var aweme_id = obj["aweme_id"].ToString();
                var desc = obj["desc"].ToString();

                SortedSet<string> music_url_list = new SortedSet<string>();
                var title = "";
                var music = obj["music"];
                if (music != null)
                {
                    title = music["title"].ToString();
                    music_url_list.Add(music["play_url"]["uri"].ToString());
                    foreach (var play_url in music["play_url"]["url_list"] as JArray)
                    {
                        music_url_list.Add(play_url.ToString());
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
                            url = wm_video_url_HQ,
                            description = "с водяным знаком",
                            fileName = $"watermark_hq_{aweme_id}.mp4"
                            },
                        new UrlDescription{
                            url = nwm_video_url_HQ,
                            description = "без водяного знака",
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

                string dynamic_cover_description = null;
                string cover_description = null;
                var cover_data = obj["cover_data"];
                if (cover_data != null)
                {
                    {
                        var cover = cover_data["dynamic_cover"];
                        if (cover != null)
                        {
                            var url_list = cover["url_list"];
                            foreach (var cover_url in url_list as JArray)
                            {
                                dynamic_cover_description = cover_url.ToString();
                                break;
                            }
                        }
                    }
                    {
                        var cover = cover_data["origin_cover"];
                        if (cover != null)
                        {
                            var url_list = cover["url_list"];
                            foreach (var cover_url in url_list as JArray)
                            {
                                cover_description = cover_url.ToString();
                                break;
                            }
                        }
                    }
                    if (cover_description != null)
                    {
                        var cover = cover_data["cover"];
                        if (cover != null)
                        {
                            var url_list = cover["url_list"];
                            foreach (var cover_url in url_list as JArray)
                            {
                                cover_description = cover_url.ToString();
                                break;
                            }
                        }
                    }
                }

                FirebaseCrashlyticsServiceInstance.Log("getContentFromDouyin success");
                return new DownloadData
                {
                    dynamic_cover = dynamic_cover_description,
                    cover = cover_description,
                    video_description = desc,
                    video_list = video_list,
                    music_description = title,
                    music_list = music_url_list.Select(x => new UrlDescription
                    {
                        url = x,
                        fileName = $"music_{aweme_id}.mp3"
                    }).ToList(),
                    url_display_image_list = no_watermark_image_list,
                    url_user_watermark_image_list = watermark_image_list,
                    url_owner_watermark_image_list = new List<UrlDescription>(),
                    url_thumbnail_list = new List<UrlDescription>()
                };
            }
            FirebaseCrashlyticsServiceInstance.Log("getContentFromDouyin null");
            return null;
        }

        private bool MatchTikTokUrl(string url)
        {
            FirebaseCrashlyticsServiceInstance.Log("MatchTikTokUrl");
            return Regex.Match(url ?? "", "http.://.*tiktok.*/.*").Success;
        }

        private async Task WaitCancelToken(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Thread.Yield();
            }
        }

        private async void Button_ClickedAsync(object sender, EventArgs e)
        {
            FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync");
            (sender as Button).IsEnabled = false;

            var fileService = DependencyService.Get<IFileService>();
            bool permissionsGranted = await fileService.CheckPermissions();
            if (!permissionsGranted)
            {
                FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync permissions not granted");
                bool answer = await DisplayAlert("Нет прав доступа к хранилищу файлов", "Чтобы исправить проблему, можно переустановить программу и разрешить доступ, либо дать доступ в настройках. Открыть настройки приложения?", "Да", "Нет");
                if (answer)
                {
                    FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync open settings");
                    var isOpened = fileService.OpenAppSettings();
                    if (!isOpened)
                    {
                        FirebaseCrashlyticsServiceInstance.Log("setting not opened");
                        FirebaseCrashlyticsServiceInstance.RecordException(new Exception("can not open settings"));
                        FirebaseCrashlyticsServiceInstance.SendUnsentReports();
                        await DisplayAlert("Нет возможности открыть настройки", "Откройте настройки, найдите там приложение и разрешите доступ к хранилищу файлов вручную", "Хорошо");
                    }
                }
                else
                {
                    FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync grant permissions ignored");
                    await DisplayAlert("Отказано в правах доступа", "Программа не может работать без прав доступа к файлам", "Я понимаю");
                }
                (sender as Button).IsEnabled = true;
                return;
            }
            var paths = new[] { await fileService.getDownloadsPath(), await fileService.getGalleryPath() };

            if (!MatchTikTokUrl(videoURL))
            {
                FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync url not matched");
                DependencyService.Get<IToastService>().MakeText("Вставьте ТикТок ссылку в поле");
                (sender as Button).IsEnabled = true;
                return;
            }
            var banner = new DownloadBanner();
            await Navigation.PushModalAsync(banner);
            CancellationTokenSource cancellationTokenSource = null;
            int failsCount = 0;
            Mutex mutex = new Mutex();
            Func<Func<string, DownloadData>, Task<DownloadData>> downloadLauncher = async (Func<string, DownloadData> task) =>
            {
                var taskResult = task(videoURL);
                if (taskResult == null)
                {
                    mutex.WaitOne();
                    ++failsCount;
                    if (failsCount == 2)
                    {
                        cancellationTokenSource.Cancel();
                    }
                    mutex.ReleaseMutex();
                    await WaitCancelToken(cancellationTokenSource.Token);
                }
                return taskResult;
            };

            DownloadData result = null;
            for (int i = 1; i < 4; i++)
            {
                banner.tryCount = i;
                cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(25));
                failsCount = 0;
                try
                {
                    result = await await Task.WhenAny(new[]{
                        downloadLauncher(getContentFromTikTok),
                        downloadLauncher(getContentFromDouyin)
                    });
                }
                catch (Exception ex)
                {
                    FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync getContent* exception");
                    FirebaseCrashlyticsServiceInstance.RecordException(ex);
                }
                if (result != null)
                {
                    break;
                }
                cancellationTokenSource.Cancel();
            }
            
            
            await Navigation.PopModalAsync();
            if (result != null)
            {
                FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync success");
                await Navigation.PushAsync(new DownloadPage(result));
            }
            else
            {
                DependencyService.Get<IToastService>().MakeText("Попытайтесь снова");
            }
            (sender as Button).IsEnabled = true;
        }

        private void ImageButton_Clicked(object sender, EventArgs e)
        {
            FirebaseCrashlyticsServiceInstance.Log("ImageButton_Clicked");
            urlEditor.Text = "";
            videoURL = "";
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            FirebaseCrashlyticsServiceInstance.Log("Button_Clicked");
            var text = DependencyService.Get<IClipBoardService>().Get();
            urlEditor.Text = text;
            videoURL = text;
        }
    }
}
