using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TikTokDownloader
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class IntercatContentPage : ContentPage
    {
        private HistoryData historyData;
        public bool isOpenEnabled { get; private set; }
        public bool isShareEnabled { get; private set; }
        public bool isVideoWithWatermarkEnabled { get => historyData.video_path_watermark != null; }
        public bool isVideoWithoutWatermarkEnabled { get => historyData.video_path != null; }
        public bool isImagesWithWatermarkEnabled { get => historyData.images_path_watermark != null; }
        public bool isImagesWithoutWatermarkEnabled { get => historyData.images_path != null; }
        public bool isMusicEnabled { get => historyData.music_path != null; }
        public IntercatContentPage(HistoryData historyData, bool isOpen)
        {
            this.historyData = historyData;
            isOpenEnabled = isOpen;
            isShareEnabled = !isOpen;
            InitializeComponent();
            BindingContext = this;
        }
        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
        }

        private void VideoWithWatermark_Clicked(object sender, EventArgs e)
        {
            Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(historyData.video_path_watermark) });
        }

        private void VideoWithoutWatermark_Clicked(object sender, EventArgs e)
        {
            Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(historyData.video_path) });
        }

        private void ImagesWithWatermark_Clicked(object sender, EventArgs e)
        {
            Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(historyData.images_path_watermark.First()) });
        }

        private void ImagesWithoutWatermark_Clicked(object sender, EventArgs e)
        {
            Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(historyData.images_path.First()) });
        }

        private void Music_Clicked(object sender, EventArgs e)
        {
            Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(historyData.music_path) });
        }
    }
}