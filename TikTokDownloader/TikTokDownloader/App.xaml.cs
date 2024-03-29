﻿using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TikTokDownloader
{
    public partial class App : Application
    {
        public delegate void FocusHandler();
        public delegate void EmptyHandler();
        public static event EmptyHandler OnApplicationStarted;
        public static event FocusHandler OnGotFocus;
        public static event FocusHandler OnLostFocus;
        public App()
        {
            Settings.Load();

            InitializeComponent();

            var navigationPage = new NavigationPage(new MainPage());
            navigationPage.BarBackgroundColor = Color.FromHex("#6983fa");

#if RELEASE_RUSTORE
            //RuStoreReviewManager.Instance.Init();
#endif
            MainPage = navigationPage;
        }

        protected override void OnStart()
        {
            OnApplicationStarted?.Invoke();
        }

        protected override void OnSleep()
        {
            OnLostFocus?.Invoke();
        }

        protected override void OnResume()
        {
            OnGotFocus?.Invoke();
        }
    }
}
