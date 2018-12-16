﻿using RayCarrot.CarrotFramework;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace RayCarrot.WPF
{
    /// <summary>
    /// A base value converter that allows direct XAML usage
    /// </summary>
    /// <typeparam name="T">The type of this value converter</typeparam>
    public abstract class BaseValueConverter<T> : MarkupExtension, IValueConverter
        where T : class, new()
    {
        #region Private Members

        /// <summary>
        /// A single static instance of this value converter
        /// </summary>
        private static T _converter;

        #endregion

        #region Markup Extension Methods

        /// <summary>
        /// Provides a static instance of the value converter 
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        /// <returns></returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _converter ?? (_converter = new T());
        }

        #endregion

        #region Value Converter Methods

        /// <summary>
        /// The method to convert one type to another
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        /// <summary>
        /// The method to convert a value back to it's source type
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// A base value converter that allows direct XAML usage
    /// </summary>
    /// <typeparam name="TConverter">The type of this value converter</typeparam>
    /// <typeparam name="TValue1">The type of the first expected value</typeparam>
    /// <typeparam name="TValue2">The type of the second expected value</typeparam>
    public abstract class BaseValueConverter<TConverter, TValue1, TValue2> : BaseValueConverter<TConverter>
        where TConverter : class, new()
    {
        #region Value Converter Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TValue1 converterValue))
            {
                RCF.Logger.LogWarningSource($"The converter {typeof(TConverter).Name} returned null due to the value not being of the expected type {typeof(TValue1).FullName}");
                return DependencyProperty.UnsetValue;
            }

            return ConvertValue(converterValue, targetType, parameter, culture);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TValue2 converterValue))
            {
                RCF.Logger.LogWarningSource($"The converter {typeof(TConverter).Name} returned null due to the value not being of the expected type {typeof(TValue2).FullName}");
                return DependencyProperty.UnsetValue;
            }

            return ConvertValueBack(converterValue, targetType, parameter, culture);
        }

        #endregion

        #region Abstract Methods

        public abstract TValue2 ConvertValue(TValue1 value, Type targetType, object parameter, CultureInfo culture);

        public virtual TValue1 ConvertValueBack(TValue2 value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// A base value converter that allows direct XAML usage
    /// </summary>
    /// <typeparam name="TConverter">The type of this value converter</typeparam>
    /// <typeparam name="TValue1">The type of the first expected value</typeparam>
    /// <typeparam name="TValue2">The type of the second expected value</typeparam>
    /// <typeparam name="TParamater">The type of the expected parameter</typeparam>
    public abstract class BaseValueConverter<TConverter, TValue1, TValue2, TParamater> : BaseValueConverter<TConverter>
        where TConverter : class, new()
    {
        #region Value Converter Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TValue1 converterValue))
            {
                RCF.Logger.LogWarningSource($"The converter {typeof(TConverter).Name} returned null due to the value not being of the expected type {typeof(TValue1).FullName}");
                return DependencyProperty.UnsetValue;
            }

            if (!(parameter is TParamater parameterValue))
            {
                RCF.Logger.LogWarningSource($"The converter {typeof(TConverter).Name} returned null due to the parameter value not being of the expected type {typeof(TParamater).FullName}");
                return DependencyProperty.UnsetValue;
            }

            return ConvertValue(converterValue, targetType, parameterValue, culture);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TValue2 converterValue))
            {
                RCF.Logger.LogWarningSource($"The converter {typeof(TConverter).Name} returned null due to the value not being of the expected type {typeof(TValue2).FullName}");
                return DependencyProperty.UnsetValue;
            }

            if (!(parameter is TParamater parameterValue))
            {
                RCF.Logger.LogWarningSource($"The converter {typeof(TConverter).Name} returned null due to the parameter value not being of the expected type {typeof(TParamater).FullName}");
                return DependencyProperty.UnsetValue;
            }

            return ConvertValueBack(converterValue, targetType, parameterValue, culture);
        }

        #endregion

        #region Abstract Methods

        public abstract TValue2 ConvertValue(TValue1 value, Type targetType, TParamater parameter, CultureInfo culture);

        public virtual TValue1 ConvertValueBack(TValue2 value, Type targetType, TParamater parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}