using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TikTokDownloader
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DownloadBanner : ContentPage
    {
        public DownloadBanner()
        {
            InitializeComponent();
        }
        protected override bool OnBackButtonPressed() => true;
    }
}