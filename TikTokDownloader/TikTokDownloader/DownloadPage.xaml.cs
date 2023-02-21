using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TikTokDownloader
{
    public class UrlDescription
    {
        public string url { get; set; }
        private string _description;
        public string description { get => $"Скачать {_description}"; set => _description = value; }
        public string fileName { get; set; }
        public string shareFilesPath { get; set; } = null;
    }
    public class DownloadData
    {
        public string video_description { get; set; }
        public List<UrlDescription> video_list { get; set; }

        public string music_description { get; set; }
        public List<UrlDescription> music_list { get; set; }

        public List<UrlDescription> url_display_image_list { get; set; }
        public List<UrlDescription> url_owner_watermark_image_list { get; set; }
        public List<UrlDescription> url_user_watermark_image_list { get; set; }
        public List<UrlDescription> url_thumbnail_list { get; set; }
    }

    public struct ImageUrlDescription
    {
        public string description { get; set; }
        public List<UrlDescription> downloadInfo { get; set; }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DownloadPage : ContentPage
    {
        public DownloadData data { get; }
        public List<ImageUrlDescription> imageList { get; } = new List<ImageUrlDescription>();
        public bool isSaveToDownloads { get; set; } = false;
        public bool isHaveVideos { get => data.video_list.Count > 0; }
        public bool isHaveMusic { get => data.music_list.Count > 0; }
        public bool isHaveDescription { get => data.video_description != null && data.video_description.Length > 0; }
        public bool isHaveImages { get => imageList.Count > 0; }
        public DownloadPage(DownloadData data)
        {
            InitializeComponent();

            this.data = data;

            if (data.url_display_image_list.Count > 0)
            {
                imageList.Add(new ImageUrlDescription { description = "Скачать изображения без водяного знака", downloadInfo = data.url_display_image_list });
            }

            if (data.url_owner_watermark_image_list.Count > 0)
            {
                imageList.Add(new ImageUrlDescription { description = "Скачать изображения с водяным знаком", downloadInfo = data.url_owner_watermark_image_list });
            }

            if (data.url_user_watermark_image_list.Count > 0)
            {
                imageList.Add(new ImageUrlDescription { description = "Скачать изображения с водяным знаком и значком пользователя", downloadInfo = data.url_user_watermark_image_list });
            }

            if (data.url_thumbnail_list.Count > 0)
            {
                imageList.Add(new ImageUrlDescription { description = "Скачать миниатюру без водяного знака", downloadInfo = data.url_thumbnail_list });
            }

            BindingContext = this;
        }

        private async Task DownloadAndSave(UrlDescription downloadInfo)
        {
            var client = new HttpClient();
            var uri = new Uri(downloadInfo.url);
            var downloadBytes = await client.GetByteArrayAsync(uri);
            var result = await DependencyService.Get<IFileService>().Save(downloadBytes, downloadInfo.fileName, isSaveToDownloads);
            downloadInfo.shareFilesPath = result;
        }

        private async Task DownloadAndSave(List<UrlDescription> downloadInfo)
        {
            foreach (var info in downloadInfo)
            {
                await DownloadAndSave(info);
            }
        }

        private async void DownloadClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            button.IsEnabled = false;
            await Navigation.PushModalAsync(new DownloadBanner());

            if (button.CommandParameter is UrlDescription)
            {
                var downloadInfo = (UrlDescription)button?.CommandParameter;
                await DownloadAndSave(downloadInfo);
            }
            else if (button.CommandParameter is List<UrlDescription>)
            {
                var downloadInfo = (List<UrlDescription>)button?.CommandParameter;
                await DownloadAndSave(downloadInfo);
            }

            await Navigation.PopModalAsync();
            DependencyService.Get<IToastService>().MakeText(isSaveToDownloads ? "Сохранено в загрузки" : "Сохранено в галерею");
            button.BackgroundColor = Color.LightGreen;
            button.Text = "Поделиться";
            button.Clicked -= DownloadClicked;
            button.Clicked += Share_Clicked;
            button.IsEnabled = true;
        }

        private void TabbedSwitch_OnSwitch(object sender, OnSwitchArgs e)
        {
            isSaveToDownloads = e.SelectedIndex == 1;
        }

        private string getIntentType(UrlDescription description)
        {
            string intentType = "";
            if (data.video_list.Contains(description))
            {
                intentType = "video/mp4";
            }
            else if (data.music_list.Contains(description))
            {
                intentType = "music/mp3";
            }
            else if (data.url_display_image_list.Contains(description))
            {
                intentType = "image/jpg";
            }
            else if (data.url_owner_watermark_image_list.Contains(description))
            {
                intentType = "image/jpg";
            }
            else if (data.url_user_watermark_image_list.Contains(description))
            {
                intentType = "image/jpg";
            }
            else if (data.url_thumbnail_list.Contains(description))
            {
                intentType = "image/jpg";
            }
            return intentType;
        }

        private string getIntentTitle(UrlDescription description)
        {
            string title = data.video_description;
            if (data.music_list.Contains(description))
            {
                title = data.music_description;
            }
            return title;
        }

        private void Share_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            button.IsEnabled = false;

            if (button.CommandParameter is UrlDescription)
            {
                var downloadInfo = (UrlDescription)button?.CommandParameter;
                if (downloadInfo.shareFilesPath == null)
                {
                    DependencyService.Get<IToastService>().MakeText("Файл не найден");
                    button.IsEnabled = true;
                    return;
                }
                List<string> filePaths = new List<string>();
                filePaths.Add(downloadInfo.shareFilesPath);

                string intentType = getIntentType(downloadInfo);
                string intentTitle = getIntentTitle(downloadInfo);
                DependencyService.Get<IFileService>().ShareMediaFile(intentTitle, filePaths.ToArray(), intentType);
            }
            else if (button.CommandParameter is List<UrlDescription>)
            {
                var downloadInfos = (List<UrlDescription>)button?.CommandParameter;
                List<string> filePaths = new List<string>();
                string intentType = "";
                string intentTitle = "";
                foreach (var downloadInfo in downloadInfos)
                {
                    if (downloadInfo.shareFilesPath == null)
                    {
                        DependencyService.Get<IToastService>().MakeText("Файлы не найдены");
                        button.IsEnabled = true;
                        return;
                    }
                    if (intentType.Length == 0)
                    {
                        intentType = getIntentType(downloadInfo);
                    }
                    if (intentTitle.Length == 0)
                    {
                        intentTitle = getIntentTitle(downloadInfo);
                    }
                    filePaths.Add(downloadInfo.shareFilesPath);
                }

                DependencyService.Get<IFileService>().ShareMediaFile(intentTitle, filePaths.ToArray(), intentType);
            }

            button.IsEnabled = true;
        }
    }
}