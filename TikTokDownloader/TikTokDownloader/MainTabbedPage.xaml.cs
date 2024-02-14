using System.Threading.Tasks;
using System;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Xamarin.Forms.Xaml;

namespace TikTokDownloader
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainTabbedPage : Xamarin.Forms.TabbedPage
    {
        public ImageSource historyImageSource { get => ImageSource.FromResource("TikTokDownloader.Assets.history-icon.png"); }
        public ImageSource homeImageSource { get => ImageSource.FromResource("TikTokDownloader.Assets.home-icon.png"); }
        public MainTabbedPage()
        {
            BindingContext = this;
            InitializeComponent();
            On<Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);
        }

        public async Task<bool> CheckPermissions()
        {
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
    }
}