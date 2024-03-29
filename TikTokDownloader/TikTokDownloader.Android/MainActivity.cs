﻿using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Java.IO;
using Xamarin.Essentials;
using System.Threading.Tasks;
using Android.Content;
using Android.Widget;
using System.Collections.Generic;
using System.IO;
using Android;
using static Xamarin.Essentials.Permissions;
using System.Linq;
using static Android.Icu.Text.CaseMap;
using RU.Rustore.Sdk.Review;
using RU.Rustore.Sdk.Review.Model;
using Java.Util.Concurrent;
using Java.Lang;
using Firebase.Crashlytics.Internal.Common;

[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.Droid.FileService))]
[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.Droid.ClipBoardService))]
[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.Droid.ToastService))]
[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.Droid.FirebaseService))]
[assembly: Xamarin.Forms.Dependency(typeof(TikTokDownloader.Droid.RuStore))]
namespace TikTokDownloader.Droid
{
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "text/plain", Label = "@string/DownloadAndShare")]
    [Activity(Label = "TikTokDownloader", Icon = "@mipmap/ic_launcher", Theme = "@style/MainTheme", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (Intent.ActionSend.Equals(Intent.Action) &&
                Intent.Type != null &&
                "text/plain".Equals(Intent.Type))
            {
                CustomActivityFlags.needDownloadAndShare = true;
                CustomActivityFlags.url = Intent.Extras.GetString(Intent.ExtraText);
            }

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
            
            StrictMode.VmPolicy.Builder builder = new StrictMode.VmPolicy.Builder();
            StrictMode.SetVmPolicy(builder.Build());
            builder.DetectFileUriExposure();

            //string[] permissions = new string[] {
            //    Manifest.Permission.ManageExternalStorage,
            //    Manifest.Permission.ReadExternalStorage,
            //    Manifest.Permission.WriteExternalStorage
            //};
            //Platform.CurrentActivity.RequestPermissions(permissions, 1);

            //try
            //{
            //    var pckg = Application.Context.ApplicationInfo.PackageName;
            //    var uri = Android.Net.Uri.Parse("package:" + pckg);
            //    Intent intent = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission, uri);
            //    StartActivity(intent);
            //}
            //catch (Java.Lang.Exception)
            //{
            //    Intent intent = new Intent();
            //    intent.SetAction(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
            //    StartActivity(intent);
            //}
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }


    public class ReadWriteStoragePermission : Xamarin.Essentials.Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new List<(string androidPermission, bool isRuntime)>
        {
            (Android.Manifest.Permission.ReadExternalStorage, true),
            (Android.Manifest.Permission.WriteExternalStorage, true)
        }.ToArray();
    }
    public class ManageStoragePermission : Xamarin.Essentials.Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new List<(string androidPermission, bool isRuntime)>
        {
            (Android.Manifest.Permission.ManageExternalStorage, true)
        }.ToArray();
    }
    public class ManageMediaPermission : Xamarin.Essentials.Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new List<(string androidPermission, bool isRuntime)>
        {
            (Android.Manifest.Permission.ManageMedia, true)
        }.ToArray();
    }
    public class ReadMediaPermission : Xamarin.Essentials.Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new List<(string androidPermission, bool isRuntime)>
        {
            (Android.Manifest.Permission.ReadMediaAudio, true),
            (Android.Manifest.Permission.ReadMediaVideo, true),
            (Android.Manifest.Permission.ReadMediaImages, true)
        }.ToArray();
    }
    public class RuStore : IRuStore
    {
        static RuStoreReviewManager manager = null;
        static ReviewInfo info = null;
        public void launchReviewFlow()
        {
            requestReviewFlow((Java.Lang.Object obj) => {
                info = obj as ReviewInfo;
                launchReviewFlowInternal();
            });
        }
        private void requestReviewFlow(System.Action<Java.Lang.Object> action = null)
        {
            if (info != null)
            {
                return;
            }
            try
            {
                manager = manager ?? RuStoreReviewManagerFactory.Instance.Create(Application.Context);

                System.IntPtr classHandle = new System.IntPtr();
                System.IntPtr theClass = JNIEnv.FindClass("ru/rustore/sdk/review/RuStoreReviewManager", ref classHandle);
                System.IntPtr theMethod = JNIEnv.GetMethodID(theClass, "requestReviewFlow", "()Lru/rustore/sdk/core/tasks/Task;");

                System.IntPtr result = JNIEnv.CallObjectMethod(manager.Handle, theMethod);

                var resultCasted = Java.Lang.Object.GetObject<RU.Rustore.Sdk.Core.Tasks.Task>(result, JniHandleOwnership.TransferLocalRef);


                resultCasted.AddOnSuccessListener(new SuccessListener(action));
                resultCasted.AddOnFailureListener(new FailureListener());
            }
            catch (Java.Lang.Exception ex)
            {
                FirebaseCrashlyticsServiceInstance.Log("Init RuStoreReviewManager or Get/Call requestReviewFlow method failed");
                FirebaseCrashlyticsServiceInstance.RecordException(ex);
            }
        }
        private void launchReviewFlowInternal()
        {
            if (info == null)
            {
                return;
            }

            try
            {
                System.IntPtr classHandle = new System.IntPtr();
                System.IntPtr theClass = JNIEnv.FindClass("ru/rustore/sdk/review/RuStoreReviewManager", ref classHandle);
                System.IntPtr theMethod = JNIEnv.GetMethodID(theClass, "launchReviewFlow", "(Lru/rustore/sdk/review/model/ReviewInfo;)Lru/rustore/sdk/core/tasks/Task;");

                System.IntPtr result = JNIEnv.CallObjectMethod(manager.Handle, theMethod, new JValue(info));

                var resultCasted = Java.Lang.Object.GetObject<RU.Rustore.Sdk.Core.Tasks.Task>(result, JniHandleOwnership.TransferLocalRef);

                resultCasted.AddOnSuccessListener(new SuccessListener());
                resultCasted.AddOnFailureListener(new FailureListener());
            }
            catch (Java.Lang.Exception ex)
            {
                FirebaseCrashlyticsServiceInstance.Log("Get/Call launchReviewFlow method failed");
                FirebaseCrashlyticsServiceInstance.RecordException(ex);
            }
        }

        class SuccessListener : Java.Lang.Object, RU.Rustore.Sdk.Core.Tasks.IOnSuccessListener
        {
            System.Action<Java.Lang.Object> action;
            public SuccessListener(System.Action<Java.Lang.Object> action = null)
            {
                this.action = action;
            }
            public void OnSuccess(Java.Lang.Object result)
            {
                action?.Invoke(result);
            }
        }

        class FailureListener : Java.Lang.Object, RU.Rustore.Sdk.Core.Tasks.IOnFailureListener
        {
            System.Action<Throwable> action;
            public FailureListener(System.Action<Throwable> action = null)
            {
                this.action = action;
            }
            public void OnFailure(Throwable throwable)
            {
                action?.Invoke(throwable);
            }
        }
    }
    public class FileService : IFileService
    {
        public async Task<bool> CheckPermissions()
        {
            {
                var status = await Permissions.CheckStatusAsync<ReadWriteStoragePermission>();
                if (status != PermissionStatus.Granted)
                {
                    status = await RequestAsync<ReadWriteStoragePermission>();
                }
                if (status == PermissionStatus.Granted)
                {
                    return true;
                }
            }

            {
                var status = await Permissions.CheckStatusAsync<ReadMediaPermission>();
                if (status != PermissionStatus.Granted)
                {
                    status = await RequestAsync<ReadMediaPermission>();
                }
                if (status == PermissionStatus.Granted)
                {
                    return true;
                }
            }
            return false;
        }
        public string getMusicPath()
        {
            string path = null;
            if (Environment.IsExternalStorageEmulated)
            {
                path = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryMusic).AbsolutePath;
            }
            else
            {
                path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            }
            return path;
        }
        public string getGalleryPath()
        {
            string path = null;
            if (Environment.IsExternalStorageEmulated)
            {
                path = Path.Combine(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDcim).AbsolutePath, "Camera");
            }
            else
            {
                path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            }
            return path;
        }
        public async Task<string> Save(byte[] data, string name, ContentType contentType)
        {
            if (!await CheckPermissions())
            {
                return null;
            }
            string path;
            if (contentType == ContentType.MUSIC)
            {
                path = getMusicPath();
            }
            else
            {
                path = getGalleryPath();
            }
            {
                var pathDir = new Java.IO.File(path);
                if (!pathDir.Exists())
                {
                    pathDir.Mkdir();
                }
            }
            string filePath = Path.Combine(path, name);
            if (System.IO.File.Exists(filePath))
            {
                return filePath;
            }
            FileOutputStream fileOutputStream = new FileOutputStream(new Java.IO.File(filePath));
            fileOutputStream.Write(data);
            fileOutputStream.Close();

            var mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
            mediaScanIntent.SetData(Android.Net.Uri.FromFile(new Java.IO.File(filePath)));
            Application.Context.SendBroadcast(mediaScanIntent);
            return filePath;
        }
        public async void ShareMediaFile(string title, string[] filesPath, string intentType)
        {
            if (!await CheckPermissions())
            {
                return;
            }
            // File Share
            //var request = new ShareFileRequest
            //{
            //    Title = name,
            //    File = new ShareFile(filePath)
            //};
            //await Share.RequestAsync(request);

            // Action View
            //Java.IO.File file = new Java.IO.File(filePath);
            //Intent intent = new Intent();
            //intent.AddFlags(ActivityFlags.NewTask);
            //intent.SetAction(Intent.ActionView);
            //intent.SetDataAndType(Android.Net.Uri.FromFile(file), "video/mp4");
            //Xamarin.Forms.Forms.Context.StartActivity(Intent.CreateChooser(intent, "Поделиться видео"));

            if (filesPath.Length == 1)
            {
                // Media Share one file
                var file = new Java.IO.File(filesPath[0]);
                var uri = Android.Net.Uri.FromFile(file);

                var intent = new Intent(Intent.ActionSend);
                intent.SetType(intentType);
                intent.SetFlags(ActivityFlags.GrantReadUriPermission);
                intent.PutExtra(Intent.ExtraStream, uri);
                intent.PutExtra(Intent.ExtraText, title);

                var chooserIntent = Intent.CreateChooser(intent, title);
                chooserIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
                Platform.AppContext.StartActivity(chooserIntent);
            }
            else
            {
                // Media Share

                var intent = new Intent(Intent.ActionSendMultiple);
                if (intentType != null && intentType.Length > 0)
                {
                    intent.SetType(intentType);
                }
                intent.SetFlags(ActivityFlags.GrantReadUriPermission);
                var uris = new List<IParcelable>();
                foreach (var filePath in filesPath)
                {
                    var file = new Java.IO.File(filePath);
                    var uri = Android.Net.Uri.FromFile(file);
                    uris.Add(uri);
                }
                intent.PutParcelableArrayListExtra(Intent.ExtraStream, uris);
                intent.PutExtra(Intent.ExtraText, title);

                var chooserIntent = Intent.CreateChooser(intent, title);
                chooserIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
                Platform.AppContext.StartActivity(chooserIntent);
            }
        }
        public bool OpenAppSettings()
        {
            try
            {
                var intent = new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
                intent.AddFlags(ActivityFlags.NewTask);
                var uri = Android.Net.Uri.FromParts("package", AppInfo.PackageName, null);
                intent.SetData(uri);
                Application.Context.StartActivity(intent);
            }
            catch (Java.Lang.Exception ex)
            {
                FirebaseCrashlyticsServiceInstance.Log("OpenAppSettings exception");
                FirebaseCrashlyticsServiceInstance.RecordException(ex);
                return false;
            }
            return true;
        }
        public void PlayVideoFromLocalStorage(string filePath)
        {
            try
            {
                var file = new Java.IO.File(filePath);
                var uri = Android.Net.Uri.FromFile(file);
                Intent intent = new Intent();
                intent.SetAction(Intent.ActionView);
                intent.AddFlags(ActivityFlags.NewTask);
                intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                intent.SetDataAndType(uri, "video/*");
                Android.App.Application.Context.ApplicationContext.StartActivity(intent);
            }
            catch (Java.Lang.Exception)
            {

            }
        }
    }

    public class ClipBoardService : IClipBoardService
    {
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
            var clipboardManager = (ClipboardManager)Application.Context.GetSystemService(Context.ClipboardService);
            clipboardManager.PrimaryClipChanged += handler;
        }
    }

    public class ToastService : IToastService
    {
        public void MakeText(string text, ToastLength length)
        {
            Toast.MakeText(Application.Context, text, (Android.Widget.ToastLength)length).Show();
        }
    }

    public class FirebaseService : IFirebaseService
    {
        public void SendUnsentReports()
        {
            Firebase.Crashlytics.FirebaseCrashlytics.Instance.SendUnsentReports();
        }
        public void RecordException(System.Exception exception)
        {
            Firebase.Crashlytics.FirebaseCrashlytics.Instance.RecordException(Java.Lang.Throwable.FromException(exception));
        }

        public void Log(string message)
        {
            Firebase.Crashlytics.FirebaseCrashlytics.Instance.Log(message);
        }

        public void LogEvent(string eventId)
        {
            LogEvent(eventId, null);
        }
        public void LogEvent(string eventId, string paramName, string value)
        {
            LogEvent(eventId, new Dictionary<string, string>
            {
                {paramName, value}
            });
        }

        public void SetUserId(string userId)
        {
            var fireBaseAnalytics = Firebase.Analytics.FirebaseAnalytics.GetInstance(Application.Context);
            fireBaseAnalytics.SetUserId(userId);
        }

        public void LogEvent(string eventId, IDictionary<string, string> parameters)
        {
            var fireBaseAnalytics = Firebase.Analytics.FirebaseAnalytics.GetInstance(Application.Context);

            if (parameters == null)
            {
                fireBaseAnalytics.LogEvent(eventId, null);
                return;
            }

            var bundle = new Bundle();

            foreach (var item in parameters)
            {
                bundle.PutString(item.Key, item.Value);
            }

            fireBaseAnalytics.LogEvent(eventId, bundle);
        }
    }
}