using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AdofaiModHelper.Core;

namespace AdofaiModHelper
{
    // 목록 한 줄에 바인딩되는 표시용 객체들. 언어가 바뀌면 다시 만든다
    public abstract class Observable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Raise(name);
            return true;
        }

        protected void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class ModRow : Observable
    {
        public ModRow(InstalledMod mod, CatalogMod catalog)
        {
            Mod = mod;
            Catalog = catalog;
            enabled = mod.Enabled;
            var author = string.IsNullOrEmpty(mod.Author) ? "" : mod.Author + " · ";
            Sub = author + (catalog == null ? App.T("S.NotInCatalog") : mod.Id);
            HasUpdate = catalog?.LatestVersion != null && Versions.Compare(catalog.LatestVersion, mod.Version) > 0;
            VersionText = HasUpdate ? $"{mod.Version} → {catalog.LatestVersion}" : mod.Version;
        }

        public InstalledMod Mod { get; }
        public CatalogMod Catalog { get; }
        public string Name => Mod.DisplayName;
        public string Sub { get; }
        public string VersionText { get; }
        public bool HasUpdate { get; }

        bool enabled;
        public bool Enabled { get => enabled; set => Set(ref enabled, value); }
    }

    public sealed class StoreRow
    {
        public StoreRow(CatalogMod mod, InstalledMod installed, bool resolving, bool english)
        {
            Mod = mod;
            Description = english ? mod.DescriptionEn : mod.DescriptionKo;
            if (mod.Requires.Length > 0) Description += "  ·  " + App.T("S.Requires", string.Join(", ", mod.Requires));
            var latest = mod.LatestVersion ?? (resolving ? App.T("S.Store.Loading") : "--");
            bool update = installed != null && mod.LatestVersion != null && Versions.Compare(mod.LatestVersion, installed.Version) > 0;
            VersionText = update ? $"{installed.Version} → {latest}" : latest;
            ActionText = installed == null ? App.T("S.Install") : update ? App.T("S.Update") : App.T("S.InstalledLabel");
            CanAct = installed == null || update;
        }

        public CatalogMod Mod { get; }
        public string Name => Mod.Name;
        public bool IsLibrary => Mod.Library;
        public string Description { get; }
        public string VersionText { get; }
        public string ActionText { get; }
        public bool CanAct { get; }
    }

    public sealed class PickRow : Observable
    {
        public PickRow(CatalogMod mod, bool english)
        {
            Mod = mod;
            Description = english ? mod.DescriptionEn : mod.DescriptionKo;
        }

        public CatalogMod Mod { get; }
        public string Name => Mod.Name;
        public string Description { get; }

        bool isChecked;
        public bool IsChecked { get => isChecked; set => Set(ref isChecked, value); }

        string autoNote;
        public string AutoNote { get => autoNote; set { if (Set(ref autoNote, value)) Raise(nameof(HasAutoNote)); } }
        public bool HasAutoNote => !string.IsNullOrEmpty(autoNote);
    }

    public sealed class Banner
    {
        public Banner(string message, params BannerAction[] actions)
        {
            Message = message;
            Actions = actions;
        }

        public string Message { get; }
        public BannerAction[] Actions { get; }
    }

    public sealed class BannerAction
    {
        public BannerAction(string label, Func<System.Threading.Tasks.Task> run)
        {
            Label = label;
            Run = run;
        }

        public string Label { get; }
        public Func<System.Threading.Tasks.Task> Run { get; }
    }
}
