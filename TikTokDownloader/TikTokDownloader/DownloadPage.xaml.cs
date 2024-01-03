using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Linq;
using static Xamarin.Forms.BindableProperty;

namespace TikTokDownloader
{
    public class UrlDescription
    {
        public string url { get; set; }
        private string _description;
        public string description { get => $"{TranslateExtension.GetText("Download")} {_description}"; set => _description = value; }
        public string descriptionShare { get => TranslateExtension.GetText("Share"); }
        public string fileName { get; set; }
        public string shareFilesPath { get; set; } = null;
        public bool withWatermark { get; set; } = false;
    }
    public class DownloadData
    {
        public string video_description { get; set; }
        public string dynamic_cover { get; set; }
        public string cover { get; set; }
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
        public string descriptionShare { get => TranslateExtension.GetText("Share"); }
        public List<UrlDescription> downloadInfo { get; set; }
    }

    public class DownloadButton : Button
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
        public async void checkIsDownloaded()
        {
            FirebaseCrashlyticsServiceInstance.Log("checkIsDownloaded");
            try
            {
                isDownloaded = true;
                var fileService = DependencyService.Get<IFileService>();
                var paths = new[] { await fileService.getDownloadsPath(), await fileService.getGalleryPath(), await fileService.getMusicPath() };

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

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DownloadPage : ContentPage
    {
        public DownloadData data { get; }
        public List<ImageUrlDescription> imageList { get; } = new List<ImageUrlDescription>();
        public static bool isSaveToDownloads { get; set; } = false;

        public int SelectedIndex
        {
            get
            {
                return isSaveToDownloads ? 1 : 0;
            }
            set
            {
                isSaveToDownloads = value == 1;
            }
        }
        public bool isHaveVideos { get => data.video_list.Count > 0; }
        public bool isHaveMusic { get => data.music_list.Count > 0; }
        public bool isHaveDescription { get => data.video_description != null && data.video_description.Length > 0; }
        public bool isHaveImages { get => imageList.Count > 0; }
        public DownloadPage(DownloadData data)
        {
            isSaveToDownloads = false;
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
                }
                else if (isHaveImages)
                {
                    var urls = data.url_display_image_list;
                    await Download(null, urls, ContentType.IMAGE);
                    Share(null, urls);
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
            var result = await DependencyService.Get<IFileService>().Save(downloadBytes, downloadInfo.fileName, false, contentType);
            downloadInfo.shareFilesPath = result;
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
                if (UrlDescription != null)
                {
                    await DownloadAndSave(UrlDescription, contentType);
                }
                else if (UrlDescriptions != null)
                {
                    await DownloadAndSave(UrlDescriptions, contentType);
                }
                DependencyService.Get<IToastService>().MakeText(
                    isSaveToDownloads ?
                    TranslateExtension.GetText("SavedToDownloads") :
                    TranslateExtension.GetText("SavedToGallery"));
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

        private async void DownloadClicked(object sender, EventArgs e)
        {
            FirebaseCrashlyticsServiceInstance.Log("DownloadClicked");
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