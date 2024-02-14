using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace TikTokDownloader
{
    public class HistoryData
    {
        public string aweme_id { get; set; }
        public string author { get; set; }
        public string date { get; set; }
        public string thubnail_path { get; set; }
        public SortedSet<string> images_path { get; set; }
        public SortedSet<string> images_path_watermark { get; set; }
        public string video_path { get; set; }
        public string video_path_watermark { get; set; }
        public string music_path { get; set; }
        public string video_description { get; set; }
        public string music_description { get; set; }
        public bool not_loaded { get; set; }
    }
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        protected override event PropertyChangedEventHandler PropertyChanged;
        public override event NotifyCollectionChangedEventHandler CollectionChanged;
        public void Invalidate()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
        }
    }
    internal class History
    {
        static string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "history");
        public static ObservableCollection<HistoryData> historyList { get; private set; } = new ObservableCollection<HistoryData>();
        public static void AddData(string awemeId, HistoryData data, bool addToBack = false)
        {
            var current = historyList.Where(x => x.aweme_id == awemeId);
            if (current.Count() > 0)
            {
                var localData = current.First();
                data.thubnail_path = data.thubnail_path ?? localData.thubnail_path;
                data.music_path = data.music_path ?? localData.music_path;
                data.video_path = data.video_path ?? localData.video_path;
                data.video_path_watermark = data.video_path_watermark ?? localData.video_path_watermark;
                data.video_description = data.video_description ?? localData.video_description;
                data.music_description = data.music_description ?? localData.music_description;
                data.date = data.date ?? localData.date;
                data.author = data.author ?? localData.author;
                if (data.images_path != null && localData.images_path != null)
                {
                    localData.images_path.ForEach(x => data.images_path.Add(x));
                }
                else
                {
                    data.images_path = data.images_path ?? localData.images_path;
                }
                if (data.images_path_watermark != null && localData.images_path_watermark != null)
                {
                    localData.images_path_watermark.ForEach(x => data.images_path_watermark.Add(x));
                }
                else
                {
                    data.images_path_watermark = data.images_path_watermark ?? localData.images_path_watermark;
                }

                localData.not_loaded = data.not_loaded;
                historyList[historyList.IndexOf(localData)] = data;
                //historyList.Invalidate();
            }
            else
            {
                if (addToBack)
                {
                    historyList.Add(data);
                }
                else
                {
                    historyList.Insert(0, data);
                }
            }
        }
        public static void Save()
        {
            var jsonString = JsonConvert.SerializeObject(historyList);
            File.WriteAllText(path, jsonString);
        }
        public static void RemoveData(string aweme_id)
        {
            var current = historyList.Where(x => x.aweme_id == aweme_id);
            if (current.Count() > 0)
            {
                historyList.Remove(current.First());
            }
        }

        public static Task Load()
        {
            return Task.Run(() => {
                if (File.Exists(path))
                {
                    var jsonString = File.ReadAllText(path);
                    var obj = JsonConvert.DeserializeObject<List<HistoryData>>(jsonString);
                    if (obj != null)
                    {
                        historyList = new ObservableCollection<HistoryData>(obj);
                    }
                }
            });
        }
    }
}
