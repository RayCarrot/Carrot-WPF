﻿using System;
using System.Globalization;
using RayCarrot.CarrotFramework;

namespace RayCarrot.WPF
{
    /// <summary>
    /// Converts a <see cref="String"/> to a <see cref="Boolean"/> which is true if the value is an existing file system path
    /// </summary>
    public class PathExistsConverter : BaseValueConverter<PathExistsConverter, string, bool>
    {
        public override bool ConvertValue(string value, Type targetType, object parameter, CultureInfo culture)
        {
            return new FileSystemPath(value).Exists;
        }
    }
}