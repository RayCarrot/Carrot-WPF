﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Nito.AsyncEx;
using RayCarrot.CarrotFramework.Abstractions;
using RayCarrot.IO;
using RayCarrot.UI;
using RayCarrot.Windows.Registry;
using RayCarrot.Windows.Shell;

namespace RayCarrot.WPF
{
    /// <summary>
    /// A control for browsing file, directory and Registry paths
    /// </summary>
    [TemplatePart(Name = BrowseButtonName, Type = typeof(Button))]
    [TemplatePart(Name = OpenLocationMenuItemName, Type = typeof(MenuItem))]
    public class BrowseBox : Control
    {
        #region Static Constructor

        static BrowseBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BrowseBox), new FrameworkPropertyMetadata(typeof(BrowseBox)));
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrowseBox()
        {
            BrowseAsyncLock = new AsyncLock();
        }

        #endregion

        #region Protected Constants

        protected const string BrowseButtonName = "PART_BrowseButton";

        protected const string OpenLocationMenuItemName = "PART_OpenLocationMenuItem";

        #endregion

        #region Private Fields

        private Button _browseButton;

        private MenuItem _openLocationMenuItem;

        #endregion

        #region Private Properties

        /// <summary>
        /// The async lock for browsing
        /// </summary>
        private AsyncLock BrowseAsyncLock { get; }

        /// <summary>
        /// The browse button
        /// </summary>
        private Button BrowseButton
        {
            get => _browseButton;
            set
            {
                if (BrowseButton != null)
                {
                    BrowseButton.Click -= BrowseFileAsync;
                    BrowseButton.DragEnter -= BrowseButton_DragEnter;
                    BrowseButton.Drop -= BrowseButton_Drop;
                }

                _browseButton = value;

                if (BrowseButton != null)
                {
                    BrowseButton.Click += BrowseFileAsync;
                    BrowseButton.DragEnter += BrowseButton_DragEnter;
                    BrowseButton.Drop += BrowseButton_Drop;
                }
            }
        }

        /// <summary>
        /// The menu item for opening the location
        /// </summary>
        private MenuItem OpenLocationMenuItem
        {
            get => _openLocationMenuItem;
            set
            {
                if (OpenLocationMenuItem != null)
                {
                    OpenLocationMenuItem.Click -= OpenLocationMenuItem_ClickAsync;
                    OpenLocationMenuItem.Loaded -= OpenLocationMenuItem_Loaded;
                }

                _openLocationMenuItem = value;

                if (OpenLocationMenuItem != null)
                {
                    OpenLocationMenuItem.Click += OpenLocationMenuItem_ClickAsync;
                    OpenLocationMenuItem.Loaded += OpenLocationMenuItem_Loaded;
                }
            }
        }

        #endregion

        #region Public Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            BrowseButton = Template.FindName(BrowseButtonName, this) as Button;
            OpenLocationMenuItem = Template.FindName(OpenLocationMenuItemName, this) as MenuItem;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Checks if the current path is valid based on the browse type
        /// </summary>
        /// <returns>True if the path is valid, otherwise false</returns>
        protected virtual bool IsPathValid()
        {
            switch (BrowseType)
            {
                case BrowseTypes.File:
                    return File.Exists(SelectedPath);

                case BrowseTypes.Directory:
                    return Directory.Exists(SelectedPath);

                case BrowseTypes.Drive:
                    FileSystemPath path = SelectedPath;
                    return path.DirectoryExists && path.IsDirectoryRoot;

                case BrowseTypes.RegistryKey:
                    return RCFWinReg.RegistryManager.KeyExists(SelectedPath, SelectedRegistryView);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if the file drag/drop is allowed based on the current properties
        /// </summary>
        /// <returns>True if it's allowed, otherwise false</returns>
        protected virtual bool AllowFileDragDrop()
        {
            return BrowseType == BrowseTypes.Directory || BrowseType == BrowseTypes.File || BrowseType == BrowseTypes.Drive;
        }

        protected virtual async Task BrowseAsync()
        {
            using (await BrowseAsyncLock.LockAsync())
            {
                switch (BrowseType)
                {
                    case BrowseTypes.File:
                        var fileResult = await RCFUI.BrowseUI.BrowseFileAsync(new FileBrowserViewModel()
                        {
                            Title = "Select a file",
                            DefaultDirectory = IsPathValid() ? new FileSystemPath(SelectedPath).Parent.FullPath : InitialLocation,
                            DefaultName = UseCurrentPathAsDefaultLocationIfValid && IsPathValid() ? new FileSystemPath(SelectedPath).Name : String.Empty,
                            ExtensionFilter = FileFilter
                        });

                        if (fileResult.CanceledByUser)
                            return;

                        SelectedPath = fileResult.SelectedFile;

                        break;

                    case BrowseTypes.Directory:
                        var dirResult = await RCFUI.BrowseUI.BrowseDirectoryAsync(new DirectoryBrowserViewModel()
                        {
                            Title = "Select a file",
                            DefaultDirectory = UseCurrentPathAsDefaultLocationIfValid && IsPathValid() ? new FileSystemPath(SelectedPath).FullPath : InitialLocation,
                            DefaultName = IsPathValid() ? new FileSystemPath(SelectedPath).Name : String.Empty
                        });

                        if (dirResult.CanceledByUser)
                            return;

                        SelectedPath = dirResult.SelectedDirectory;

                        break;

                    case BrowseTypes.RegistryKey:
                        var keyResult = await RCFWinReg.RegistryBrowseUIManager.BrowseRegistryKeyAsync(new RegistryBrowserViewModel()
                        {
                            Title = "Select a Registry key",
                            AllowCustomRegistryView = AllowCustomRegistryView,
                            DefaultRegistryView = SelectedRegistryView,
                            BrowseValue = false,
                            DefaultKeyPath = UseCurrentPathAsDefaultLocationIfValid && IsPathValid() ? SelectedPath : InitialLocation,
                        });

                        if (keyResult.CanceledByUser)
                            return;

                        SelectedPath = keyResult.KeyPath;
                        SelectedRegistryView = keyResult.SelectedRegistryView;
                        break;

                    case BrowseTypes.Drive:
                        var driveResult = await RCFUI.BrowseUI.BrowseDriveAsync(new DriveBrowserViewModel()
                        {
                            Title = "Select a drive",
                            DefaultDirectory = UseCurrentPathAsDefaultLocationIfValid && IsPathValid() ? new FileSystemPath(SelectedPath).FullPath : InitialLocation,
                            MultiSelection = false,
                            AllowedTypes = AllowedDriveTypes,
                            AllowNonReadyDrives = AllowNonReadyDrives
                        });

                        if (driveResult.CanceledByUser)
                            return;

                        SelectedPath = driveResult.SelectedDrive;
                        break;

                    default:
                        throw new ArgumentException("The specified browse type is not valid");
                }

                MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        protected virtual async Task OpenLocationAsync()
        {
            if (!IsPathValid())
            {
                await RCFUI.MessageUI.DisplayMessageAsync($"The path {SelectedPath} does not exist", "Path not found", MessageType.Error);
                return;
            }

            try
            {
                switch (BrowseType)
                {
                    case BrowseTypes.File:
                    case BrowseTypes.Directory:
                    case BrowseTypes.Drive:
                        WindowsHelpers.OpenExplorerPath(SelectedPath);
                        break;

                    case BrowseTypes.RegistryKey:
                        WindowsHelpers.OpenRegistryPath(SelectedPath);
                        break;
                }
            }
            catch (Exception ex)
            {
                ex.HandleUnexpected("Opening browse location");
            }
        }

        protected virtual void FileDrop(DragEventArgs e)
        {
            if (!AllowFileDragDrop() || !e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            // Get the path
            FileSystemPath filePath = (e.Data.GetData(DataFormats.FileDrop) as string[])?.FirstOrDefault() ?? FileSystemPath.EmptyPath;

            // Get the target if it's a shortcut
            if (filePath.FullPath.EndsWith(".lnk"))
                filePath = WindowsHelpers.GetShortCutTarget(filePath);

            // Set the path
            SelectedPath = filePath;
        }

        #endregion

        #region Event Handlers

        private async void BrowseFileAsync(object sender, RoutedEventArgs e)
        {
            await BrowseAsync();
        }

        private void BrowseButton_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = AllowFileDragDrop() && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void BrowseButton_Drop(object sender, DragEventArgs e)
        {
            FileDrop(e);
        }

        private async void OpenLocationMenuItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            await OpenLocationAsync();
        }

        private void OpenLocationMenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            IsSelectedPathValid = IsPathValid();
        }

        #endregion

        #region Dependency Properties

        #region PathValidation

        /// <summary>
        /// The validation rule to use for the path
        /// </summary>
        public BrowseValidationRules PathValidation
        {
            get => (BrowseValidationRules)GetValue(PathValidationProperty);
            set => SetValue(PathValidationProperty, value);
        }

        public static readonly DependencyProperty PathValidationProperty = DependencyProperty.Register(nameof(PathValidation), typeof(BrowseValidationRules), typeof(BrowseBox), new FrameworkPropertyMetadata(BrowseValidationRules.None));

        #endregion

        #region IsSelectedPathValid

        private static readonly DependencyPropertyKey IsSelectedPathValidPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsSelectedPathValid), typeof(bool), typeof(BrowseBox), new FrameworkPropertyMetadata());

        public static readonly DependencyProperty IsSelectedPathValidProperty = IsSelectedPathValidPropertyKey.DependencyProperty;

        /// <summary>
        /// Indicates if the selected path is valid
        /// </summary>
        public bool IsSelectedPathValid
        {
            get => (bool)GetValue(IsSelectedPathValidProperty);
            protected set => SetValue(IsSelectedPathValidPropertyKey, value);
        }

        #endregion

        #region FileFilter

        /// <summary>
        /// The file filter to use when browsing
        /// </summary>
        public string FileFilter
        {
            get => (string)GetValue(FileFilterProperty);
            set => SetValue(FileFilterProperty, value);
        }

        public static readonly DependencyProperty FileFilterProperty = DependencyProperty.Register(nameof(FileFilter), typeof(string), typeof(BrowseBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region InitialLocation

        /// <summary>
        /// The default location when browsing
        /// </summary>
        public string InitialLocation
        {
            get => (string)GetValue(InitialFileDirectoryProperty);
            set => SetValue(InitialFileDirectoryProperty, value);
        }

        public static readonly DependencyProperty InitialFileDirectoryProperty = DependencyProperty.Register(nameof(InitialLocation), typeof(string), typeof(BrowseBox), new FrameworkPropertyMetadata(Environment.SpecialFolder.MyComputer.GetFolderPath().FullPath, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region SelectedPath

        /// <summary>
        /// The selected path
        /// </summary>
        public string SelectedPath
        {
            get => (string)GetValue(SelectedPathProperty);
            set => SetValue(SelectedPathProperty, value);
        }

        public static readonly DependencyProperty SelectedPathProperty = DependencyProperty.Register(nameof(SelectedPath), typeof(string), typeof(BrowseBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region BrowseType

        /// <summary>
        /// The type of path to browse for
        /// </summary>
        public BrowseTypes BrowseType
        {
            get => (BrowseTypes)GetValue(BrowseTypeProperty);
            set => SetValue(BrowseTypeProperty, value);
        }

        public static readonly DependencyProperty BrowseTypeProperty = DependencyProperty.Register(nameof(BrowseType), typeof(BrowseTypes), typeof(BrowseBox), new FrameworkPropertyMetadata(BrowseTypes.File));

        #endregion

        #region AllowedDriveTypes

        /// <summary>
        /// The allowed drive types
        /// </summary>
        public IEnumerable<DriveType> AllowedDriveTypes
        {
            get => (IEnumerable<DriveType>)GetValue(AllowedDriveTypesProperty);
            set => SetValue(AllowedDriveTypesProperty, value);
        }

        public static readonly DependencyProperty AllowedDriveTypesProperty = DependencyProperty.Register(nameof(AllowedDriveTypes), typeof(IEnumerable<DriveType>), typeof(BrowseBox));

        #endregion

        #region SelectedRegistryView

        /// <summary>
        /// The selected <see cref="RegistryView"/>
        /// </summary>
        public RegistryView SelectedRegistryView
        {
            get => (RegistryView)GetValue(SelectedRegistryViewProperty);
            set => SetValue(SelectedRegistryViewProperty, value);
        }

        public static readonly DependencyProperty SelectedRegistryViewProperty = DependencyProperty.Register(nameof(SelectedRegistryView), typeof(RegistryView), typeof(BrowseBox), new FrameworkPropertyMetadata(RegistryView.Default, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region UseCurrentPathAsDefaultLocationIfValid

        /// <summary>
        /// True if the current path should be used as the default location if it is valid
        /// </summary>
        public bool UseCurrentPathAsDefaultLocationIfValid
        {
            get => (bool)GetValue(UseCurrentPathAsDefaultLocationIfValidProperty);
            set => SetValue(UseCurrentPathAsDefaultLocationIfValidProperty, value);
        }

        public static readonly DependencyProperty UseCurrentPathAsDefaultLocationIfValidProperty = DependencyProperty.Register(nameof(UseCurrentPathAsDefaultLocationIfValid), typeof(bool), typeof(BrowseBox), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region CanBrowse

        /// <summary>
        /// True if the user can browse for a path
        /// </summary>
        public bool CanBrowse
        {
            get => (bool)GetValue(CanBrowseProperty);
            set => SetValue(CanBrowseProperty, value);
        }

        public static readonly DependencyProperty CanBrowseProperty = DependencyProperty.Register(nameof(CanBrowse), typeof(bool), typeof(BrowseBox), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region AllowNonReadyDrives

        /// <summary>
        /// True if non-ready drives are allowed to be selected, false if not
        /// </summary>
        public bool AllowNonReadyDrives
        {
            get => (bool)GetValue(AllowNonReadyDrivesProperty);
            set => SetValue(AllowNonReadyDrivesProperty, value);
        }

        public static readonly DependencyProperty AllowNonReadyDrivesProperty = DependencyProperty.Register(nameof(AllowNonReadyDrives), typeof(bool), typeof(BrowseBox), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region AllowNonReadyDrives

        /// <summary>
        /// True if the user can change the <see cref="RegistryView"/>, false if not
        /// </summary>
        public bool AllowCustomRegistryView
        {
            get => (bool)GetValue(AllowCustomRegistryViewProperty);
            set => SetValue(AllowCustomRegistryViewProperty, value);
        }

        public static readonly DependencyProperty AllowCustomRegistryViewProperty = DependencyProperty.Register(nameof(AllowCustomRegistryView), typeof(bool), typeof(BrowseBox), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #endregion
    }
}