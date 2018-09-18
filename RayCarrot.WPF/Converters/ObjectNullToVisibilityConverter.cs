﻿using System;
using System.Globalization;
using System.Windows;

namespace RayCarrot.WPF
{
    /// <summary>
    /// Converts a <see cref="Object"/> to a <see cref="Visibility"/> which is set to <see cref="Visibility.Visible"/> when the value is null
    /// </summary>
    public class ObjectNullToVisibilityConverter : BaseValueConverter<IsNotNullConverter, object, Visibility>
    {
        public override Visibility ConvertValue(object value, Type targetType, object parameter, CultureInfo culture) => value == null ? Visibility.Collapsed : Visibility.Visible;
    }
}