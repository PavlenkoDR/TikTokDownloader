using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Java.IO;
using Xamarin.Essentials;
using System.Threading.Tasks;
using System.IO;
using Android.Content;
using Android.Widget;

[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.Droid.FileService))]
[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.Droid.ClipBoardService))]
[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.Droid.ToastService))]
namespace TikTokDownloader.Droid
{
    [Activity(Label = "TikTokDownloader", Icon = "@mipmap/ic_launcher", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
    public class FileService : IFileService
    {
        public async Task Save(byte[] data, string name, bool isSaveToDownloads)
        {
            if (await Permissions.CheckStatusAsync<Permissions.StorageWrite>() == PermissionStatus.Granted ||
                await Permissions.RequestAsync<Permissions.StorageWrite>() == PermissionStatus.Granted)
            {
                if (await Permissions.CheckStatusAsync<Permissions.StorageRead>() == PermissionStatus.Granted ||
                    await Permissions.RequestAsync<Permissions.StorageRead>() == PermissionStatus.Granted)
                {
                    if (await Permissions.CheckStatusAsync<Permissions.Media>() == PermissionStatus.Granted ||
                        await Permissions.RequestAsync<Permissions.Media>() == PermissionStatus.Granted)
                    {
                        string path;
                        if (Environment.IsExternalStorageEmulated)
                        {
                            //(Android.OS.Environment.DirectoryDownloads)
                            if (isSaveToDownloads)
                            {
                                path = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads).AbsolutePath;
                            }
                            else
                            {
                                path = Path.Combine(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDcim).AbsolutePath, "Camera");
                            }
                        }
                        else
                        {
                            path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                        }
                        string filePath = Path.Combine(path, name);
                        FileOutputStream fileOutputStream = new FileOutputStream(new Java.IO.File(filePath));
                        fileOutputStream.Write(data);
                        fileOutputStream.Close();
                    }
                }
            }
        }
    }

    public class ClipBoardService : IClipBoardService
    {
        public event System.EventHandler PrimaryClipChanged;

        public void Set(string text)
        {
            var clipboardManager = (ClipboardManager)Application.Context.GetSystemService(Context.ClipboardService);
            ClipData clipData = ClipData.NewPlainText("label", text);
            clipboardManager.PrimaryClip = clipData;
        }

        public string Get()
        {
            var clipboardManager = (ClipboardManager)Application.Context.GetSystemService(Context.ClipboardService);
            ClipData clipData = clipboardManager.PrimaryClip;
            if (clipData != null && clipData.ItemCount > 0)
            {
                ClipData.Item clipDataItem = clipData.GetItemAt(0);
                return clipDataItem.Text;
            }
            return "";
        }

        public void AddPrimaryClipChanged(System.EventHandler handler)
        {
            PrimaryClipChanged += handler;
        }
    }

    public class ToastService : IToastService
    {
        public void MakeText(string text, ToastLength length)
        {
            Toast.MakeText(Application.Context, text, (Android.Widget.ToastLength)length).Show();
        }
    }
}