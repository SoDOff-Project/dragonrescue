﻿using dragonrescue;
using dragonrescue.Util;
using dragonrescuegui.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text; 
using System.Threading.Tasks;
using dragonrescue.Api;
using dragonrescue.Util;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Platform.Storage;
using static dragonrescue.Api.LoginApi;
using System.Diagnostics;

namespace dragonrescuegui.ViewModels {
    public class ExportImportViewModel : ViewModelBase {

        public event EventHandler ScrollRequested;
        public ReactiveCommand<Unit, Unit> OkCommand { get; }
        private Mode mode;


        #region MVVM private vars
        private string _userApiUrl;
        private string _contentApiUrl;
        private string _username;
        private string _password;
        private string _vikingName;
        private string _path;
        private string _log;
        private string _modeText;
        private string _oldVikingName;
        private string _dragonId;
        private string _warningText;
        private string _selectText;
        private double _progressValue;

        private bool _isImportMode;
        private bool _isRemoveDragonMode;
        private bool _isEnabled;
        private bool _isDragonsSelected;
        private bool _isInventorySelected;
        private bool _isHideoutSelected;
        private bool _isAvatarSelected;
        private bool _isFarmsSelected;
        #endregion

        #region MVVM Bindings
        public bool IsDragonsSelected {
            get => _isDragonsSelected;
            set => UpdateUI(ref _isDragonsSelected, value);
        }

        public bool IsInventorySelected {
            get => _isInventorySelected;
            set => UpdateUI(ref _isInventorySelected, value);
        }

        public bool IsHideoutSelected {
            get => _isHideoutSelected;
            set => UpdateUI(ref _isHideoutSelected, value);
        }

        public bool IsAvatarSelected {
            get => _isAvatarSelected;
            set {
                // FIXME: For some reason RaiseAndSetIfChanged inside UpdateUI doesn't work correctly when used here
                this.RaiseAndSetIfChanged(ref _isAvatarSelected, value);
                UpdateUI(ref _isAvatarSelected, value);
            }
        }

        public bool IsFarmsSelected {
            get => _isFarmsSelected;
            set => UpdateUI(ref _isFarmsSelected, value);
        }

        public bool IsImportMode {
            get => _isImportMode;
            set => this.RaiseAndSetIfChanged(ref _isImportMode, value);
        }

        public bool IsRemoveDragonMode {
            get => _isRemoveDragonMode;
            set => this.RaiseAndSetIfChanged(ref _isRemoveDragonMode, value);
        }

        public string UserApiUrl {
            get => _userApiUrl;
            set => UpdateUI(ref _userApiUrl, value);
        }

        public string ContentApiUrl {
            get => _contentApiUrl;
            set => UpdateUI(ref _contentApiUrl, value);
        }

        public string Username {
            get => _username;
            set => UpdateUI(ref _username, value);
        }

        public string Password {
            get => _password;
            set => UpdateUI(ref _password, value);
        }

        public string VikingName {
            get => _vikingName;
            set {
                if (string.IsNullOrWhiteSpace(OldVikingName) || OldVikingName == _vikingName)
                    OldVikingName = value;
                UpdateUI(ref _vikingName, value);
            }
        }

        public string ModeText {
            get => _modeText;
            set => this.RaiseAndSetIfChanged(ref _modeText, value);
        }

        public string OldVikingName {
            get => _oldVikingName;
            set => this.RaiseAndSetIfChanged(ref _oldVikingName, value);
        }

        public string DragonId {
            get => _dragonId;
            set => UpdateUI(ref _dragonId, value);
        }

        public string WarningText {
            get => _warningText;
            set => this.RaiseAndSetIfChanged(ref _warningText, value);
        }

        public string SelectText {
            get => _selectText;
            set => this.RaiseAndSetIfChanged(ref _selectText, value);
        }

        public string Log {
            get => _log;
            set {
                this.RaiseAndSetIfChanged(ref _log, value);
                OnScrollRequested();
            }
        }

        public string Path {
            get {
                if (_path is null)
                    return null;
                return Uri.UnescapeDataString(_path);
            }
            set {
                // FIXME: For some reason RaiseAndSetIfChanged inside UpdateUI doesn't work correctly when used here
                this.RaiseAndSetIfChanged(ref _path, value);
                UpdateUI(ref _path, value);
            }
        }

        public bool IsEnabled {
            get => _isEnabled;
            set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
        }

        public double ProgressValue {
            get => _progressValue;
            set => this.RaiseAndSetIfChanged(ref _progressValue, value);
        }
        #endregion

        public ExportImportViewModel(MainWindowViewModel mainWindow, Mode mode) {
            mainWindow.Width = 400;
            this.mode = mode;
            switch (mode) {
                case Mode.Export:
                    ModeText = "Export";
                    mainWindow.Height = 605;
                    break;
                case Mode.Import:
                    ModeText = "Import";
                    mainWindow.Height = 735;
                    break;
                case Mode.RemoveDragon:
                    ModeText = "Remove Dragon";
                    mainWindow.Height = 605;
                    break;
            }
            SelectText = mode == Mode.Export ? "Select folder:" : "Select XML:";
            IsImportMode = mode == Mode.Import;
            IsRemoveDragonMode = mode == Mode.RemoveDragon;
        }

