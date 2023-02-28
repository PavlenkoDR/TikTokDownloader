using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TikTokDownloader
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DownloadBanner : ContentPage
    {
        private int _tryCount = 1;
        public int tryCount
        {
            get => _tryCount;
            set
            {
                _tryCount = value;
                BindingContext = null;
                BindingContext = this;
            }
        }
        public bool tryCountEnabled { get => tryCount > 1; }
        public string tryCountFormatted { get => $"Попытка {tryCount}/3"; }
        public DownloadBanner()
        {
            InitializeComponent();
            BindingContext = this;
        }
        protected override bool OnBackButtonPressed() => true;
    }
}