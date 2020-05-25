﻿using System.Collections.Generic;
using System.IO;

namespace RayCarrot.WPF
{
    /// <summary>
    /// A model to use when browsing for a drive
    /// </summary>
    public class DriveBrowserViewModel : BrowseViewModel
    {
        /// <summary>
        /// The allowed drive types
        /// </summary>
        public IEnumerable<DriveType> AllowedTypes { get; set; }

        /// <summary>
        /// Enables or disables multi selection option
        /// </summary>
        public bool MultiSelection { get; set; }

        /// <summary>
        /// True if non-ready drives are allowed to be selected, false if not
        /// </summary>
        public bool AllowNonReadyDrives { get; set; }
    }
}