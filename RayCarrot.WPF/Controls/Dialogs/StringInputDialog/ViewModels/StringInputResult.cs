﻿using RayCarrot.CarrotFramework;

namespace RayCarrot.WPF
{
    /// <summary>
    /// Result for a string input dialog
    /// </summary>
    public class StringInputResult : UserInputResult
    {
        /// <summary>
        /// The string input by the user
        /// </summary>
        public string StringInput { get; set; }
    }
}