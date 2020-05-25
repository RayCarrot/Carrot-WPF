﻿using Microsoft.Extensions.Logging;

namespace RayCarrot.WPF
{
    /// <summary>
    /// Shortcuts for the common application services
    /// </summary>
    public static class Services
    {
        /// <summary>
        /// Gets the logger factory for creating loggers
        /// </summary>
        public static ILoggerFactory LoggerFactory => BaseApp.Current.GetService<ILoggerFactory>();

        /// <summary>
        /// Gets the common app data
        /// </summary>
        public static ICommonAppData Data => BaseApp.Current.GetService<ICommonAppData>();

        /// <summary>
        /// The logs stored for this session, if a session logger is used
        /// </summary>
        public static ISessionLoggerCollection Logs => BaseApp.Current.GetService<ISessionLoggerCollection>();

        /// <summary>
        /// Gets the message UIManager
        /// </summary>
        public static IMessageUIManager MessageUI => BaseApp.Current.GetService<IMessageUIManager>();

        /// <summary>
        /// Gets the browse UIManager
        /// </summary>
        public static IBrowseUIManager BrowseUI => BaseApp.Current.GetService<IBrowseUIManager>();

        /// <summary>
        /// Gets the WPF style
        /// </summary>
        public static IWPFStyle WPFStyle => BaseApp.Current.GetService<IWPFStyle>();

        /// <summary>
        /// Gets the dialog base manager, or the default one
        /// </summary>
        public static IDialogBaseManager DialogBaseManager => BaseApp.Current.GetService<IDialogBaseManager>();
    }
}