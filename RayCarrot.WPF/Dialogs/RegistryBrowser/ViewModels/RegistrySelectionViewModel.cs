﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RayCarrot.CarrotFramework.Abstractions;
using RayCarrot.Extensions;
using RayCarrot.IO;
using RayCarrot.UI;
using RayCarrot.Windows.Registry;
using RayCarrot.Windows.Shell;

namespace RayCarrot.WPF
{
    /// <summary>
    /// View model for a Registry selection
    /// </summary>
    public class RegistrySelectionViewModel : BaseViewModel
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="RegistrySelectionViewModel"/> with default values
        /// </summary>
        public RegistrySelectionViewModel() : this(new RegistryBrowserViewModel())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="RegistrySelectionViewModel"/> from a browse view model
        /// </summary>
        /// <param name="browseVM">The view model</param>
        public RegistrySelectionViewModel(RegistryBrowserViewModel browseVM)
        {
            UIFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
            Keys = new ObservableCollection<RegistryKeyViewModel>();
            Favorites = new ObservableCollection<FavoritesItemViewModel>();
            Values = new ObservableCollection<RegistryValueViewModel>();
            BrowseVM = browseVM;

            // Set values from defaults
            CurrentRegistryView = BrowseVM.DefaultRegistryView;
            ShowEmptyDefaultValues = BrowseVM.AllowEmptyDefaultValues;

            // Reset the view with the default path
            string[] pathID = BrowseVM.DefaultKeyPath?.Split(RCFWinReg.RegistryManager.KeySeparatorCharacter);
            _ = UpdateViewAsync(pathID ?? new string[] { RCFWinReg.RegistryManager.GetSubKeyName(browseVM.AvailableBaseKeys[0]) }, pathID ?? new string[] { });

            // Retrieve saved values
            RetrieveSavedValues();
        }

        #endregion

        #region Private Fields

        private RegistryView _currentRegistryView;

        private RegistryKeyViewModel _selectedItem;

        private bool _showEmptyDefaultValues;

        #endregion

        #region Public Properties

        /// <summary>
        /// The task factory for the UI
        /// </summary>
        public TaskFactory UIFactory { get; }

        /// <summary>
        /// The browse view model
        /// </summary>
        public RegistryBrowserViewModel BrowseVM { get; }

        /// <summary>
        /// The available keys to select
        /// </summary>
        public ObservableCollection<RegistryKeyViewModel> Keys { get; }

        /// <summary>
        /// The favorites items
        /// </summary>
        public ObservableCollection<FavoritesItemViewModel> Favorites { get; }

        /// <summary>
        /// The available values for the selected key
        /// </summary>
        public ObservableCollection<RegistryValueViewModel> Values { get; }

