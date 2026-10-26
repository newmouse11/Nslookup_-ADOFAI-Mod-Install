using System;
using System.Windows;
using System.Windows.Threading;
using AdofaiModHelper.Core;

namespace AdofaiModHelper
{
    public partial class App : Application
    {
        const int LanguageSlot = 1;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 예상 못 한 오류로 앱이 통째로 꺼지면 초보자는 원인을 알 수 없다 → 기록하고 알린 뒤 계속 실행
            DispatcherUnhandledException += OnUnhandled;
            Log.Info($"시작: v{AppInfo.Version}, {Environment.OSVersion}");
            base.OnStartup(e);
        }

        void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error("처리되지 않은 오류", e.Exception);
            MessageBox.Show(T("S.D.Error.Title") + "\n\n" + e.Exception.Message + "\n\n" + T("S.D.Error.Hint"),
                "ADOFAI Mod Helper", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        public static void SetLanguage(string language)
        {
            var dict = new ResourceDictionary
            {
                Source = new Uri(language == "en" ? "Strings.en.xaml" : "Strings.ko.xaml", UriKind.Relative),
            };
            Current.Resources.MergedDictionaries[LanguageSlot] = dict;
        }

        public static string T(string key) => Current.TryFindResource(key) as string ?? key;

        public static string T(string key, params object[] args) => string.Format(T(key), args);
    }
}
