﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using RayCarrot.CarrotFramework.Abstractions;
using RayCarrot.Extensions;
using RayCarrot.UI;

namespace RayCarrot.WPF
{
    /// <summary>
    /// Default dialog base manager, showing the dialog in a <see cref="Window"/>
    /// </summary>
    public class WindowDialogBaseManager : IDialogBaseManager
    {
        /// <summary>
        /// Shows the dialog and returns when the dialog closes with a result
        /// </summary>
        /// <typeparam name="V">The view model type</typeparam>
        /// <typeparam name="R">The return type</typeparam>
        /// <param name="dialog">The dialog to show</param>
        /// <param name="owner">The owner window</param>
        /// <returns>The result</returns>
        public Task<R> ShowDialogAsync<V, R>(IDialogBaseControl<V, R> dialog, object owner)
            where V : UserInputViewModel
        {
            using (dialog)
            {
                // Get the dispatcher
                var dispatcher = Application.Current.Dispatcher;

                // Make sure the dispatcher is not null
                if (dispatcher == null)
                    throw new Exception("A dialog can not be created before the application has been loaded");

                // Run on UI thread
                return dispatcher.Invoke(() =>
                {
                    // Create the window
                    var window = GetWindow();

                    // Configure the window
                    ConfigureWindow(window, dialog, owner);

                    void Dialog_CloseDialog(object sender, EventArgs e)
                    {
                        window.Close();
                    }

                    // Close window on request
                    dialog.CloseDialog += Dialog_CloseDialog;

                    // Show window as dialog
                    window.ShowDialog();

                    // Unsubscribe
                    dialog.CloseDialog -= Dialog_CloseDialog;

                    // Return the result
                    return Task.FromResult(dialog.GetResult());
                });
            }
        }

        /// <summary>
        /// Shows the Window without waiting for it to close
        /// </summary>
        /// <typeparam name="VM">The view model</typeparam>
        /// <param name="windowContent">The window content to show</param>
        /// <param name="owner">The owner window</param>
        /// <returns>The window</returns>
        public Task<Window> ShowWindowAsync<VM>(IWindowBaseControl<VM> windowContent, object owner)
            where VM : UserInputViewModel
        {
            lock (Application.Current)
            {
                // Get the dispatcher
                var dispatcher = Application.Current.Dispatcher;

                // Make sure the dispatcher is not null
                if (dispatcher == null)
                    throw new Exception("A dialog can not be created before the application has been loaded");

                // Run on UI thread
                return Task.FromResult(dispatcher.Invoke(() =>
                {
                    // Create the window
                    var window = GetWindow();

                    // Configure the window
                    ConfigureWindow(window, windowContent, owner);

                    // Show the window
                    WindowHelpers.ShowWindow(window, WindowHelpers.ShowWindowFlags.DuplicatesAllowed, windowContent.GetType().FullName);

                    // Return the window
                    return window;
                }));
            }
        }

        /// <summary>
        /// Creates a new window with the specified content
        /// </summary>
        /// <typeparam name="VM">The view model</typeparam>
        /// <param name="window">The window to configure</param>
        /// <param name="windowContent">The window content to show</param>
        /// <param name="owner">The owner window</param>
        public virtual void ConfigureWindow<VM>(Window window, IWindowBaseControl<VM> windowContent, object owner)
            where VM : UserInputViewModel
        {
            // Set window properties
            window.Content = windowContent.UIContent;
            window.ResizeMode = windowContent.Resizable ? ResizeMode.CanResize : ResizeMode.NoResize;
            window.Title = windowContent.ViewModel.Title ?? String.Empty;
            window.SizeToContent = windowContent.Resizable ? SizeToContent.Manual : SizeToContent.WidthAndHeight;

            // Set size properties
            switch (windowContent.BaseSize)
            {
                case DialogBaseSize.Smallest:
                    if (windowContent.Resizable)
                    {
                        window.Height = 100;
                        window.Width = 150;
                    }

                    window.MinHeight = 100;
                    window.MinWidth = 150;

                    break;

                case DialogBaseSize.Small:
                    if (windowContent.Resizable)
                    {
                        window.Height = 200;
                        window.Width = 250;
                    }

                    window.MinHeight = 200;
                    window.MinWidth = 250;

                    break;

                case DialogBaseSize.Medium:
                    if (windowContent.Resizable)
                    {
                        window.Height = 350;
                        window.Width = 500;
                    }

                    window.MinHeight = 300;
                    window.MinWidth = 400;

                    break;

                case DialogBaseSize.Large:
                    if (windowContent.Resizable)
                    {
                        window.Height = 475;
                        window.Width = 750;
                    }

                    window.MinHeight = 350;
                    window.MinWidth = 500;

                    break;

                case DialogBaseSize.Largest:
                    if (windowContent.Resizable)
                    {
                        window.Height = 600;
                        window.Width = 900;
                    }

                    window.MinHeight = 500;
                    window.MinWidth = 650;

                    break;
            }

            // Set owner
            if (owner is Window ow)
                window.Owner = ow;
            else if (owner is IntPtr oi)
                new WindowInteropHelper(window).Owner = oi;
            else
                window.Owner = Application.Current?.Windows.Cast<Window>().FindItem(x => x.IsActive);

            // Set startup location
            window.WindowStartupLocation = window.Owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;

            // Attempt to get default Window style from Framework
            window.Style = RCF.GetService<IWPFStyle>(false)?.WindowStyle ?? window.Style;
        }

        /// <summary>
        /// Gets a new instance of a window
        /// </summary>
        /// <returns>The window instance</returns>
        public virtual Window GetWindow()
        {
            return new Window();
        }
    }
}