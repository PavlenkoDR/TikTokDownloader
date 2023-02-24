namespace TikTokDownloader
{
    public interface IFirebaseService
    {
        void SendUnsentReports();
        void RecordException(System.Exception throwable);
        void Log(string message);
    }
}
