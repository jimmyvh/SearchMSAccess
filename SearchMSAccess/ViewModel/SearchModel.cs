using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace SearchMSAccess
{
    public class SearchModel : ViewModelBase
    {
        private string searchTerm;
        private string folderPath;
        private string filePath;
        private string activeMessage;
        private string cacheInfo;
        private int progress;
        private bool zoekBestand;
        private bool zoekMap;
        private bool useCache;
        private bool cacheAvailable;
        private BackgroundWorker worker;
        private ObservableCollection<SearchResultModel> zoekResultaten;
        public RelayCommand SearchCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        public SearchModel()
        {
            ZoekResultaten = new ObservableCollection<SearchResultModel>();
            if (IsInDesignMode)
            {
                #region testdata
                // design mode variabelen
                SearchTerm = "voorbeeld tekst";
                FilePath = @"C:\test\voorbeeld.accdb";
                FolderPath = @"C:\test\subfolder\collectie";
                CacheInfo = "Geen cache beschikbaar";
                Progress = 50;
                ZoekBestand = true;
                ZoekResultaten.Add(new SearchResultModel("bestand1.accdb", "module2.vba", 23, "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."));
                ZoekResultaten.Add(new SearchResultModel("bestand met een langere naam xxxxxyyyy.accdb", "module2.vba", 23, "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."));
                ZoekResultaten.Add(new SearchResultModel("bestand2.accdb", "modulenaam met spaties.vba", 1234, "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."));
                ZoekResultaten.Add(new SearchResultModel("k.accdb", "module2.vba", 3225, "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."));

                #endregion
            }
            else
            {
            }

            CheckCacheStatus();
            worker = new BackgroundWorker();
            SearchCommand = new RelayCommand(ExecuteSearch, CanSearch);
            CancelCommand = new RelayCommand(CancelSearch, CanCancel);
            PropertyChanged += SearchModel_PropertyChanged;
            ActiveMessage = "Klaar om te zoeken";
        }

        private void CheckCacheStatus()
        {
            CacheInfo = "Geen cache beschikbaar";
            CacheAvailable = false;
        }

        private bool CanSearch()
        {
            return (!string.IsNullOrEmpty(FilePath) || !string.IsNullOrEmpty(FolderPath)) && !string.IsNullOrEmpty(SearchTerm) && !worker.IsBusy;
        }

        private void ExecuteSearch()
        {
            var verwerktefiles = 0;
            Progress = 0;
            ZoekResultaten.Clear();
            ActiveMessage = "Bezig met starten zoekopdracht ...";
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.DoWork += delegate (object s, DoWorkEventArgs args)
            {
                CancelCommand.RaiseCanExecuteChanged();
                var fileList = new List<string>();
                if (!string.IsNullOrEmpty(FilePath))
                    fileList.Add(FilePath);
                if (!string.IsNullOrEmpty(FolderPath))
                {
                    ActiveMessage = "Bezig met oplijsten bestanden ...";
                    fileList.AddRange(Directory.EnumerateFiles(FolderPath, "*.accdb", SearchOption.AllDirectories));
                }

                using (var client = new AccessClient())
                {
                    fileList = fileList.Where(f => !f.ToLower().Contains("backup") && !f.ToLower().Contains("\\archief\\") && !f.ToLower().Contains(".old") && !f.ToLower().Contains("(old")).ToList();

                    foreach (var file in fileList)
                    {
                        if (worker.CancellationPending)
                        {
                            args.Cancel = true;
                            ActiveMessage = "Zoekopdracht gestopt.";
                            return;
                        }

                        ActiveMessage = $"Bezig met doorzoeken {file} ({ verwerktefiles + 1 } van { fileList.Count })...";
                        try
                        {
                            var results = client.SearchFile(file, SearchTerm);
                            foreach (var result in results)
                            {
                                App.Current.Dispatcher.Invoke((Action)delegate // observablecollection werkt niet doorheen verschillende threads
                                {
                                    ZoekResultaten.Add(result);
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            App.Current.Dispatcher.Invoke((Action)delegate
                            {
                                ZoekResultaten.Add(new SearchResultModel(file, "ERROR", 0, ex.Message));
                            });
                        }
                        verwerktefiles++;
                        worker.ReportProgress((int)Math.Round((double)verwerktefiles / (double)fileList.Count * 100));
                    }
                    ActiveMessage = "Klaar!";
                }
            };

            worker.ProgressChanged += delegate (object sender, ProgressChangedEventArgs e) { Progress = e.ProgressPercentage; };
            worker.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) { CancelCommand.RaiseCanExecuteChanged(); };
            worker.RunWorkerAsync();
        }

        private bool CanCancel()
        {
            return worker != null && worker.IsBusy;
        }

        private void CancelSearch()
        {
            ActiveMessage = "Bezig met annuleren zoekopdracht ...";
            worker.CancelAsync();
        }

        private void SearchModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ZoekBestand":
                    if (ZoekBestand)
                        FolderPath = "";
                    break;
                case "ZoekMap":
                    if (ZoekMap)
                        FilePath = "";
                    break;
                case "SearchTerm":
                case "FolderPath":
                case "FilePath":
                    SearchCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        #region properties
        public string SearchTerm
        {
            get
            {
                return searchTerm;
            }
            set
            {
                searchTerm = value;
                RaisePropertyChanged("SearchTerm");
            }
        }

        public string FolderPath
        {
            get
            {
                return folderPath;
            }
            set
            {
                folderPath = value;
                RaisePropertyChanged("FolderPath");
            }
        }

        public string FilePath
        {
            get
            {
                return filePath;
            }
            set
            {
                filePath = value;
                RaisePropertyChanged("FilePath");
            }
        }

        public string ActiveMessage
        {
            get
            {
                return activeMessage;
            }
            set
            {
                activeMessage = value;
                RaisePropertyChanged("ActiveMessage");
            }
        }

        public bool ZoekBestand
        {
            get
            {
                return zoekBestand;
            }
            set
            {
                zoekBestand = value;
                RaisePropertyChanged("ZoekBestand");
            }
        }

        public int Progress
        {
            get
            {
                return progress;
            }
            set
            {
                progress = value;
                RaisePropertyChanged("Progress");
            }
        }

        public bool ZoekMap
        {
            get
            {
                return zoekMap;
            }
            set
            {
                zoekMap = value;
                RaisePropertyChanged("ZoekMap");
            }
        }

        public bool UseCache
        {
            get
            {
                return useCache;
            }
            set
            {
                useCache = value;
                RaisePropertyChanged("UseCache");
            }
        }

        public bool CacheAvailable
        {
            get
            {
                return cacheAvailable;
            }
            set
            {
                cacheAvailable = value;
                RaisePropertyChanged("CacheAvailable");
            }
        }

        public string CacheInfo
        {
            get
            {
                return cacheInfo;
            }
            set
            {
                cacheInfo = value;
                RaisePropertyChanged("CacheInfo");
            }
        }

        public ObservableCollection<SearchResultModel> ZoekResultaten
        {
            get
            {
                return zoekResultaten;
            }
            set
            {
                zoekResultaten = value;
                RaisePropertyChanged("ZoekResultaten");
            }
        }
        #endregion
    }
}
