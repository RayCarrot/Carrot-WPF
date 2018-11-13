﻿using Microsoft.Win32;
using RayCarrot.CarrotFramework;
using RayCarrot.CarrotFramework.UI;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RayCarrot.WPF
{
    /// <summary>
    /// The default <see cref="IBrowseUIManager"/> for WPF
    /// </summary>
    public abstract class DefaultWPFBrowseUIManager : IBrowseUIManager
    {
        /// <summary>
        /// Indicates if the browse requests should be logged
        /// </summary>
        public virtual bool LogRequests { get; set; }

        /// <summary>
        /// The implementation for allowing the user to browse for a directory
        /// </summary>
        /// <param name="directoryBrowserModel">The directory browser information</param>
        /// <returns>The directory browser result</returns>
        protected abstract Task<DirectoryBrowserResult> BrowseDirectoryImplementationAsync(DirectoryBrowserViewModel directoryBrowserModel);

        /// <summary>
        /// Allows the user to browse for a directory
        /// </summary>
        /// <param name="directoryBrowserModel">The directory browser information</param>
        /// <returns>The directory browser result</returns>
        public virtual async Task<DirectoryBrowserResult> BrowseDirectoryAsync(DirectoryBrowserViewModel directoryBrowserModel, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (LogRequests)
                RCF.Logger.LogTraceSource($"A browse directory dialog was opened with the title of: {directoryBrowserModel.Title}", origin: origin, filePath: filePath, lineNumber: lineNumber);

            var result = await BrowseDirectoryImplementationAsync(directoryBrowserModel);

            if (result.CanceledByUser)
                RCF.Logger.LogTraceSource($"The browse directory dialog was canceled by the user");
            else
                RCF.Logger.LogTraceSource($"The browse directory dialog returned the selected directory paths {result.SelectedDirectories.JoinItems(", ")}");

            return result;
        }

        /// <summary>
        /// Allows the user to browse for a file
        /// </summary>
        /// <param name="fileBrowserModel">The file browser information</param>
        /// <returns>The file browser result</returns>
        public virtual Task<FileBrowserResult> BrowseFileAsync(FileBrowserViewModel fileBrowserModel, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (LogRequests)
                RCF.Logger.LogTraceSource($"A browse file dialog was opened with the title of: {fileBrowserModel.Title}", origin: origin, filePath: filePath, lineNumber: lineNumber);

            // Create the dialog
            OpenFileDialog filedialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                FileName = fileBrowserModel.DefaultName,
                Filter = fileBrowserModel.ExtensionFilter,
                InitialDirectory = fileBrowserModel.DefaultDirectory,
                Multiselect = fileBrowserModel.MultiSelection,
                Title = fileBrowserModel.Title ?? "Select a file"
            };

            // Show the dialog and get the result
            bool canceled = !filedialog.ShowDialog().Value;

            if (canceled)
                RCF.Logger.LogTraceSource($"The browse file dialog was canceled by the user");
            else
                RCF.Logger.LogTraceSource($"The browse file dialog returned the selected file paths {filedialog.FileNames.JoinItems(", ")}");

            // Return the result
            return Task.FromResult(new FileBrowserResult()
            {
                CanceledByUser = canceled,
                SelectedFile = filedialog.FileName,
                SelectedFiles = new List<string>(filedialog.FileNames)
            });
        }

        /// <summary>
        /// Allows the user to browse for a location and chose a name for a file to save
        /// </summary>
        /// <param name="saveFileModel">The save file browser information</param>
        /// <returns>The save file result</returns>
        public virtual Task<SaveFileResult> SaveFileAsync(SaveFileViewModel saveFileModel, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (LogRequests)
                RCF.Logger.LogTraceSource($"A save file dialog was opened with the title of: {saveFileModel.Title}", origin: origin, filePath: filePath, lineNumber: lineNumber);

            // Create the dialog
            SaveFileDialog savedialog = new SaveFileDialog()
            {
                FileName = saveFileModel.DefaultName,
                Filter = saveFileModel.Extensions,
                InitialDirectory = saveFileModel.DefaultDirectory,
                Title = saveFileModel.Title ?? "Save file"
            };

            // Show the dialog and get the result
            bool canceled = !savedialog.ShowDialog().Value;

            if (canceled)
                RCF.Logger.LogTraceSource($"The save file dialog was canceled by the user");
            else
                RCF.Logger.LogTraceSource($"The save file dialog returned the selected file path {savedialog.FileName}");

            // Return the result
            return Task.FromResult(new SaveFileResult()
            {
                CanceledByUser = canceled,
                SelectedFileLocation = savedialog.FileName
            });
        }

        /// <summary>
        /// Allows the user to browse for a drive
        /// </summary>
        /// <param name="driveBrowserModel">The drive browser information</param>
        /// <returns>The browse drive result</returns>
        public virtual async Task<DriveBrowserResult> BrowseDriveAsync(DriveBrowserViewModel driveBrowserModel, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (LogRequests)
                RCF.Logger.LogTraceSource($"A browse drive dialog was opened with the title of: {driveBrowserModel.Title}", origin: origin, filePath: filePath, lineNumber: lineNumber);

            // Create the dialog
            var driveSelectionDialog = new DriveSelectionDialog(driveBrowserModel);

            // Show the dialog and get the result
            var result = await driveSelectionDialog.ShowDialogAsync();

            if (result.CanceledByUser)
                RCF.Logger.LogTraceSource($"The browse drive dialog was canceled by the user");
            else
                RCF.Logger.LogTraceSource($"The browse drive dialog returned the selected drive paths {result.SelectedDrives.JoinItems(", ")}");

            // Return the result
            return result;
        }
    }
}