using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static Xamarin.Forms.BindableProperty;

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
        public List<UrlDescription> downloadInfo { get; set; }
    }

    public class DownloadButton : Button
    {
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
        public string DownloadedDescription { get; set; } = null;
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
        public async void checkIsDownloaded()
        {
            FirebaseCrashlyticsServiceInstance.Log("checkIsDownloaded");
            try
            {
                isDownloaded = true;
                var fileService = DependencyService.Get<IFileService>();
                var paths = new[] { await fileService.getDownloadsPath(), await fileService.getGalleryPath() };

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
                DependencyService.Get<IToastService>().MakeText("Нет прав доступа к хранилищу");
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
            
            InitializeComponent();
            
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
            FirebaseCrashlyticsServiceInstance.Log("DownloadClicked");
            var button = sender as DownloadButton;
            button.IsEnabled = false;
            try
            {
                if (button.isDownloaded)
                {
                    if (button.UrlDescription != null)
                    {
                        var downloadInfo = button.UrlDescription;
                        if (downloadInfo.shareFilesPath == null || !File.Exists(downloadInfo.shareFilesPath))
                        {
                            DependencyService.Get<IToastService>().MakeText("Файл не найден");
                            button.checkIsDownloaded();
                            button.IsEnabled = true;
                            return;
                        }
                        List<string> filePaths = new List<string>();
                        filePaths.Add(downloadInfo.shareFilesPath);

                        string intentType = getIntentType(downloadInfo);
                        string intentTitle = getIntentTitle(downloadInfo);
                        DependencyService.Get<IFileService>().ShareMediaFile(intentTitle, filePaths.ToArray(), intentType);
                    }
                    else if (button.UrlDescriptions != null)
                    {
                        List<string> filePaths = new List<string>();
                        string intentType = "";
                        string intentTitle = "";
                        foreach (var downloadInfo in button.UrlDescriptions)
                        {
                            if (downloadInfo.shareFilesPath == null || !File.Exists(downloadInfo.shareFilesPath))
                            {
                                DependencyService.Get<IToastService>().MakeText("Файлы не найдены");
                                button.checkIsDownloaded();
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
                }
                else
                {
                    await Navigation.PushModalAsync(new DownloadBanner());

                    if (button.UrlDescription != null)
                    {
                        await DownloadAndSave(button.UrlDescription);
                    }
                    else if (button.UrlDescriptions != null)
                    {
                        await DownloadAndSave(button.UrlDescriptions);
                    }
                    button.checkIsDownloaded();

                    await Navigation.PopModalAsync();
                    DependencyService.Get<IToastService>().MakeText(isSaveToDownloads ? "Сохранено в загрузки" : "Сохранено в галерею");
                }
            }
            catch (Exception ex)
            {
                DependencyService.Get<IToastService>().MakeText("Нет прав доступа к хранилищу");
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