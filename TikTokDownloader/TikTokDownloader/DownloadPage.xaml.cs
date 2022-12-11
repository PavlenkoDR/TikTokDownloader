using System;
using System.Collections.Generic;
using System.Net.Http;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TikTokDownloader
{
    public struct UrlDescription
    {
        public string url { get; set; }
        private string _description;
        public string description { get => $"Download {_description}"; set => _description = value; }
        public string descriptionRaw { get => _description; }
    }
    public struct DownloadData
    {
        public string video_description { get; set; }
        public List<UrlDescription> video_list { get; set; }

        public string music_description { get; set; }
        public List<UrlDescription> music_list { get; set; }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DownloadPage : ContentPage
    {
        public DownloadData data { get; }
        public DownloadPage(DownloadData data)
        {
            InitializeComponent();

            this.data = data;
            BindingContext = this;
        }

        private async void Download(object sender, string description)
        {
            await Navigation.PushModalAsync(new DownloadBanner());
            var button = sender as Button;
            var client = new HttpClient();

            var downloadInfo = (UrlDescription)button?.CommandParameter;
            var uri = new Uri(downloadInfo.url);
            var fileName = $"{downloadInfo.descriptionRaw} {description}";
            //var path = Path.Combine(Xamarin.Essentials.FileSystem.AppDataDirectory, "test.mp4");

            // download the file
            var downloadBytes = await client.GetByteArrayAsync(uri);
            // save file on disk
            await DependencyService.Get<IFileService>().Save(downloadBytes, fileName);
            await Navigation.PopModalAsync();
            await DisplayAlert("Success!", $"Saved in downloads", "OK");
        }

        private void Button_Video_Clicked(object sender, EventArgs e)
        {
            Download(sender, $"{data.video_description}.mp4");
        }

        private void Button_Music_Clicked(object sender, EventArgs e)
        {
            Download(sender, $"{data.music_description}.mp3");
        }
    }
}