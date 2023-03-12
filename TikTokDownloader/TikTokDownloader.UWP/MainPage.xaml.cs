using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.UWP.FileService))]
[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.UWP.ClipBoardService))]
[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.UWP.ToastService))]
[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.UWP.FirebaseService))]
namespace TikTokDownloader.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();

            LoadApplication(new TikTokDownloader.App());
        }
    }
    public class FileService : IFileService
    {
        public Task<bool> CheckPermissions()
        {
            return Task.Run(()=> { return true; });
        }

        public Task<string> getDownloadsPath()
        {
            return Task.Run(() => { return ""; });
        }

        public Task<string> getGalleryPath()
        {
            return Task.Run(() => { return ""; });
        }

        public bool OpenAppSettings()
        {
            return false;
        }

        public async Task<string> Save(byte[] data, string name, bool isSaveToDownloads)
        {
            FolderPicker picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.Downloads };
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                var file = await folder.CreateFileAsync(name);
                var stream = await file.OpenStreamForWriteAsync();
                await stream.WriteAsync(data, 0, data.Length);
                return folder.Path;
            }
            return "";
        }

        public void ShareMediaFile(string title, string[] filesPath, string intentType)
        {
        }
    }

    public class ClipBoardService : IClipBoardService
    {
        public void AddPrimaryClipChanged(EventHandler handler)
        {
        }

        public string Get()
        {
            return "";
        }

        public void Set(string text)
        {
        }
    }

    public class ToastService : IToastService
    {
        public void MakeText(string text, ToastLength length = ToastLength.Short)
        {
        }
    }

    public class FirebaseService : IFirebaseService
    {
        public void Log(string message)
        {
        }

        public void LogEvent(string eventId)
        {
        }

        public void LogEvent(string eventId, string paramName, string value)
        {
        }

        public void LogEvent(string eventId, IDictionary<string, string> parameters)
        {
        }

        public void RecordException(Exception throwable)
        {
        }

        public void SendUnsentReports()
        {
        }

        public void SetUserId(string userId)
        {
        }
    }

}