        /// <summary>
        /// The icon for RegEdit
        /// </summary>
        public Bitmap RegeditIcon
        {
            get
            {
                try
                {
                    return new FileSystemPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "regedit.exe")).GetIconOrThumbnail(ShellThumbnailSize.Small);
                }
                catch (Exception ex)
                {
                    ex.HandleUnexpected("Getting regedit icon");
                    return null;
                }
            }
        }

        /// <summary>
        /// The currently selected <see cref="RegistryView"/>
        /// </summary>
        public RegistryView CurrentRegistryView
        {
            get => _currentRegistryView;
            set
            {
                _currentRegistryView = value;

                // Refresh view
                RefreshCommand.Execute();
            }
        }

        /// <summary>
        /// True if empty default values should be shown
        /// </summary>
        public bool ShowEmptyDefaultValues
        {
            get => _showEmptyDefaultValues;
            set
            {
                if (value == _showEmptyDefaultValues)
                    return;

                _showEmptyDefaultValues = value;

                _ = RefreshValuesAsync();
            }
        }

        /// <summary>
        /// The currently selected key
        /// </summary>
        public RegistryKeyViewModel SelectedKey
        {
            get => _selectedItem;
            set
            {
                // Make sure the value has changed
                if (value == _selectedItem)
                    return;

                // Select if not null
                if (value != null)
                    value.IsSelected = true;

                // Update backing field
                _selectedItem = value;

                // Update command status
                OpenInRegeditCommand.CanExecuteCommand = SelectedKey != null;

                // Update the values
                _ = RefreshValuesAsync();
            }
        }

        /// <summary>
        /// The full key path of the currently selected item
        /// </summary>
        public string SelectedKeyFullPath
        {
            get => SelectedKey?.FullPath;
            set => _ = ExpandToPathAsync(value);
        }

        /// <summary>
        /// The currently selected value
        /// </summary>
        public RegistryValueViewModel SelectedValue { get; set; }

        /// <summary>
        /// The key currently being edited
        /// </summary>
        public RegistryKeyViewModel EditingKey { get; private set; }

        /// <summary>
        /// True if nodes should be automatically selected when expanded
        /// </summary>
        public bool AutoSelectOnExpand { get; set; }

        /// <summary>
        /// Indicates if nodes should expand on double click or enter rename state
        /// </summary>
        public bool DoubleClickToExpand { get; set; } = true;

        #endregion

        #region Protected Flags

        /// <summary>
        /// Indicates if <see cref="ExpandingToPath"/> is currently running
        /// </summary>
        protected bool ExpandingToPath { get; set; }

        /// <summary>
        /// Indicates if <see cref="UpdateViewAsync(string[], string[][])"/> is currently running
        /// </summary>
        protected bool UpdatingView { get; set; }

        /// <summary>
        /// Indicates if <see cref="ResetAsync"/> is currently running
        /// </summary>
        protected bool Resetting { get; set; }

        #endregion

        #region Public Method

        /// <summary>
        /// Sets the key currently being edited
        /// </summary>
        /// <param name="key">The new key to edit, or null if no keys should be editing</param>
        /// <returns>The task</returns>
        public async Task SetEditingKeyAsync(RegistryKeyViewModel key)
        {
            if (key == EditingKey)
                return;

            if (EditingKey != null)
                await EditingKey.SetIsEditingAsync(false);

            EditingKey = key;

            if (EditingKey != null)
                await EditingKey.SetIsEditingAsync(true);
        }

        /// <summary>
        /// Retrieves the saved values
        /// </summary>
        public void RetrieveSavedValues()
        {
            try
            {
                // Get the path
                var path = WPFRegistryPaths.BaseKeyPath;

                // Get values
                AutoSelectOnExpand = Registry.GetValue(path, nameof(AutoSelectOnExpand), AutoSelectOnExpand ? 1 : 0).CastTo<int>() == 1;
                DoubleClickToExpand = Registry.GetValue(path, nameof(DoubleClickToExpand), DoubleClickToExpand ? 1 : 0).CastTo<int>() == 1;
            }
            catch (Exception ex)
            {
                ex.HandleUnexpected("Getting Registry values for Registry selection options");
            }
        }

        /// <summary>
        /// Saves the saved values
        /// </summary>
        public void SaveSavedValues()
        {
            try
            {
                // Get the path
                var path = WPFRegistryPaths.BaseKeyPath;

                // Set values
                Registry.SetValue(path, nameof(AutoSelectOnExpand), AutoSelectOnExpand ? 1 : 0);
                Registry.SetValue(path, nameof(DoubleClickToExpand), DoubleClickToExpand ? 1 : 0);
            }
            catch (Exception ex)
            {
                ex.HandleUnexpected("Setting Registry values for Registry selection options");
            }
        }

        /// <summary>
        /// Resets the keys to the default keys and the favorites
        /// </summary>
        /// <returns>The task</returns>
        public async Task ResetAsync()
        {
            if (Resetting)
                return;

            try
            {
                Resetting = true;

                // Clear collection
                Keys.Clear();

                // Add root keys
                foreach (string baseKey in BrowseVM.AvailableBaseKeys)
                {
                    var vm = new RegistryKeyViewModel(baseKey, this);
                    await vm.EnableSynchronizationAsync();
                    Keys.Add(vm);
                    await vm.ResetCommand.ExecuteAsync();
                }

                if (Keys.Count == 0)
                    throw new Exception("The registry selection can not have 0 root keys");

                // Select the first item
                SelectedKey = Keys[0];

                // Reset the favorites
                await ResetFavoritesAsync();
            }
            finally
            {
                Resetting = false;
            }
        }

        /// <summary>
        /// Resets the favorites
        /// </summary>
        public async Task ResetFavoritesAsync()
        {
            try
            {
                lock (this)
                {
                    Favorites.Clear();

                    using (var key = RCFWinReg.RegistryManager.GetKeyFromFullPath(CommonRegistryPaths.RegeditFavoritesPath, RegistryView.Default))
                    {
                        foreach (var value in key.GetValues())
                        {
                            Favorites.Add(new FavoritesItemViewModel(this)
                            {
                                Name = value.Name,
                                KeyPath = RCFWinReg.RegistryManager.NormalizePath(value.Value.ToString())
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.HandleError("Getting Registry favorites");
                await RCFUI.MessageUI.DisplayMessageAsync("Could not get the saved favorites", "Unknown error", MessageType.Error);
            }
        }

        /// <summary>
        /// Allows the user to expand to a given key path
        /// </summary>
        /// <returns>The task</returns>
        public async Task ExpandToKeyAsync()
        {
            // Get the key
            var result = await new StringInputDialog(new StringInputViewModel()
            {
                Title = "Expand to Key",
                HeaderText = "Enter the key path:",
                StringInput = SelectedKey?.FullPath
            }).ShowDialogAsync();

            // Make sure it was not canceled
            if (result.CanceledByUser)
                return;

            // Expand to the key
            await ExpandToPathAsync(result.StringInput);
        }

        /// <summary>
        /// Expands to the given path
        /// </summary>
        /// <param name="keyPath">The path of the key to expand to</param>
        /// <returns>The task</returns>
        public async Task ExpandToPathAsync(string keyPath)
        {
            if (ExpandingToPath)
                return;

            try
            {
                ExpandingToPath = true;

                if (!RCFWinReg.RegistryManager.KeyExists(keyPath, CurrentRegistryView))
                    return;

                string[] pathID = keyPath.Split(RCFWinReg.RegistryManager.KeySeparatorCharacter);
                await UpdateViewAsync(pathID, pathID);
            }
            finally
            {
                ExpandingToPath = false;
            }
        }

        /// <summary>
        /// Updates the view with the specified properties
        /// </summary>
        /// <param name="selectedFullID">The full ID of the key to select</param>
        /// <param name="expandedFullIDs">The full IDs of the keys to expand</param>
        /// <returns>The task</returns>
        public async Task UpdateViewAsync(string[] selectedFullID, params string[][] expandedFullIDs)
        {
            if (Resetting || UpdatingView)
                return;

            try
            {
                UpdatingView = true;

                // Reset the keys
                await ResetAsync();

                // Expand all nodes which match the given paths
                foreach (var path in expandedFullIDs)
                {
                    // Save the last key collection
                    var keys = Keys;

                    // Expand all keys and sub keys
                    foreach (var item in path)
                    {
                        // Get the index
                        int index = keys.FindItemIndex(x => x.ID == item);

                        // Make sure we got an index
                        if (index == -1)
                            break;

                        // Get the key
                        var key = keys[index];

                        // Expand the key if not expanded
                        if (!key.IsExpanded)
                            await key.ExpandAsync();

                        // Set the last node collection
                        keys = key;
                    }
                }

                if (selectedFullID != null)
                {
                    RegistryKeyViewModel lastSelected = null;
                    foreach (var root in Keys)
                    {
                        if (root.FullID.SequenceEqual(selectedFullID))
                        {
                            lastSelected = root;
                            break;
                        }

                        // Find the selected key
                        lastSelected = root.GetAllChildren().FindItem(x => x.FullID.SequenceEqual(selectedFullID));

                        if (lastSelected != null)
                            break;
                    }

                    // Select it if found
                    if (lastSelected != null)
                        SelectedKey = lastSelected;
                }
            }
            finally
            {
                UpdatingView = false;
            }
        }

        /// <summary>
        /// Opens the currently selected key in RegEdit
        /// </summary>
        public async Task OpenInRegeditAsync()
        {
            if (SelectedKey == null)
            {
                await RCFUI.MessageUI.DisplayMessageAsync("No key has been selected", "Error Opening Key", MessageType.Information);
                return;
            }

            await SelectedKey.OpenInRegeditAsync();
        }

        /// <summary>
        /// Refreshes the list of values
        /// </summary>
        public async Task RefreshValuesAsync()
        {
            // Clear the values
            Values.Clear();

            // Make sure a key is selected
            if (SelectedKey == null)
                return;

            try
            {
                lock (this)
                {
                    using (RegistryKey key = RCFWinReg.RegistryManager.GetKeyFromFullPath(SelectedKeyFullPath, CurrentRegistryView))
                    {
                        // Add values
                        Values.AddRange(key.GetValues().Select(x => new RegistryValueViewModel()
                        {
                            Name = x.Name,
                            Data = x.Value,
                            Type = x.ValueKind
                        }));

                        // Add empty default if non has been added and if set to do so
                        if (ShowEmptyDefaultValues && !Values.Any(x => x.IsDefault))
                        {
                            Values.Add(new RegistryValueViewModel()
                            {
                                Name = String.Empty,
                                Type = RegistryValueKind.String
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.HandleExpected("Getting Registry key values");
                await RCFUI.MessageUI.DisplayMessageAsync("The Registry key values could not be obtained for the selected key", "Error retrieving values", MessageType.Error);
            }
        }

        /// <summary>
        /// Begins editing of the currently selected key
        /// </summary>
        public async Task BeginEditAsync()
        {
            // Make sure a key is selected and the selected key has a parent key in the list and it does not have access denied
            if (SelectedKey?.Parent != null && !SelectedKey.AccessDenied && SelectedKey.CanEditKey)
                await SetEditingKeyAsync(SelectedKey);
        }

        /// <summary>
        /// End editing of key in edit state
        /// </summary>
        public async Task EndEditAsync()
        {
            await SetEditingKeyAsync(null);
        }

        /// <summary>
        /// Deletes the selected key
        /// </summary>
        public async Task DeleteKeyAsync()
        {
            if (SelectedKey != null)
                await SelectedKey.DeleteKeyAsync();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Refreshes the key view models
        /// </summary>
        protected async Task RefreshAsync()
        {
            // Save all paths which are expanded
            var expanded = new List<string[]>();
            foreach (var root in Keys)
            {
                if (root.IsExpanded)
                    expanded.Add(root.FullID);

                expanded.AddRange(root.GetAllChildren().Where(x => x?.IsExpanded == true).Select(x => x.FullID));
            }

            // Save the selected path
            var selected = SelectedKey?.FullID;

            // Update view to specified paths
            await UpdateViewAsync(selected, expanded.ToArray());
        }

        #endregion

        #region Commands

        private AsyncRelayCommand _ExpandToKeyCommand;

        /// <summary>
        /// Command for expanding to key
        /// </summary>
        public AsyncRelayCommand ExpandToKeyCommand => _ExpandToKeyCommand ?? (_ExpandToKeyCommand = new AsyncRelayCommand(ExpandToKeyAsync));

        private AsyncRelayCommand _OpenInRegeditCommand;

        /// <summary>
        /// Command for opening the selected key in RegEdit
        /// </summary>
        public AsyncRelayCommand OpenInRegeditCommand => _OpenInRegeditCommand ?? (_OpenInRegeditCommand = new AsyncRelayCommand(OpenInRegeditAsync, SelectedKey != null));

        private AsyncRelayCommand _RefreshCommand;

        /// <summary>
        /// Command for refreshing the key view models
        /// </summary>
        public AsyncRelayCommand RefreshCommand => _RefreshCommand ?? (_RefreshCommand = new AsyncRelayCommand(RefreshAsync));

        private AsyncRelayCommand _BeginEditCommand;

        /// <summary>
        /// Command for beginning editing of the selected key
        /// </summary>
        public AsyncRelayCommand BeginEditCommand => _BeginEditCommand ?? (_BeginEditCommand = new AsyncRelayCommand(BeginEditAsync, !BrowseVM.DisableEditing));

        private AsyncRelayCommand _EndEditCommand;

        /// <summary>
        /// Command for ending editing of key in edit state
        /// </summary>
        public AsyncRelayCommand EndEditCommand => _EndEditCommand ?? (_EndEditCommand = new AsyncRelayCommand(EndEditAsync));

        private AsyncRelayCommand _DeleteKeyCommand;

        /// <summary>
        /// Command for deleting the selected key
        /// </summary>
        public AsyncRelayCommand DeleteKeyCommand => _DeleteKeyCommand ?? (_DeleteKeyCommand = new AsyncRelayCommand(DeleteKeyAsync, !BrowseVM.DisableEditing));

        #endregion
    }
}