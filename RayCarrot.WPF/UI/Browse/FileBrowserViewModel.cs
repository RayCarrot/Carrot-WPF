﻿namespace RayCarrot.WPF
{
    /// <summary>
    /// A model to use when browsing for a file
    /// </summary>
    public class FileBrowserViewModel : BrowseViewModel
    {
        /// <summary>
        /// The filter to use for file extensions
        /// </summary>
        public string ExtensionFilter { get; set; }

        /// <summary>
        /// Enables or disables multi selection option
        /// </summary>
        public bool MultiSelection { get; set; }
    }
}