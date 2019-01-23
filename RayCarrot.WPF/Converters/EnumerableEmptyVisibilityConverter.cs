﻿using RayCarrot.CarrotFramework;
using System;
using System.Collections;
using System.Globalization;
using System.Windows;

namespace RayCarrot.WPF
{
    /// <summary>
    /// Converts a <see cref="IEnumerable"/> to a <see cref="Visibility"/> which is set to <see cref="Visibility.Visible"/> when the collection is not empty
    /// </summary>
    public class EnumerableEmptyVisibilityConverter : BaseValueConverter<EnumerableEmptyVisibilityConverter, IEnumerable, Visibility>
    {
        public override Visibility ConvertValue(IEnumerable value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Any() ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}