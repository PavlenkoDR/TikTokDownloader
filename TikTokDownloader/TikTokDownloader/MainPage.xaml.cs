using System;
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
using Xamarin.Essentials;
using System.Globalization;
using static TikTokDownloader.ContentDownloadManager;

namespace TikTokDownloader
{
    public class HyperlinkSpan : Span
    {
        public static readonly BindableProperty UrlProperty =
            BindableProperty.Create(nameof(Url), typeof(string), typeof(HyperlinkSpan), null);

        public string Url
        {
            get { return (string)GetValue(UrlProperty); }
            set { SetValue(UrlProperty, value); }
        }

        public HyperlinkSpan()
        {
            TextDecorations = TextDecorations.Underline;
            TextColor = Color.Blue;
            GestureRecognizers.Add(new TapGestureRecognizer
            {
                // Launcher.OpenAsync is provided by Xamarin.Essentials.
                Command = new Command(async () => await Launcher.OpenAsync(Url))
            });
        }
    }

    public partial class MainPage : ContentPage
    {
        public string videoURL { get; set; } = "";
        public string debugText { get; set; } = "";
        public ImageSource linkImageSource { get => ImageSource.FromResource("TikTokDownloader.Assets.link.jpg"); }
        public ImageSource shareImageSource { get => ImageSource.FromResource("TikTokDownloader.Assets.share.jpg"); }
        private string language;
        public string languageProperty
        {
            get { return language; }
            set
            {
                language = value;
                OnPropertyChanged(nameof(languageProperty));
            }
        }

        private string lastMatchedUrl = "";
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            languageProperty = TranslateExtension.GetText("LanguageFlag");

            App.OnGotFocus += () => {
                Device.BeginInvokeOnMainThread(() => {
                    if (!oneShotPaste)
                    {
                        return;
                    }
                    var text = DependencyService.Get<IClipBoardService>().Get();
                    if (CustomActivityFlags.needDownload || CustomActivityFlags.needDownloadAndShare)
                    {
                        text = CustomActivityFlags.url;
                        DependencyService.Get<IClipBoardService>().Set(text);
                    }
                    foreach (var match in MatchTikTokUrl(text))
                    {
                        if (match.url != text)
                        {
                            FirebaseCrashlyticsServiceInstance.Log("OnGotFocus url matched");
                            urlEditor.Text = match.url;
                            videoURL = match.url;
                            lastMatchedUrl = match.url;
                            DependencyService.Get<IToastService>().MakeText(TranslateExtension.GetText("NewUrlFinded"));

                            Navigation.PopToRootAsync();
                        }
                    }
                });
            };
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
                if (CustomActivityFlags.needDownload || CustomActivityFlags.needDownloadAndShare)
                {
                    text = CustomActivityFlags.url;
                    DependencyService.Get<IClipBoardService>().Set(text);
                }
                foreach (var match in MatchTikTokUrl(text))
                {
                    FirebaseCrashlyticsServiceInstance.Log("oneShotPaste url matched");
                    urlEditor.Text = match.url;
                    videoURL = match.url;
                    lastMatchedUrl = match.url;
                    DependencyService.Get<IToastService>().MakeText(TranslateExtension.GetText("NewUrlFinded"));

                    if (CustomActivityFlags.needDownload || CustomActivityFlags.needDownloadAndShare)
                    {
                        CustomActivityFlags.needDownload = false;
                        Button_ClickedAsync(null, null);
                    }
                }
            }
        }
        public async Task<bool> CheckPermissions()
        {
            //return await (Application.Current.MainPage as MainTabbedPage)?.CheckPermissions();
            var fileService = DependencyService.Get<IFileService>();
            bool permissionsGranted = await fileService.CheckPermissions();
            if (!permissionsGranted)
            {
                FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync permissions not granted");
                bool answer = await DisplayAlert(
                    TranslateExtension.GetText("NoHaveFilePermissionsTitle"),
                    TranslateExtension.GetText("NoHaveFilePermissionsInfo"),
                    TranslateExtension.GetText("Yes"),
                    TranslateExtension.GetText("No"));
                if (answer)
                {
                    FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync open settings");
                    var isOpened = fileService.OpenAppSettings();
                    if (!isOpened)
                    {
                        FirebaseCrashlyticsServiceInstance.Log("setting not opened");
                        FirebaseCrashlyticsServiceInstance.RecordException(new Exception("can not open settings"));
                        FirebaseCrashlyticsServiceInstance.SendUnsentReports();
                        await DisplayAlert(
                            TranslateExtension.GetText("CanNotOpenSettingsTitle"),
                            TranslateExtension.GetText("CanNotOpenSettingsInfo"),
                            TranslateExtension.GetText("Good"));
                    }
                }
                else
                {
                    FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync grant permissions ignored");
                    await DisplayAlert(
                        TranslateExtension.GetText("PermissionsDescardedTitle"),
                        TranslateExtension.GetText("PermissionsDescardedInfo"),
                        TranslateExtension.GetText("Understend"));
                }
                return false;
            }
            return permissionsGranted;
        }

        private void Button_ClickedAsync(object sender, EventArgs e)
        {
            Task.Run(() => {
                runPreload();
            });
        }

        private async void runPreload()
        {
            FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync");
            var isPermissionsGranted = await Device.InvokeOnMainThreadAsync(async () => {
                downloadButton.IsEnabled = false;

                var isGranted = await CheckPermissions();
                if (!isGranted)
                {
                    downloadButton.IsEnabled = true;
                }

                return isGranted;
            });
            if (!isPermissionsGranted)
            {
                return;
            }

            var matches = MatchTikTokUrl(videoURL);
            if (matches.Count == 0)
            {
                FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync url not matched");
                DependencyService.Get<IToastService>().MakeText(TranslateExtension.GetText("PastLinkToField"));
                downloadButton.IsEnabled = true;
                return;
            }
            var banner = new DownloadBanner();
            Device.BeginInvokeOnMainThread(async () => {
                await Navigation.PushModalAsync(banner);
            });

            DownloadData result = null;
            for (int i = 1; i < 4; i++)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    banner.tryCount = i;
                });
                bool downloaded = false;
                foreach (var match in matches)
                {
                    result = await ContentDownloadManager.RunDownload(match.url);
                    if (result != null)
                    {
                        downloaded = true;
                        break;
                    }
                }
                if (downloaded)
                {
                    break;
                }
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                await Navigation.PopModalAsync();
                if (result != null)
                {
                    FirebaseCrashlyticsServiceInstance.Log("Button_ClickedAsync success");
                    await Navigation.PushAsync(new DownloadPage(result));
                }
                else
                {
                    await DisplayAlert(
                        TranslateExtension.GetText("GetDataFailedTitle"),
                        TranslateExtension.GetText("GetDataFailedInfo"), "OK");
                    DependencyService.Get<IToastService>().MakeText(TranslateExtension.GetText("TryAgain"));
                }
                downloadButton.IsEnabled = true;
            });
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
            foreach (var match in MatchTikTokUrl(text))
            {
                lastMatchedUrl = match.url;
            }
        }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            if (Settings.GetLanguage() == "ru-RU")
            {
                Settings.SetLanguage("en-US");
            }
            else
            {
                Settings.SetLanguage("ru-RU");
            }
            languageProperty = TranslateExtension.GetText("LanguageFlag");
            TranslateExtension.Instance.Invalidate();
        }
    }
}
