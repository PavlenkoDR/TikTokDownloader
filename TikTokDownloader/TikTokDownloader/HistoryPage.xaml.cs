using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TikTokDownloader
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HistoryPage : ContentPage
    {
        public ObservableCollection<HistoryData> historyList { get => History.historyList; }
        public HistoryPage()
        {
            InitializeComponent();

            Task.Run(async () => {
                await History.Load();
                Device.BeginInvokeOnMainThread(() => {
                    Refresh();
                    BindingContext = this;

                    App.OnGotFocus += () =>
                    {
                        Refresh();
                    };
                });
            });
        }

        bool Match(string input, string pattern, out Match match)
        {
            match = Regex.Match(input, pattern);
            return match.Success;
        }

        struct FindedAwemeData
        {
            public string aweme_id;
            public string file;
            public ContentType content_type;
            public bool with_watermark;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            Refresh();
        }

        private async void Refresh()
        {
            var isGranted = await (Application.Current.MainPage as MainTabbedPage)?.CheckPermissions();
            if (!isGranted)
            {
                return;
            }
            var fileService = DependencyService.Get<IFileService>();
            var paths = new[] { await fileService.getGalleryPath(), await fileService.getMusicPath() };
            SortedDictionary<DateTime, FindedAwemeData> findedAwemes = new SortedDictionary<DateTime, FindedAwemeData>();
            foreach (var path in paths)
            {
                var files = Directory.GetFiles(path);
                if (files == null)
                {
                    return;
                }
                foreach (var file in files)
                {
                    /*
                        fileName = $"watermark_{aweme_id}.mp4"
                        fileName = $"no_watermark_{gear_name}_{aweme_id}.mp4",
                        fileName = $"{imageObjIdx}_no_watermark_{aweme_id}.jpeg",
                        fileName = $"{imageObjIdx}_watermark_{aweme_id}.jpeg",
                        fileName = $"{imageObjIdx}_user_watermark_{aweme_id}.jpeg",
                        fileName = $"{imageObjIdx}_thumbnail_no_watermark_{aweme_id}.jpeg",
                        fileName = $"music_{aweme_id}.mp3"
                        fileName = $"watermark_hq_{aweme_id}.mp4",
                        fileName = $"no_watermark_hq_{aweme_id}.mp4",
                        fileName = $"{idx++}_no_watermark_{aweme_id}.jpeg",
                        fileName = $"{idx++}_watermark_{aweme_id}.jpeg",
                     */
                    string aweme_id = null;
                    Match match;
                    ContentType contentType = ContentType.VIDEO;
                    if (Match(file, "watermark_.*_(.*?)\\.(mp4|jpeg)", out match) && match.Groups.Count > 2)
                    {
                        aweme_id = match.Groups[1].Value;
                        if (match.Groups[2].Value == "jpeg")
                        {
                            contentType = ContentType.IMAGE;
                        }
                    }
                    else if (Match(file, "watermark_(.*?)\\.(mp4|jpeg)", out match) && match.Groups.Count > 2)
                    {
                        aweme_id = match.Groups[1].Value;
                        if (match.Groups[2].Value == "jpeg")
                        {
                            contentType = ContentType.IMAGE;
                        }
                    }
                    else if (Match(file, "music_(.*?)\\.mp3$", out match) && match.Groups.Count > 1)
                    {
                        aweme_id = match.Groups[1].Value;
                        contentType = ContentType.MUSIC;
                    }
                    if (aweme_id != null)
                    {
                        var time = File.GetCreationTime(file);
                        findedAwemes.Add(time, new FindedAwemeData()
                        {
                            aweme_id = aweme_id,
                            content_type = contentType,
                            file = file,
                            with_watermark = !file.Contains("no_watermark_") && !file.Contains("music_")
                        });
                    }
                }
            }
            var oldHistory = new List<HistoryData>(History.historyList);
            History.historyList.Clear();
            await Task.Run(async () =>
            {
                foreach (var aweme in findedAwemes.ToList().Reverse<KeyValuePair<DateTime, FindedAwemeData>>())
                {
                    var aweme_id = aweme.Value.aweme_id;
                    var historyData = new HistoryData();
                    historyData.aweme_id = aweme_id;
                    historyData.not_loaded = true;
                    historyData.date = aweme.Key.ToLongDateString();
                    var oldHistoryDataFinded = oldHistory.Where(x => x.aweme_id == aweme_id);
                    if (oldHistoryDataFinded.Count() > 0)
                    {
                        var oldHistoryData = oldHistoryDataFinded.First();
                        historyData.video_path = oldHistoryData.video_path;
                        historyData.video_path_watermark = oldHistoryData.video_path_watermark;
                        historyData.images_path = oldHistoryData.images_path != null ? new SortedSet<string>(oldHistoryData.images_path) : null;
                        historyData.images_path_watermark = oldHistoryData.images_path_watermark != null ? new SortedSet<string>(oldHistoryData.images_path_watermark) : null;
                        historyData.music_path = oldHistoryData.music_path;
                        historyData.author = oldHistoryData.author;
                        historyData.thubnail_path = oldHistoryData.thubnail_path;
                        historyData.video_description = oldHistoryData.video_description;
                        historyData.music_description = oldHistoryData.music_description;
                        historyData.not_loaded = false;
                    }

                    await Device.InvokeOnMainThreadAsync(() => {
                        History.AddData(aweme_id, historyData, true);
                    });
                }
                List<Task> tasks = new List<Task>();
                int completedTasks = 0;
                int runnedTasks = 0;
                foreach (var aweme in findedAwemes.Reverse())
                {
                    if (runnedTasks - completedTasks > 5)
                    {
                        await tasks[completedTasks];
                        completedTasks++;
                    }
                    tasks.Add(Task.Run(async () =>
                    {
                        var aweme_id = aweme.Value.aweme_id;
                        var oldHistoryDataFinded = oldHistory.Where(x => x.aweme_id == aweme.Value.aweme_id);
                        if (oldHistoryDataFinded.Count() > 0)
                        {
                            var oldHistoryData = oldHistoryDataFinded.First();
                            if (!oldHistoryData.not_loaded)
                            {
                                return;
                            }
                        }
                        for (int i = 0; i < 10; ++i)
                        {
                            var content = ContentDownloadManager.getContentFromTikTokAwemeId(aweme_id);
                            if (content != null)
                            {
                                var contentType = aweme.Value.content_type;
                                var file = aweme.Value.file;
                                var withWatermark = aweme.Value.with_watermark;

                                await ContentDownloadManager.DownloadAndSaveThubnail(content);
                                var thubnail_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"{content.aweme_id}.jpg");

                                var historyData = new HistoryData();
                                historyData.aweme_id = aweme_id;
                                historyData.thubnail_path = thubnail_path;
                                historyData.not_loaded = false;
                                historyData.author = content.author;

                                switch (contentType)
                                {
                                    case ContentType.VIDEO:
                                        if (withWatermark)
                                        {
                                            historyData.video_path_watermark = file;
                                        }
                                        else
                                        {
                                            historyData.video_path = file;
                                        }
                                        historyData.video_description = content.video_description;
                                        break;

                                    case ContentType.IMAGE:

                                        if (withWatermark)
                                        {
                                            historyData.images_path_watermark = new SortedSet<string> { file };
                                        }
                                        else
                                        {
                                            historyData.images_path = new SortedSet<string> { file };
                                        }
                                        historyData.video_description = content.video_description;
                                        break;

                                    case ContentType.MUSIC:
                                        historyData.music_path = file;
                                        historyData.music_description = content.music_description;
                                        break;
                                }

                                await Device.InvokeOnMainThreadAsync(() => {
                                    History.AddData(aweme_id, historyData);
                                });
                                return;
                            }
                        }
                    }));
                    runnedTasks++;
                }
                History.Save();
            });
        }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            var historyData =  (sender as Frame).BindingContext as HistoryData;
            Navigation.PushModalAsync(new IntercatContentPage(historyData, true));
        }
    }
}