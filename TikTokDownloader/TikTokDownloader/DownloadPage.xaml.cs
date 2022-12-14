using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TikTokDownloader
{
    public struct UrlDescription
    {
        public string url { get; set; }
        private string _description;
        public string description { get => $"Download {_description}"; set => _description = value; }
        public string fileName { get; set; }
    }
    public struct DownloadData
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
        public bool isSaveToDownloads { get; set; }
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
                imageList.Add(new ImageUrlDescription { description = "Download no watermark images", downloadInfo = data.url_display_image_list });
            }

            if (data.url_owner_watermark_image_list.Count > 0)
            {
                imageList.Add(new ImageUrlDescription { description = "Download watermark images", downloadInfo = data.url_owner_watermark_image_list });
            }

            if (data.url_user_watermark_image_list.Count > 0)
            {
                imageList.Add(new ImageUrlDescription { description = "Download watermark images with user sign", downloadInfo = data.url_user_watermark_image_list });
            }

            if (data.url_thumbnail_list.Count > 0)
            {
                imageList.Add(new ImageUrlDescription { description = "Download thumbnail no watermark images", downloadInfo = data.url_thumbnail_list });
            }

            BindingContext = this;
        }

        private async Task DownloadAndSave(UrlDescription downloadInfo)
        {
            var client = new HttpClient();
            var uri = new Uri(downloadInfo.url);
            var downloadBytes = await client.GetByteArrayAsync(uri);
            await DependencyService.Get<IFileService>().Save(downloadBytes, downloadInfo.fileName, isSaveToDownloads);
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
            await Navigation.PushModalAsync(new DownloadBanner());
            var button = sender as Button;

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
            await DisplayAlert("Success!", isSaveToDownloads ? "Saved in downloads" : "Saved in gallery", "OK");
        }
    }
}