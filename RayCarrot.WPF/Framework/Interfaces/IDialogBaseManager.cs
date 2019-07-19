﻿using System.Threading.Tasks;
using RayCarrot.UI;

namespace RayCarrot.WPF
{
    /// <summary>
    /// A dialog base manager for managing dialogs
    /// </summary>
    public interface IDialogBaseManager
    {
        /// <summary>
        /// Shows the dialog and returns when the dialog closes with a result
        /// </summary>
        /// <typeparam name="V">The view model type</typeparam>
        /// <typeparam name="R">The return type</typeparam>
        /// <param name="dialog">The dialog to show</param>
        /// <param name="owner">The owner window</param>
        /// <returns>The result</returns>
        Task<R> ShowDialogAsync<V, R>(IDialogBaseControl<V, R> dialog, object owner)
            where V : UserInputViewModel;
    }
}