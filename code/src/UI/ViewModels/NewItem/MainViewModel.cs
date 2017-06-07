﻿// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Templates.Core;
using Microsoft.Templates.Core.Diagnostics;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.Core.Locations;
using Microsoft.Templates.Core.Mvvm;
using Microsoft.Templates.UI.Controls;
using Microsoft.Templates.UI.Resources;
using Microsoft.Templates.UI.Services;
using Microsoft.Templates.UI.Views.NewItem;
using System.Windows.Controls;
using Microsoft.Templates.UI.ViewModels.Common;
using System.IO;
using System.Xml.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.Templates.UI.Extensions;

namespace Microsoft.Templates.UI.ViewModels.NewItem
{
    public class MainViewModel : Observable
    {
        private bool _canGoBack;
        private bool _canGoForward;
        private bool _hasValidationErrors;
        private bool _templatesAvailable;
        private OverlayBox _overlayBox;

        public static MainViewModel Current;
        public MainView MainView;
        public TemplateType ConfigTemplateType;
        public string ConfigFramework;
        public string ConfigProjectType;
        public string LastSelectedTemplateIdentity;

        private StatusViewModel _status = StatusControl.EmptyStatus;

        public StatusViewModel Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _wizardVersion;
        public string WizardVersion
        {
            get => _wizardVersion;
            set => SetProperty(ref _wizardVersion, value);
        }

