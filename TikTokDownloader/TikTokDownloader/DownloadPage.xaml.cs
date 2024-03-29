﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Linq;
using static Xamarin.Forms.BindableProperty;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using static TikTokDownloader.ContentDownloadManager;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace TikTokDownloader
{
    public struct ImageUrlDescription
    {
        public string description { get; set; }
        public string descriptionShare { get => TranslateExtension.GetText("Share"); }
        public List<UrlDescription> downloadInfo { get; set; }
    }

    public class DownloadButton : Xamarin.Forms.Button
    {

        public static List<DownloadButton> allDownloadButtons = new List<DownloadButton>();

        public static readonly BindableProperty UrlDescriptionProperty = Create(
            "UrlDescription",
            typeof(object),
            typeof(DownloadButton)
        );
        public static readonly BindableProperty UrlDescriptionsProperty = Create(
            "UrlDescriptions",
            typeof(object),
            typeof(DownloadButton)
        );
        public static readonly BindableProperty NotDownloadedDescriptionProperty = Create(
            "NotDownloadedDescription",
            typeof(string),
            typeof(DownloadButton)
        );
        public static readonly BindableProperty DownloadedDescriptionProperty = Create(
            "DownloadedDescription",
            typeof(string),
            typeof(DownloadButton)
        );

        public UrlDescription UrlDescription
        {
            get
            {
                return GetValue(UrlDescriptionProperty) as UrlDescription;
            }
            set
            {
                SetValue(UrlDescriptionProperty, value);
                OnPropertyChanged("UrlDescription");
            }
        }
        public List<UrlDescription> UrlDescriptions
        {
            get
            {
                return GetValue(UrlDescriptionsProperty) as List<UrlDescription>;
            }
            set
            {
                SetValue(UrlDescriptionsProperty, value);
                OnPropertyChanged("UrlDescriptions");
            }
        }
        private Color? _DownloadedColor;
        public Color DownloadedColor {
            get => _DownloadedColor.Value;
            set
            {
                _DownloadedColor = value;
                OnPropertyChanged("DownloadedColor");
            }
        }
        private Color? _NotDownloadedColor;
        public Color NotDownloadedColor
        {
            get => _NotDownloadedColor.Value;
            set
            {
                _NotDownloadedColor = value;
                OnPropertyChanged("NotDownloadedColor");
            }
        }
        public string DownloadedDescription
        {
            get
            {
                return GetValue(DownloadedDescriptionProperty) as string;
            }
            set
            {
                SetValue(DownloadedDescriptionProperty, value);
                OnPropertyChanged("DownloadedDescription");
            }
        }
        public string NotDownloadedDescription
        {
            get
            {
                return GetValue(NotDownloadedDescriptionProperty) as string;
            }
            set
            {
                SetValue(NotDownloadedDescriptionProperty, value);
                OnPropertyChanged("NotDownloadedDescription");
            }
        }

        public bool isDownloaded { get; private set; } = true;
        public ContentType contentType { get; set; }
        public void checkIsDownloaded()
        {
            FirebaseCrashlyticsServiceInstance.Log("checkIsDownloaded");
            try
            {
                isDownloaded = true;
                var fileService = DependencyService.Get<IFileService>();
                var paths = new[] { fileService.getGalleryPath(), fileService.getMusicPath() };

                if (UrlDescription != null)
                {
                    bool findedInPath = false;
                    foreach (var path in paths)
                    {
                        var filePath = Path.Combine(path, UrlDescription.fileName);
                        if (!File.Exists(filePath))
                        {
                            continue;
                        }
                        findedInPath = true;
                        UrlDescription.shareFilesPath = filePath;
                        break;
                    }
                    isDownloaded = findedInPath;
                }
                else if (UrlDescriptions != null)
                {
                    foreach (var downloadInfo in UrlDescriptions)
                    {
                        bool findedInPath = false;
                        foreach (var path in paths)
                        {
                            var filePath = Path.Combine(path, downloadInfo.fileName);
                            if (!File.Exists(filePath))
                            {
                                continue;
                            }
                            findedInPath = true;
                            downloadInfo.shareFilesPath = filePath;
                            break;
                        }
                        isDownloaded = findedInPath;
                    }
                }
                if (isDownloaded)
                {
                    BackgroundColor = _DownloadedColor ?? BackgroundColor;
                    Text = DownloadedDescription ?? Text;
                }
                else
                {
                    BackgroundColor = _NotDownloadedColor ?? BackgroundColor;
                    Text = NotDownloadedDescription ?? Text;
                }

                OnPropertyChanged("BackgroundColor");
                OnPropertyChanged("Text");
            }
            catch (Exception ex)
            {
                DependencyService.Get<IToastService>().MakeText(TranslateExtension.GetText("NoHavePermissions"));
                FirebaseCrashlyticsServiceInstance.Log("checkIsDownloaded fail. Store permissions not granted");
                FirebaseCrashlyticsServiceInstance.RecordException(ex);
                FirebaseCrashlyticsServiceInstance.SendUnsentReports();
            }
        }

        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName == "UrlDescription")
            {
                checkIsDownloaded();
            }
            if (propertyName == "UrlDescriptions")
            {
                checkIsDownloaded();
            }
        }
        public DownloadButton() : base()
        {
            allDownloadButtons.Add(this);
        }
    }

    public class Profile
    {
        public int downloadCount { get; set; }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DownloadPage : ContentPage
    {
        public DownloadData data { get; }
        public List<ImageUrlDescription> imageList { get; } = new List<ImageUrlDescription>();

        public bool isHaveVideos { get => data.video_list.Count > 0; }
        public bool isHaveMusic { get => data.music_list.Count > 0; }
        public bool isHaveDescription { get => data.video_description != null && data.video_description.Length > 0; }
        public bool isHaveImages { get => imageList.Count > 0; }
        static string profilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "history");
        public DownloadPage(DownloadData data)
        {
            this.data = data;

            if (data.url_display_image_list.Count > 0)
            {
                imageList.Add(new ImageUrlDescription {
                    description = TranslateExtension.GetText("DownloadImagesWithotWatermark"),
                    downloadInfo = data.url_display_image_list });
            }

            if (data.url_owner_watermark_image_list.Count > 0)
            {
                imageList.Add(new ImageUrlDescription {
                    description = TranslateExtension.GetText("DownloadImagesWithWatermark"),
                    downloadInfo = data.url_owner_watermark_image_list });
            }

            if (data.url_user_watermark_image_list.Count > 0)
            {
                imageList.Add(new ImageUrlDescription {
                    description = TranslateExtension.GetText("DownloadImagesWithWatermarkAndUser"),
                    downloadInfo = data.url_user_watermark_image_list });
            }

            if (data.url_thumbnail_list.Count > 0)
            {
                imageList.Add(new ImageUrlDescription {
                    description = TranslateExtension.GetText("DownloadThubnailWithoutWatermark"),
                    downloadInfo = data.url_thumbnail_list });
            }

            DownloadButton.allDownloadButtons.Clear();
            InitializeComponent();

            BindingContext = this;

            Device.BeginInvokeOnMainThread(() =>
            {
                Profile profile = new Profile() { downloadCount = 0 };
                if (File.Exists(profilePath))
                {
                    var jsonString = File.ReadAllText(profilePath);
                    profile = JsonConvert.DeserializeObject<Profile>(jsonString);
                }
                ++profile.downloadCount;
                if (profile.downloadCount >= 5)
                {
#if DEBUG || RELEASE_RUSTORE
                    DependencyService.Get<IRuStore>().launchReviewFlow();
#endif
                    profile.downloadCount = 0;
                }
                {
                    var jsonString = JsonConvert.SerializeObject(profile);
                    File.WriteAllText(profilePath, jsonString);
                }
            });
        }

        private async void checkActivityFlags()
        {
            if (CustomActivityFlags.needDownloadAndShare)
            {
                CustomActivityFlags.needDownloadAndShare = false;
                if (isHaveVideos)
                {
                    var url = data.video_list.Where(x => !x.withWatermark).ToList().First();
                    await Download(url, null, ContentType.VIDEO);
                    Share(url, null);
                    //AddToHistory(url, null, ContentType.VIDEO);
                }
                else if (isHaveImages)
                {
                    var urls = data.url_display_image_list;
                    await Download(null, urls, ContentType.IMAGE);
                    Share(null, urls);
                    //AddToHistory(null, urls, ContentType.VIDEO);
                }
                DownloadButton.allDownloadButtons.ForEach(x => x.checkIsDownloaded());
            }
        }
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            checkActivityFlags();
        }

        private async Task DownloadAndSave(UrlDescription downloadInfo, ContentType contentType)
        {
            var client = new HttpClient();
            var uri = new Uri(downloadInfo.url);
            var downloadBytes = await client.GetByteArrayAsync(uri);
            var result = await DependencyService.Get<IFileService>().Save(downloadBytes, downloadInfo.fileName, contentType);
            downloadInfo.shareFilesPath = result;
        }

        private Task DownloadAndSaveThubnail()
        {
            return ContentDownloadManager.DownloadAndSaveThubnail(data);
        }

        private async Task DownloadAndSave(List<UrlDescription> downloadInfo, ContentType contentType)
        {
            foreach (var info in downloadInfo)
            {
                await DownloadAndSave(info, contentType);
            }
        }

        private async Task Download(UrlDescription UrlDescription, List<UrlDescription> UrlDescriptions, ContentType contentType)
        {
            await Navigation.PushModalAsync(new DownloadBanner());

            try
            {
                await DownloadAndSaveThubnail();
                if (UrlDescription != null)
                {
                    await DownloadAndSave(UrlDescription, contentType);
                }
                else if (UrlDescriptions != null)
                {
                    await DownloadAndSave(UrlDescriptions, contentType);
                }
                DependencyService.Get<IToastService>().MakeText(TranslateExtension.GetText("SavedToGallery"));
            }
            catch (Exception ex)
            {
                DependencyService.Get<IToastService>().MakeText(TranslateExtension.GetText("SaveFailTryAgain"));
                FirebaseCrashlyticsServiceInstance.Log("download fail. httpclient or filesystem error");
                FirebaseCrashlyticsServiceInstance.RecordException(ex);
                FirebaseCrashlyticsServiceInstance.SendUnsentReports();
            }

            await Navigation.PopModalAsync();
        }

        private void Share(UrlDescription UrlDescription, List<UrlDescription> UrlDescriptions)
        {
            if (UrlDescription != null)
            {
                var downloadInfo = UrlDescription;
                if (downloadInfo.shareFilesPath == null || !File.Exists(downloadInfo.shareFilesPath))
                {
                    DependencyService.Get<IToastService>().MakeText(TranslateExtension.GetText("FileNotFound"));
                    return;
                }
                List<string> filePaths = new List<string>();
                filePaths.Add(downloadInfo.shareFilesPath);

                string intentType = getIntentType(downloadInfo);
                string intentTitle = getIntentTitle(downloadInfo);
                DependencyService.Get<IFileService>().ShareMediaFile(intentTitle, filePaths.ToArray(), intentType);
            }
            else if (UrlDescriptions != null)
            {
                List<string> filePaths = new List<string>();
                string intentType = "";
                string intentTitle = "";
                foreach (var downloadInfo in UrlDescriptions)
                {
                    if (downloadInfo.shareFilesPath == null || !File.Exists(downloadInfo.shareFilesPath))
                    {
                        DependencyService.Get<IToastService>().MakeText(TranslateExtension.GetText("FilesNotFound"));
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
        }

        private void AddToHistory(UrlDescription urlDescription, List<UrlDescription> urlDescriptions, ContentType contentType)
        {
            var historyData = new HistoryData()
            {
                author = data.author,
                video_description = data.video_description,
                music_description = data.music_description,
                thubnail_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"{data.aweme_id}.jpg"),
                not_loaded = false
            };

            if (contentType == ContentType.VIDEO)
            {
                historyData.video_path = urlDescription.shareFilesPath;
            }
            else if (contentType == ContentType.IMAGE)
            {
                historyData.images_path = new SortedSet<string>(urlDescriptions.Select(x => x.shareFilesPath));
            }
            else if (contentType == ContentType.MUSIC)
            {
                historyData.music_path = urlDescription.shareFilesPath;
            }

            History.AddData(data.aweme_id, historyData);
            History.Save();
        }

        private async void DownloadClicked(object sender, EventArgs e)
        {
            FirebaseCrashlyticsServiceInstance.Log("DownloadClicked");
            await Device.InvokeOnMainThreadAsync(async () =>
            {
                var button = sender as DownloadButton;
                button.IsEnabled = false;
                try
                {
                    if (button.isDownloaded)
                    {
                        Share(button.UrlDescription, button.UrlDescriptions);
                        button.checkIsDownloaded();
                    }
                    else
                    {
                        await Download(button.UrlDescription, button.UrlDescriptions, button.contentType);
                        button.checkIsDownloaded();

                        AddToHistory(button.UrlDescription, button.UrlDescriptions, button.contentType);
                    }
                }
                catch (Exception ex)
                {
                    DependencyService.Get<IToastService>().MakeText(TranslateExtension.GetText("NoHavePermissions"));
                    FirebaseCrashlyticsServiceInstance.Log("download fail. store permissions not granted");
                    FirebaseCrashlyticsServiceInstance.RecordException(ex);
                    FirebaseCrashlyticsServiceInstance.SendUnsentReports();
                }
                button.IsEnabled = true;
            });
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
    }
}