﻿using RayCarrot.CarrotFramework;
using System.Threading.Tasks;

namespace RayCarrot.WPF
{
    /// <summary>
    /// Extension methods for <see cref="IDialogBaseControl{V, R}"/>
    /// </summary>
    public static class DialogBaseControlExtensions
    {
        /// <summary>
        /// Shows the dialog and returns when the dialog closes with a result
        /// </summary>
        /// <typeparam name="V">The view model type</typeparam>
        /// <typeparam name="R">The return type</typeparam>
        /// <param name="dialog">The dialog to show</param>
        /// <param name="owner">The owner window</param>
        /// <returns>The result</returns>
        public static async Task<R> ShowDialogAsync<V, R>(this IDialogBaseControl<V, R> dialog, object owner = null)
            where V : UserInputViewModel
        {
            return await RCFWPF.DialogBaseManager.ShowDialogAsync(dialog, owner);
        }

        /// <summary>
        /// Shows the dialog and returns when the dialog closes with a result
        /// </summary>
        /// <typeparam name="D">The dialog base manager type</typeparam>
        /// <typeparam name="V">The view model type</typeparam>
        /// <typeparam name="R">The return type</typeparam>
        /// <param name="dialog">The dialog to show</param>
        /// <param name="owner">The owner window</param>
        /// <returns>The result</returns>
        public static async Task<R> ShowDialogAsync<D, V, R>(this IDialogBaseControl<V, R> dialog, object owner = null)
            where D : IDialogBaseManager, new()
            where V : UserInputViewModel
        {
            return await new D().ShowDialogAsync(dialog, owner);
        }
    }
}