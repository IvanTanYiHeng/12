﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using FocusTreeManager.DataContract;
using FocusTreeManager.Parsers;
using FocusTreeManager.ViewModel;
using FocusTreeManager.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls.Dialogs;
using MonitoredUndo;

namespace FocusTreeManager.Model.TabModels
{
    public class LocalisationModel : ObservableObject, ISupportsUndo
    {
        public Guid UniqueID { get; }

        private string visibleName;

        public string VisibleName
        {
            get
            {
                return visibleName;
            }
            set
            {
                if (value == visibleName)
                {
                    return;
                }
                DefaultChangeFactory.Current.OnChanging(this,
                         "VisibleName", visibleName, value, "VisibleName Changed");
                visibleName = value;
                RaisePropertyChanged(() => VisibleName);
            }
        }

        private string languageName;

        public string LanguageName
        {
            get
            {
                return languageName;
            }
            set
            {
                if (value == languageName)
                {
                    return;
                }
                languageName = value;
                RaisePropertyChanged(() => LanguageName);
            }
        }

        private DataContract.FileInfo fileInfo;

        public DataContract.FileInfo FileInfo
        {
            get
            {
                return fileInfo;
            }
            set
            {
                if (value == fileInfo)
                {
                    return;
                }
                fileInfo = value;
                RaisePropertyChanged(() => FileInfo);
            }
        }

        public ObservableCollection<LocaleModel> LocalisationMap { get; set; }

        public RelayCommand DeleteElementCommand { get; private set; }

        public RelayCommand EditElementCommand { get; private set; }

        public LocalisationModel(string Filename)
        {
            visibleName = Filename;
            UniqueID = Guid.NewGuid();
            LocalisationMap = new ObservableCollection<LocaleModel>();
            LocalisationMap.CollectionChanged += LocalisationMap_CollectionChanged;
            //Messenger
            Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
            //Commands
            DeleteElementCommand = new RelayCommand(SendDeleteSignal);
            EditElementCommand = new RelayCommand(SendEditSignal);
        }

        public LocalisationModel(LocalisationContainer container)
        {
            UniqueID = container.IdentifierID;
            visibleName = container.ContainerID;
            languageName = container.LanguageName;
            FileInfo = container.FileInfo;
            LocalisationMap = new ObservableCollection<LocaleModel>();
            foreach (LocaleContent content in container.LocalisationMap)
            {
                LocalisationMap.Add(new LocaleModel() { Key = content.Key, Value = content.Value});
            }
            LocalisationMap.CollectionChanged += LocalisationMap_CollectionChanged;
            //Messenger
            Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
            //Commands
            DeleteElementCommand = new RelayCommand(SendDeleteSignal);
            EditElementCommand = new RelayCommand(SendEditSignal);
        }

        private void NotificationMessageReceived(NotificationMessage msg)
        {
            if (visibleName == null)
            {
                return;
            }
            if (msg.Notification == "ContainerRenamed")
            {
                RaisePropertyChanged(() => visibleName);
            }
        }

        public string translateKey(string key)
        {
            LocaleModel locale = LocalisationMap.FirstOrDefault(l => 
                                    string.Equals(l.Key, key, 
                                    StringComparison.CurrentCultureIgnoreCase));
            return locale?.Value;
        }

        private void SendDeleteSignal()
        {
            Messenger.Default.Send(new NotificationMessage(this,
                new ViewModelLocator().ProjectView, "SendDeleteItemSignal"));
        }

        private void SendEditSignal()
        {
            Messenger.Default.Send(new NotificationMessage(this,
                new ViewModelLocator().ProjectView, "SendEditItemSignal"));
        }

        public async void CheckForChanges()
        {
            DataContract.FileInfo info = FileInfo;
            //check the fileinfo data
            if (info == null) return;
            //If the file exists
            if (!File.Exists(info.Filename)) return;
            //If the file was modified after the last modification date
            if (File.GetLastWriteTime(info.Filename) <= info.LastModifiedDate) return;
            //Then we can show a message
            MessageDialogResult Result = await new ViewModelLocator()
                .Main.ShowFileChangedDialog();
            if (Result == MessageDialogResult.Affirmative)
            {
                string oldText = LocalisationParser
                    .ParseLocalizationForCompare(this);
                string newText = LocalisationParser.
                    ParseLocalizationFileForCompare(info.Filename);
                SideBySideDiffModel model = new SideBySideDiffBuilder(
                    new Differ()).BuildDiffModel(oldText, newText);
                new ViewModelLocator().CodeComparator.DiffModel = model;
                new CompareCode().ShowDialog();
            }
        }

        #region Undo/Redo

        private void LocalisationMap_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Current.OnCollectionChanged(this, "LocalisationMap",
                LocalisationMap, e, "LocalisationMap Changed");
        }

        public object GetUndoRoot()
        {
            return new ViewModelLocator().Main;
        }

        #endregion
    }
}
