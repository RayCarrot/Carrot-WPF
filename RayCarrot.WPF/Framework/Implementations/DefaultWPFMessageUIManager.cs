﻿using RayCarrot.CarrotFramework;
using RayCarrot.CarrotFramework.UI;
using System.Runtime.CompilerServices;
using System.Windows;

namespace RayCarrot.WPF
{
    /// <summary>
    /// The default <see cref="IMessageUIManager"/> for WPF
    /// </summary>
    public class DefaultWPFMessageUIManager : IMessageUIManager
    {
        /// <summary>
        /// Indicates if the UI requests should be logged
        /// </summary>
        public virtual bool LogRequests { get; set; }

        /// <summary>
        /// Displays a message to the user
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="header">The header for the message</param>
        /// <param name="messageType">The type of message, determining its visual appearance</param>
        /// <param name="allowCancel">True if the option to cancel is present</param>
        /// <returns>True if the user accepted the message, otherwise false</returns>
        public virtual bool DisplayMessage(string message, string header, MessageType messageType, bool allowCancel, [CallerMemberName]string origin = "", [CallerFilePath]string filePath = "", [CallerLineNumber]int lineNumber = 0)
        {
            if (LogRequests)
                RCF.Logger.LogTraceSource($"A message was displayed with the content of: {message}", origin: origin, filePath: filePath, lineNumber: lineNumber);

            MessageBoxImage image = MessageBoxImage.None;

            switch (messageType)
            {
                case MessageType.Generic:
                    image = MessageBoxImage.None;
                    break;

                case MessageType.Information:
                    image = MessageBoxImage.Information;
                    break;

                case MessageType.Error:
                    image = MessageBoxImage.Error;
                    break;

                case MessageType.Warning:
                    image = MessageBoxImage.Warning;
                    break;

                case MessageType.Success:
                    image = MessageBoxImage.Information;
                    break;

                case MessageType.Question:
                    image = MessageBoxImage.Question;
                    break;
            }

            var result = MessageBox.Show(message, header, allowCancel ? MessageBoxButton.OKCancel : MessageBoxButton.OK, image);

            return !allowCancel ? true : result == MessageBoxResult.OK;
        }
    }
}