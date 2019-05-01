// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Extensions.Internal
{
    internal class PropertyHelper
    {
        // Delegate type for a by-ref property getter
        private delegate TValue ByRefFunc<TDeclaringType, TValue>(ref TDeclaringType arg);

        private static readonly MethodInfo CallPropertyGetterOpenGenericMethod =
            typeof(PropertyHelper).GetTypeInfo().GetDeclaredMethod(nameof(CallPropertyGetter));

        private static readonly MethodInfo CallPropertyGetterByReferenceOpenGenericMethod =
            typeof(PropertyHelper).GetTypeInfo().GetDeclaredMethod(nameof(CallPropertyGetterByReference));

        private Func<object, object> _valueGetter;

        public PropertyHelper(PropertyInfo property)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
        }

        public PropertyInfo Property { get; }

        private Func<object, object> ValueGetter
        {
            get
            {
                if (_valueGetter == null)
                {
                    _valueGetter = MakeFastPropertyGetter(Property);
                }

                return _valueGetter;
            }
        }

        public object GetValue(object instance)
        {
            return ValueGetter(instance);
        }

        private static Func<object, object> MakeFastPropertyGetter(PropertyInfo propertyInfo)
        {
            Debug.Assert(propertyInfo != null);

            return MakeFastPropertyGetter(
                propertyInfo,
                CallPropertyGetterOpenGenericMethod,
                CallPropertyGetterByReferenceOpenGenericMethod);
        }

        private static Func<object, object> MakeFastPropertyGetter(
            PropertyInfo propertyInfo,
            MethodInfo propertyGetterWrapperMethod,
            MethodInfo propertyGetterByRefWrapperMethod)
        {
            Debug.Assert(propertyInfo != null);

            // Must be a generic method with a Func<,> parameter
            Debug.Assert(propertyGetterWrapperMethod != null);
            Debug.Assert(propertyGetterWrapperMethod.IsGenericMethodDefinition);
            Debug.Assert(propertyGetterWrapperMethod.GetParameters().Length == 2);

            // Must be a generic method with a ByRefFunc<,> parameter
            Debug.Assert(propertyGetterByRefWrapperMethod != null);
            Debug.Assert(propertyGetterByRefWrapperMethod.IsGenericMethodDefinition);
            Debug.Assert(propertyGetterByRefWrapperMethod.GetParameters().Length == 2);

            var getMethod = propertyInfo.GetMethod;
            Debug.Assert(getMethod != null);
            Debug.Assert(!getMethod.IsStatic);
            Debug.Assert(getMethod.GetParameters().Length == 0);

            // Instance methods in the CLR can be turned into static methods where the first parameter
            // is open over "target". This parameter is always passed by reference, so we have a code
            // path for value types and a code path for reference types.
            if (getMethod.DeclaringType.GetTypeInfo().IsValueType)
            {
                // Create a delegate (ref TDeclaringType) -> TValue
                return MakeFastPropertyGetter(
                    typeof(ByRefFunc<,>),
                    getMethod,
                    propertyGetterByRefWrapperMethod);
            }
            else
            {
                // Create a delegate TDeclaringType -> TValue
                return MakeFastPropertyGetter(
                    typeof(Func<,>),
                    getMethod,
                    propertyGetterWrapperMethod);
            }
        }

        private static Func<object, object> MakeFastPropertyGetter(
            Type openGenericDelegateType,
            MethodInfo propertyGetMethod,
            MethodInfo openGenericWrapperMethod)
        {
            var typeInput = propertyGetMethod.DeclaringType;
            var typeOutput = propertyGetMethod.ReturnType;

            var delegateType = openGenericDelegateType.MakeGenericType(typeInput, typeOutput);
            var propertyGetterDelegate = propertyGetMethod.CreateDelegate(delegateType);

            var wrapperDelegateMethod = openGenericWrapperMethod.MakeGenericMethod(typeInput, typeOutput);
            var accessorDelegate = wrapperDelegateMethod.CreateDelegate(
                typeof(Func<object, object>),
                propertyGetterDelegate);

            return (Func<object, object>)accessorDelegate;
        }

        // Called via reflection
        private static object CallPropertyGetter<TDeclaringType, TValue>(
            Func<TDeclaringType, TValue> getter,
            object target)
        {
            return getter((TDeclaringType)target);
        }

        // Called via reflection
        private static object CallPropertyGetterByReference<TDeclaringType, TValue>(
            ByRefFunc<TDeclaringType, TValue> getter,
            object target)
        {
            var unboxed = (TDeclaringType)target;
            return getter(ref unboxed);
        }
    }
}