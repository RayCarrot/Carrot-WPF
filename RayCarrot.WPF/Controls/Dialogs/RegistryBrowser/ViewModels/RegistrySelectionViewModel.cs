﻿using Microsoft.Win32;
using RayCarrot.CarrotFramework;
using RayCarrot.CarrotFramework.UI;
using RayCarrot.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        public RegistrySelectionViewModel() : this(new RegistryBrowserViewModel()
        {

        })
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="RegistrySelectionViewModel"/> from a browse view model
        /// </summary>
        /// <param name="browseVM">The view model</param>
        public RegistrySelectionViewModel(RegistryBrowserViewModel browseVM)
        {
            Keys = new ObservableCollection<RegistryKeyViewModel>();
            Favorites = new ObservableCollection<FavoritesItemViewModel>();
            BrowseVM = browseVM;
            Result = new RegistryBrowserResult();

            // Set values from defaults
            CurrentRegistryView = BrowseVM.DefaultRegistryView;
            ShowEmptyDefaultValues = BrowseVM.AllowEmptyDefaultValues;

            // Reset
            _ = ResetAsync();
        }

        #endregion

        #region Private Fields

        private RegistryView _currentRegistryView;

        private RegistryKeyViewModel _selectedItem;

        #endregion

        #region Public Properties

        /// <summary>
        /// The current result
        /// </summary>
        public virtual RegistryBrowserResult Result { get; }

        /// <summary>
        /// The browse view model
        /// </summary>
        public virtual RegistryBrowserViewModel BrowseVM { get; }

        /// <summary>
        /// The available keys to select
        /// </summary>
        public virtual ObservableCollection<RegistryKeyViewModel> Keys { get; }

        /// <summary>
        /// The favorites items
        /// </summary>
        public virtual ObservableCollection<FavoritesItemViewModel> Favorites { get; }

        /// <summary>
        /// The icon for RegEdit
        /// </summary>
        public virtual Bitmap RegeditIcon
        {
            get
            {
                try
                {
                    return RCFWin.WindowsFileInfoManager.GetIcon(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "regedit.exe"), IconSize.SmallIcon_16);
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
        public virtual RegistryView CurrentRegistryView
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
        /// True if nodes should be automatically selected when expanded
        /// </summary>
        public virtual bool AutoSelectOnExpand { get; set; }

        /// <summary>
        /// True if empty default values should be shown
        /// </summary>
        public virtual bool ShowEmptyDefaultValues { get; set; }

        /// <summary>
        /// The currently selected item
        /// </summary>
        public virtual RegistryKeyViewModel SelectedItem
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
                OpenInRegeditCommand.CanExecuteCommand = SelectedItem != null;
            }
        }

        /// <summary>
        /// The full key path of the currently selected item
        /// </summary>
        public virtual string SelectedItemFullPath
        {
            get => SelectedItem?.FullPath;
            set => _ = ExpandToPathAsync(value);
        }

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
        /// Resets the keys to the default keys and the favorites
        /// </summary>
        /// <returns>The task</returns>
        public virtual async Task ResetAsync()
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
                    var vm = new RegistryKeyViewModel(baseKey, this, new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext()));
                    Keys.Add(vm);
                    await vm.ResetCommand.ExecuteAsync();
                }

                if (Keys.Count == 0)
                    throw new Exception("The registry selection can not have 0 root keys");

                // Select the first item
                SelectedItem = Keys[0];

                // Reset the favorites
                ResetFavorites();
            }
            finally
            {
                Resetting = false;
            }
        }

        /// <summary>
        /// Resets the favorites
        /// </summary>
        public virtual void ResetFavorites()
        {
            Favorites.Clear();

            try
            {
                using (var key = RCFWin.RegistryManager.GetKeyFromFullPath(CommonRegistryPaths.RegeditFavoritesPath, RegistryView.Default))
                {
                    foreach (var value in key.GetValues())
                    {
                        Favorites.Add(new FavoritesItemViewModel(this)
                        {
                            Name = value.Name,
                            KeyPath = RCFWin.RegistryManager.NormalizePath(value.Value.ToString())
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ex.HandleError("Getting Registry favorites");
                RCFUI.MessageUI.DisplayMessage("Could not get the saved favorites", "Unknown error", MessageType.Error);
            }
        }

        /// <summary>
        /// Allows the user to expand to a given key path
        /// </summary>
        /// <returns>The task</returns>
        public virtual async Task ExpandToKeyAsync()
        {
            // Get the key
            var result = new StringInputDialog(new StringInputViewModel()
            {
                Title = "Expand to Key",
                HeaderText = "Enter the key path:",
                StringInput = SelectedItem?.FullPath
            }).ShowDialog();

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
        public virtual async Task ExpandToPathAsync(string keyPath)
        {
            if (ExpandingToPath)
                return;

            try
            {
                ExpandingToPath = true;

                if (!RCFWin.RegistryManager.KeyExists(keyPath, CurrentRegistryView))
                    return;

                string[] pathID = keyPath.Split(RCFWin.RegistryManager.KeySeparatorCharacter);
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
        public virtual async Task UpdateViewAsync(string[] selectedFullID, params string[][] expandedFullIDs)
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
                    for (int i = 0; i < path.Length; i++)
                    {
                        // Get the index
                        int index = keys.FindItemIndex(x => x.ID == path[i]);

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
                        SelectedItem = lastSelected;
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
        public virtual void OpenInRegedit()
        {
            if (SelectedItem == null)
            {
                RCFUI.MessageUI.DisplayMessage("No key has been selected", "Error Opening Key", MessageType.Information);
                return;
            }

            SelectedItem.OpenInRegedit();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Refreshes the key view models
        /// </summary>
        protected virtual async Task RefreshAsync()
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
            var selected = SelectedItem?.FullID;

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

        private RelayCommand _OpenInRegeditCommand;

        /// <summary>
        /// Command for opening the selected key in RegEdit
        /// </summary>
        public RelayCommand OpenInRegeditCommand => _OpenInRegeditCommand ?? (_OpenInRegeditCommand = new RelayCommand(OpenInRegedit, SelectedItem != null));

        private AsyncRelayCommand _RefreshCommand;

        /// <summary>
        /// Command for refreshing the key view models
        /// </summary>
        public AsyncRelayCommand RefreshCommand => _RefreshCommand ?? (_RefreshCommand = new AsyncRelayCommand(RefreshAsync));

        #endregion
    }
}