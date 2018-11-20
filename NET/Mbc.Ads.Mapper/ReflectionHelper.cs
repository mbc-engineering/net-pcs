//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Mbc.Ads.Mapper
{
    internal static class ReflectionHelper
    {
        internal static MemberInfo FindProperty(LambdaExpression lambdaExpression)
        {
            Expression expressionToCheck = lambdaExpression;

            while (true)
            {
                switch (expressionToCheck.NodeType)
                {
                    case ExpressionType.Convert:
                        expressionToCheck = ((UnaryExpression)expressionToCheck).Operand;
                        break;
                    case ExpressionType.Lambda:
                        expressionToCheck = ((LambdaExpression)expressionToCheck).Body;
                        break;
                    case ExpressionType.MemberAccess:
                        var memberExpression = (MemberExpression)expressionToCheck;

                        if (memberExpression.Expression.NodeType != ExpressionType.Parameter &&
                            memberExpression.Expression.NodeType != ExpressionType.Convert)
                        {
                            throw new ArgumentException(
                                $"Expression '{lambdaExpression}' must resolve to top-level member and not any child object's properties. You can use ForPath, a custom resolver on the child type or the AfterMap option instead.",
                                nameof(lambdaExpression));
                        }

                        var member = memberExpression.Member;

                        return member;
                    default:
                        throw new AdsMapperException(
                            "Custom configuration for members is only supported for top-level individual members on a type.");
                }
            }
        }

        internal static Type GetSettableDataType(this MemberInfo memberInfo)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).PropertyType;
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).FieldType;
                default:
                    throw new ArgumentException("Must be a settable member (property or field).", nameof(memberInfo));
            }
        }

        internal static void SetValue(this MemberInfo memberInfo, object obj, object value)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Property:
                    ((PropertyInfo)memberInfo).SetValue(obj, value);
                    break;
                case MemberTypes.Field:
                    ((FieldInfo)memberInfo).SetValue(obj, value);
                    break;
                default:
                    throw new ArgumentException("Must be a settable member (property or field).", nameof(memberInfo));
            }
        }

        internal static object GetValue(this MemberInfo memberInfo, object obj)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).GetValue(obj);
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).GetValue(obj);
                default:
                    throw new ArgumentException("Must be a gettable member (property or field).", nameof(memberInfo));
            }
        }

        internal static Type GetElementType(this MemberInfo memberInfo)
        {
            var type = memberInfo.GetSettableDataType();
            if (type.IsArray)
            {
                type = type.GetElementType();
            }

            return type;
        }
    }
}
