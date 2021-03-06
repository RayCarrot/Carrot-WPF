﻿using System;
using System.Globalization;
using RayCarrot.IO;

namespace RayCarrot.WPF
{
    /// <summary>
    /// Converts a <see cref="FileSystemPath"/> to a <see cref="String"/>
    /// </summary>
    public class FileSystemPathToStringConverter : BaseValueConverter<FileSystemPathToStringConverter, FileSystemPath, string>
    {
        public override string ConvertValue(FileSystemPath value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public override FileSystemPath ConvertValueBack(string value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}