        public async Task ExecuteButtonCommand() {
            Log = "";
            var writer = new TextWriterExt();
            Console.SetOut(writer);
            writer.TextWritten += (sender, e) => {
                Log += e;
            };
            LoginApi.Data data = new LoginApi.Data {
                viking = VikingName, username = Username, password = Password
            };
            Config.URL_USER_API = _userApiUrl;
            Config.URL_CONT_API = _contentApiUrl;
            IProgress<double> progress = new Progress<double>(value => ProgressValue = value);
            progress.Report(0);
            try {
                Config.ProgressInfo = progress.Report;
                if (mode == Mode.Export)
                    await Exporters.Export(data, Path);
                else if (mode == Mode.Import)
                    await Import(data);
                else if (mode == Mode.RemoveDragon)
                    await Tools.RemoveDragon(data, DragonId);
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("Finished");
        }

        private async Task Import(LoginApi.Data data) {
            if (IsAvatarSelected)
                await Importers.ImportAvatar(data, Path, OldVikingName, true);
            else if (IsDragonsSelected)
                await Importers.ImportDragons(data, Path);
            else if (IsInventorySelected)
                await Importers.ImportInventory(data, Path);
            else if (IsHideoutSelected)
                await Importers.ImportHideout(data, Path);
            else if (IsFarmsSelected)
                await Importers.ImportFarm(data, Path);
        }

        public async Task SelectFolderClick(Window window) {
            if (mode == Mode.Export) {
                var selected = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Pick folder" });
                if (selected.Count > 0 && !string.IsNullOrEmpty(selected[0].Path.ToString()))
                    Path = selected[0].Path.AbsolutePath;
            } else {
                FilePickerFileType Xml = new("XML File") {
                    Patterns = new[] { "*.xml" },
                };
                var selected = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, Title = "Pick XML file", FileTypeFilter = new[] { Xml } });
                if (selected.Count > 0 && !string.IsNullOrEmpty(selected[0].Path.ToString()))
                    Path = selected[0].Path.AbsolutePath;
            }
        }

        private TRet UpdateUI<TRet>(ref TRet backingField, TRet val) {
            var ret = this.RaiseAndSetIfChanged(ref backingField, val);
            bool baseInfo = !string.IsNullOrWhiteSpace(UserApiUrl) &&
                    !string.IsNullOrWhiteSpace(ContentApiUrl) &&
                    !string.IsNullOrWhiteSpace(Username) &&
                    !string.IsNullOrWhiteSpace(Password) &&
                    !string.IsNullOrWhiteSpace(VikingName);
            switch (mode) {
                case Mode.Export:
                    IsEnabled = baseInfo && !string.IsNullOrWhiteSpace(Path);
                    break;
                case Mode.Import:
                    IsEnabled = baseInfo && !string.IsNullOrWhiteSpace(Path) &&
                        (IsAvatarSelected || IsDragonsSelected
                        || IsFarmsSelected || IsHideoutSelected || IsInventorySelected);
                    break;
                case Mode.RemoveDragon:
                    IsEnabled = baseInfo && !string.IsNullOrWhiteSpace(DragonId);
                    break;
            }

            if (IsImportMode)
                TextWarningUpdate();
            return ret;
        }

        private void TextWarningUpdate() {
            WarningText = "";
            if (string.IsNullOrWhiteSpace(Path))
                return;
            if (IsAvatarSelected && !Path.ToLower().EndsWith("getdetailedchildlist.xml") && !Path.ToLower().EndsWith("vikingprofiledata.xml"))
                WarningText = "WARNING: Selected file doesn't end with GetDetailedChildlist.xml or VikingProfileData.xml";
            else if (IsDragonsSelected && !Path.ToLower().EndsWith("getallactivepetsbyuserid.xml"))
                WarningText = "WARNING: Selected file doesn't end with GetAllActivePetsByuserId.xml ";
            else if (IsInventorySelected && !Path.ToLower().EndsWith("getcommoninventory.xml"))
                WarningText = "WARNING: Selected file doesn't end with GetCommonInventory.xml";
            else if (IsHideoutSelected && !Path.ToLower().EndsWith("myroomint.xml"))
                WarningText = "WARNING: Selected file doesn't end with MyRoomINT.xml";
            else if (IsFarmsSelected && !Path.ToLower().EndsWith("getuserroomlist.xml"))
                WarningText = "WARNING: Selected file doesn't end with GetUserRoomList.xml";
        }

        private void OnScrollRequested() {
            ScrollRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

public class TextWriterExt : StringWriter {
    public event EventHandler<string> TextWritten;

    public override void Write(char value) {
        base.Write(value);
        TextWritten?.Invoke(this, value.ToString());
    }

    public override void Write(string value) {
        base.Write(value);
        TextWritten?.Invoke(this, value);
    }

    public override Encoding Encoding => Encoding.UTF8;
}
