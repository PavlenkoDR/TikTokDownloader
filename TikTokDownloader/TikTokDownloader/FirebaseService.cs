using System.Collections.Generic;
using Xamarin.Forms;

namespace TikTokDownloader
{
    public interface IFirebaseService
    {
        void SendUnsentReports();
        void RecordException(System.Exception throwable);
        void Log(string message);

        void LogEvent(string eventId);
        void LogEvent(string eventId, string paramName, string value);
        void LogEvent(string eventId, IDictionary<string, string> parameters);
        void SetUserId(string userId);
    }

    public static class FirebaseCrashlyticsServiceInstance
    {
        public static IFirebaseService Instance => DependencyService.Get<IFirebaseService>();
        public static void SendUnsentReports() => Instance.SendUnsentReports();
        public static void RecordException(System.Exception throwable) => Instance.RecordException(throwable);
        public static void Log(string message) => Instance.Log(message);
    }
}
