﻿using System.Diagnostics;
using JetBrains.Annotations;

namespace GeneralLibrary.Base.Core;

[DebuggerStepThrough]
public static class Check
{
    [ContractAnnotation("value:null => halt")]
    public static T NotNull<T>(
        T value,
        [InvokerParameterName] [NotNull] string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static T NotNull<T>(
        T value,
        [InvokerParameterName] [NotNull] string parameterName,
        string message)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName, message);
        }

        return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static string NotNull(
        string value,
        [InvokerParameterName] [NotNull] string parameterName,
        int maxLength = int.MaxValue,
        int minLength = 0)
    {
        if (value == null)
        {
            throw new ArgumentException($"{parameterName} can not be null!", parameterName);
        }

        if (value.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
        }

        if (minLength > 0 && value.Length < minLength)
        {
            throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
        }

        return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static string NotNullOrWhiteSpace(
        string value,
        [InvokerParameterName] [NotNull] string parameterName,
        int maxLength = int.MaxValue,
        int minLength = 0)
    {
        if (value.IsNullOrWhiteSpace())
        {
            throw new ArgumentException($"{parameterName} can not be null, empty or white space!", parameterName);
        }

        if (value.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
        }

        if (minLength > 0 && value.Length < minLength)
        {
            throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
        }

        return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static string NotNullOrEmpty(
        string value,
        [InvokerParameterName] [NotNull] string parameterName,
        int maxLength = int.MaxValue,
        int minLength = 0)
    {
        if (value.IsNullOrEmpty())
        {
            throw new ArgumentException($"{parameterName} can not be null or empty!", parameterName);
        }

        if (value.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
        }

        if (minLength > 0 && value.Length < minLength)
        {
            throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
        }

        return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static ICollection<T> NotNullOrEmpty<T>(ICollection<T> value, [InvokerParameterName] [NotNull] string parameterName)
    {
        if (value.IsNullOrEmpty())
        {
            throw new ArgumentException(parameterName + " can not be null or empty!", parameterName);
        }

        return value;
    }

    [ContractAnnotation("type:null => halt")]
    public static Type AssignableTo<TBaseType>(
        Type type,
        [InvokerParameterName] [NotNull] string parameterName)
    {
        NotNull(type, parameterName);

        if (!type.IsAssignableTo<TBaseType>())
        {
            throw new ArgumentException($"{parameterName} (type of {type.AssemblyQualifiedName}) should be assignable to the {typeof(TBaseType).GetFullNameWithAssemblyName()}!");
        }

        return type;
    }

    public static string Length(
        [CanBeNull] string value,
        [InvokerParameterName] [NotNull] string parameterName,
        int maxLength,
        int minLength = 0)
    {
        if (minLength > 0)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(parameterName + " can not be null or empty!", parameterName);
            }

            if (value.Length < minLength)
            {
                throw new ArgumentException($"{parameterName} length must be equal to or bigger than {minLength}!", parameterName);
            }
        }

        if (value != null && value.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} length must be equal to or lower than {maxLength}!", parameterName);
        }

        return value;
    }

    public static short Positive(
        short value,
        [InvokerParameterName] [NotNull] string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }

        if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }

        return value;
    }

    public static int Positive(
        int value,
        [InvokerParameterName] [NotNull] string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }

        if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }

        return value;
    }

    public static long Positive(
        long value,
        [InvokerParameterName] [NotNull] string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }

        if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }

        return value;
    }

    public static float Positive(
        float value,
        [InvokerParameterName] [NotNull] string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }

        if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }

        return value;
    }

    public static double Positive(
        double value,
        [InvokerParameterName] [NotNull] string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }

        if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }

        return value;
    }

    public static decimal Positive(
        decimal value,
        [InvokerParameterName] [NotNull] string parameterName)
    {
        if (value == 0)
        {
            throw new ArgumentException($"{parameterName} is equal to zero");
        }

        if (value < 0)
        {
            throw new ArgumentException($"{parameterName} is less than zero");
        }

        return value;
    }

    public static short Range(
        short value,
        [InvokerParameterName] [NotNull] string parameterName,
        short minimumValue,
        short maximumValue = short.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        }

        return value;
    }

    public static int Range(
        int value,
        [InvokerParameterName] [NotNull] string parameterName,
        int minimumValue,
        int maximumValue = int.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        }

        return value;
    }

    public static long Range(
        long value,
        [InvokerParameterName] [NotNull] string parameterName,
        long minimumValue,
        long maximumValue = long.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        }

        return value;
    }


    public static float Range(
        float value,
        [InvokerParameterName] [NotNull] string parameterName,
        float minimumValue,
        float maximumValue = float.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        }

        return value;
    }


    public static double Range(
        double value,
        [InvokerParameterName] [NotNull] string parameterName,
        double minimumValue,
        double maximumValue = double.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        }

        return value;
    }


    public static decimal Range(
        decimal value,
        [InvokerParameterName] [NotNull] string parameterName,
        decimal minimumValue,
        decimal maximumValue = decimal.MaxValue)
    {
        if (value < minimumValue || value > maximumValue)
        {
            throw new ArgumentException($"{parameterName} is out of range min: {minimumValue} - max: {maximumValue}");
        }

        return value;
    }

    public static T NotDefaultOrNull<T>(
        T? value,
        [InvokerParameterName] [NotNull] string parameterName)
        where T : struct
    {
        if (value == null)
        {
            throw new ArgumentException($"{parameterName} is null!", parameterName);
        }

        if (value.Value.Equals(default(T)))
        {
            throw new ArgumentException($"{parameterName} has a default value!", parameterName);
        }

        return value.Value;
    }
}