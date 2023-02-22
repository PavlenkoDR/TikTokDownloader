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
    }
}