        private string _templatesVersion;
        public string TemplatesVersion
        {
            get => _templatesVersion;
            set => SetProperty(ref _templatesVersion, value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private bool _newUpdateAvailable;
        public bool NewUpdateAvailable
        {
            get => _newUpdateAvailable;
            set => SetProperty(ref _newUpdateAvailable, value);
        }

        private Visibility _infoShapeVisibility = Visibility.Collapsed;
        public Visibility InfoShapeVisibility
        {
            get => _infoShapeVisibility;
            set => SetProperty(ref _infoShapeVisibility, value);
        }

        private Visibility _loadingContentVisibility = Visibility.Visible;
        public Visibility LoadingContentVisibility
        {
            get => _loadingContentVisibility;
            set => SetProperty(ref _loadingContentVisibility, value);
        }

        private Visibility _loadedContentVisibility = Visibility.Collapsed;
        public Visibility LoadedContentVisibility
        {
            get => _loadedContentVisibility;
            set => SetProperty(ref _loadedContentVisibility, value);
        }

        private Visibility _noContentVisibility = Visibility.Collapsed;
        public Visibility NoContentVisibility
        {
            get => _noContentVisibility;
            set => SetProperty(ref _noContentVisibility, value);
        }

        private Visibility _finishButtonVisibility = Visibility.Collapsed;
        public Visibility FinishButtonVisibility
        {
            get => _finishButtonVisibility;
            set => SetProperty(ref _finishButtonVisibility, value);
        }

        private Visibility _nextButtonVisibility = Visibility.Visible;
        public Visibility NextButtonVisibility
        {
            get => _nextButtonVisibility;
            set => SetProperty(ref _nextButtonVisibility, value);
        }

        //private Visibility _wizardInfoVisibility = Visibility.Collapsed;
        //public Visibility WizardInfoVisibility
        //{
        //    get => _wizardInfoVisibility;
        //    set => SetProperty(ref _wizardInfoVisibility, value);
        //}

        private RelayCommand _cancelCommand;
        private RelayCommand _goBackCommand;
        private RelayCommand _nextCommand;
        private RelayCommand _finishCommand;
        private RelayCommand _showOverlayMenuCommand;

        public RelayCommand CancelCommand => _cancelCommand ?? (_cancelCommand = new RelayCommand(OnCancel));
        public RelayCommand BackCommand => _goBackCommand ?? (_goBackCommand = new RelayCommand(OnGoBack, () => _canGoBack));
        public RelayCommand NextCommand => _nextCommand ?? (_nextCommand = new RelayCommand(OnNext, () => _templatesAvailable && !_hasValidationErrors && _canGoForward));
        public RelayCommand FinishCommand => _finishCommand ?? (_finishCommand = new RelayCommand(OnFinish, CanFinish));
        public RelayCommand ShowOverlayMenuCommand => _showOverlayMenuCommand ?? (_showOverlayMenuCommand = new RelayCommand(OnShowOverlayMenu));

        private bool CanFinish()
        {
            if (_hasValidationErrors)
            {
                return false;
            }
            CleanStatus();
            return true;
        }

        public NewItemSetupViewModel NewItemSetup { get; private set; } = new NewItemSetupViewModel();

        //public ProjectTemplatesViewModel ProjectTemplates { get; private set; } = new ProjectTemplatesViewModel();

        //public ObservableCollection<SummaryLicenseViewModel> SummaryLicenses { get; } = new ObservableCollection<SummaryLicenseViewModel>();

        public MainViewModel(MainView mainView)
        {
            MainView = mainView;
            Current = this;
        }

        public async Task InitializeAsync(TemplateType templateType, OverlayBox overlayBox)
        {
            _overlayBox = overlayBox;
            ConfigTemplateType = templateType;

            var projectConfiguration = GenController.ReadProjectConfiguration();
            ConfigProjectType = projectConfiguration.ProjectType;
            ConfigFramework = projectConfiguration.Framework;


            GenContext.ToolBox.Repo.Sync.SyncStatusChanged += Sync_SyncStatusChanged;

            //SummaryLicenses.CollectionChanged += (s, o) => { OnPropertyChanged(nameof(SummaryLicenses)); };

            try
            {
                Title = String.Format(StringRes.NewItemTitle_SF, ConfigTemplateType.ToString().ToLower());
                await GenContext.ToolBox.Repo.SynchronizeAsync();

                TemplatesVersion = GenContext.ToolBox.TemplatesVersion;
                WizardVersion = GenContext.ToolBox.WizardVersion;
            }
            catch (Exception ex)
            {
                Status = new StatusViewModel(StatusType.Information, StringRes.ErrorSync, true);

                await AppHealth.Current.Error.TrackAsync(ex.ToString());
                await AppHealth.Current.Exception.TrackAsync(ex);
            }
            finally
            {
                LoadingContentVisibility = Visibility.Collapsed;
                LoadedContentVisibility = Visibility.Visible;
            }
        }

        public void UnsuscribeEventHandlers()
        {
            GenContext.ToolBox.Repo.Sync.SyncStatusChanged -= Sync_SyncStatusChanged;
        }

        //public void RebuildLicenses()
        //{
        //    var userSelection = CreateUserSelection();
        //    var genItems = GenComposer.Compose(userSelection);

        //    var genLicenses = genItems
        //                        .SelectMany(s => s.Template.GetLicenses())
        //                        .Distinct(new TemplateLicenseEqualityComparer())
        //                        .ToList();

        //    SyncLicenses(genLicenses);
        //}

        public void EnableGoForward()
        {
            _canGoForward = true;
            NextCommand.OnCanExecuteChanged();
        }

        private async void Sync_SyncStatusChanged(object sender, SyncStatus status)
        {

            Status = new StatusViewModel(StatusType.Information, GetStatusText(status), true);

            if (status == SyncStatus.Updated)
            {
                TemplatesVersion = GenContext.ToolBox.Repo.TemplatesVersion;
                CleanStatus();

                _templatesAvailable = true;
                NewItemSetup.Initialize();
            }

            //if (status == SyncStatus.OverVersion)
            //{
            //    MainView.Dispatcher.Invoke(() =>
            //    {
            //        Status = new StatusViewModel(StatusType.Warning, StringRes.StatusOverVersionContent);
            //    });
            //}

            //if (status == SyncStatus.OverVersionNoContent)
            //{
            //    MainView.Dispatcher.Invoke(() =>
            //    {
            //        Status = new StatusViewModel(StatusType.Error, StringRes.StatusOverVersionNoContent);
            //        _templatesAvailable = false;
            //        NextCommand.OnCanExecuteChanged();
            //    });
            //}

            //if (status == SyncStatus.UnderVersion)
            //{
            //    MainView.Dispatcher.Invoke(() =>
            //    {
            //        Status = new StatusViewModel(StatusType.Error, StringRes.StatusLowerVersionContent);
            //        _templatesAvailable = false;
            //        NextCommand.OnCanExecuteChanged();
            //    });
            //}
        }

        private string GetStatusText(SyncStatus status)
        {
            switch (status)
            {
                case SyncStatus.Updating:
                    return StringRes.StatusUpdating;
                case SyncStatus.Updated:
                    return StringRes.StatusUpdated;
                case SyncStatus.Adquiring:
                    return StringRes.StatusAdquiring;
                case SyncStatus.Adquired:
                    return StringRes.StatusAdquired;
                case SyncStatus.Preparing:
                    return StringRes.StatusPreparing;
                case SyncStatus.Prepared:
                    return StringRes.StatusPrepared;
                default:
                    return string.Empty;
            }
        }

        private void OnCancel()
        {
            MainView.DialogResult = false;
            MainView.Result = null;

            MainView.Close();
        }

        private async void OnNext()
        {
            UpdateUserSelection();
            await GenController.GenerateNewItemAsync(MainView.Result);
            NavigationService.Navigate(new NewItemChangesSummaryView());

            _canGoBack = true;
            BackCommand.OnCanExecuteChanged();

            FinishButtonVisibility = Visibility.Visible;
            NextButtonVisibility = Visibility.Collapsed;
        }

        private void UpdateUserSelection()
        {
            MainView.Result = new UserSelection()
            {
                Framework = ConfigFramework,
                ProjectType = ConfigProjectType,
                HomeName = String.Empty
            };
            var activeGroup = NewItemSetup.TemplateGroups.FirstOrDefault(gr => gr.SelectedItem != null);
            if (activeGroup != null)
            {
                var template = activeGroup.SelectedItem as TemplateInfoViewModel;
                var dependencies = GenComposer.GetAllDependencies(template.Template, ConfigFramework);

                MainView.Result.Pages.Clear();
                MainView.Result.Features.Clear();

                AddTemplate(NewItemSetup.ItemName, template.Template, ConfigTemplateType);

                foreach (var dependencyTemplate in dependencies)
                {
                    AddTemplate(dependencyTemplate.GetDefaultName(), dependencyTemplate, dependencyTemplate.GetTemplateType());
                }

                if (String.IsNullOrEmpty(LastSelectedTemplateIdentity))
                {
                    LastSelectedTemplateIdentity = template.Identity;
                }
                else if (LastSelectedTemplateIdentity != template.Identity)
                {
                    GenController.CleanupTempGeneration();
                    LastSelectedTemplateIdentity = template.Identity;
                }
            }
        }
        private void AddTemplate(string name, ITemplateInfo template, TemplateType templateType)
        {
            if (templateType == TemplateType.Page)
            {
                MainView.Result.Pages.Add((name, template));
            }
            else if (templateType == TemplateType.Feature)
            {
                MainView.Result.Features.Add((name, template));
            }
        }

        private void OnGoBack()
        {
            NavigationService.GoBack();
            _canGoBack = false;
            BackCommand.OnCanExecuteChanged();

            FinishButtonVisibility = Visibility.Collapsed;
            NextButtonVisibility = Visibility.Visible;
        }

        private void OnFinish()
        {
            MainView.DialogResult = true;
            MainView.Close();
        }

        private void OnShowOverlayMenu()
        {
            if (_overlayBox.Opacity == 0)
            {
                _overlayBox.FadeIn();
            }
            else
            {
                _overlayBox.FadeOut();
            }
        }

        //private void SyncLicenses(IEnumerable<TemplateLicense> licenses)
        //{
        //    var toRemove = new List<SummaryLicenseViewModel>();

        //    foreach (var summaryLicense in SummaryLicenses)
        //    {
        //        if (!licenses.Any(l => l.Url == summaryLicense.Url))
        //        {
        //            toRemove.Add(summaryLicense);
        //        }
        //    }

        //    foreach (var licenseToRemove in toRemove)
        //    {
        //        SummaryLicenses.Remove(licenseToRemove);
        //    }

        //    foreach (var license in licenses)
        //    {
        //        if (!SummaryLicenses.Any(l => l.Url == license.Url))
        //        {
        //            SummaryLicenses.Add(new SummaryLicenseViewModel(license));
        //        }
        //    }
        //}

        public void SetValidationErrors(string errorMessage, StatusType statusType = StatusType.Error)
        {
            Status = new StatusViewModel(statusType, errorMessage);
            _hasValidationErrors = true;
            FinishCommand.OnCanExecuteChanged();
        }

        public void CleanStatus(bool cleanValidationError = false)
        {
            Status = StatusControl.EmptyStatus;
            if (cleanValidationError)
            {
                _hasValidationErrors = false;
                NextCommand.OnCanExecuteChanged();
                FinishCommand.OnCanExecuteChanged();
            }
        }
    }
}
