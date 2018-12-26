﻿using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using ExpressionType = System.Linq.Expressions.ExpressionType;
using static System.Linq.Expressions.Expression;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace MissingPieces.Reflection
{
    /// <summary>
    /// Provides typed access to class or value type metadata.
    /// </summary>
    public static class Type<T>
    {
        /// <summary>
        /// Gets reflected type.
        /// </summary>
        public static Type RuntimeType => typeof(T);

        /// <summary>
        /// Returns default value for this type.
        /// </summary>
        public static T Default => default;

        private static readonly System.Linq.Expressions.DefaultExpression DefaultExpression = Default(RuntimeType);

        /// <summary>
        /// Checks whether the specified value is default value.
        /// </summary>
        public static readonly Predicate<T> IsDefault;

        static Type()
        {
            IsDefault = RuntimeType.IsValueType ?
                new Predicate<int>(ValueTypes.IsDefault).Reinterpret<Predicate<T>>() :
                new Predicate<object>(input => input is null).ConvertDelegate<Predicate<T>>();
        }

        /// <summary>
        /// Determines whether an instance of a specified type can be assigned to an instance of the current type.
        /// </summary>
        /// <typeparam name="U">The type to compare with the current type.</typeparam>
        /// <returns>True, if instance of type <typeparamref name="U"/> can be assigned to type <typeparamref name="T"/>.</returns>
        public static bool IsAssignableFrom<U>() => RuntimeType.IsAssignableFrom(typeof(U));

        public static bool IsAssignableTo<U>() => Type<U>.IsAssignableFrom<T>();

        public static Optional<T> TryConvert<U>(U value)
        {
            UnaryOperator<U, T>.Invoker converter = Type<U>.UnaryOperator<T>.Get(Reflection.UnaryOperator.Convert);
            return converter is null ? Optional<T>.Empty : converter(value);
        }

        public static bool TryConvert<U>(U value, out T result) => TryConvert<U>(value).TryGet(out result);

        /// <summary>
        /// Converts object into type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// Semantics of this method includes typecast as well as conversion between numeric types
        /// and implicit/explicit cast operators.
        /// </remarks>
        /// <param name="value">The value to convert.</param>
        /// <typeparam name="U">Type of value to convert.</typeparam>
        /// <returns>Converted value.</returns>
        /// <exception cref="InvalidCastException">Cannot convert values.</exception>
        public static T Convert<U>(U value) => TryConvert<U>(value).OrThrow<InvalidCastException>();

        /// <summary>
        /// Reflects constructor as function.
        /// </summary>
        /// <param name="nonPublic">True to reflect non-public constructor.</param>
        /// <typeparam name="A">A structure describing constructor signature.</typeparam>
        /// <returns>Constructor for type <typeparamref name="T"/>; or null, if it doesn't exist.</returns>
        public static Reflection.Constructor<Function<A, T>> GetConstructor<A>(bool nonPublic = false)
            where A: struct
            => Constructor.Get<Function<A, T>>(nonPublic);

        /// <summary>
        /// Provides access to constructor of type <typeparamref name="T"/> without parameters.
        /// </summary>
        [DefaultMember("Invoke")]
        public static class Constructor
        {
            private static class Public<D>
                where D : MulticastDelegate
            {
                internal static readonly Reflection.Constructor<D> Value = Reflection.Constructor<D>.Reflect(false)?.OfType<T>();
            }

            private static class NonPublic<D>
                where D : MulticastDelegate
            {
                internal static readonly Reflection.Constructor<D> Value = Reflection.Constructor<D>.Reflect(true)?.OfType<T>();
            }

            /// <summary>
            /// Reflects constructor of type <typeparamref name="T"/> which signature
            /// is specified by delegate type.
            /// </summary>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <typeparam name="D">Type of delegate describing constructor signature.</typeparam>
            /// <returns>Reflected constructor; or null, if constructor doesn't exist.</returns>
            public static Reflection.Constructor<D> Get<D>(bool nonPublic = false)
                where D : MulticastDelegate
                => nonPublic ? NonPublic<D>.Value : Public<D>.Value;

            /// <summary>
            /// Returns public constructor of type <typeparamref name="T"/> without parameters.
            /// </summary>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Reflected constructor without parameters; or null, if it doesn't exist.</returns>
            public static Reflection.Constructor<Func<T>> Get(bool nonPublic = false)
                => Get<Func<T>>(nonPublic);

            /// <summary>
            /// Returns public constructor of type <typeparamref name="T"/> without parameters.
            /// </summary>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Reflected constructor without parameters.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static Reflection.Constructor<Func<T>> Require(bool nonPublic = false)
                => Get(nonPublic) ?? throw MissingConstructorException.Create<T>();

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/> if constructor exists.</returns>
            public static Optional<T> TryInvoke(bool nonPublic = false)
            {
                Func<T> ctor = Get(nonPublic);
                return ctor is null ? Optional<T>.Empty : ctor();
            }

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static T Invoke(bool nonPublic = false) => Require(nonPublic).Invoke();
        }

        /// <summary>
		/// Provides access to constructor of type <typeparamref name="T"/> with single parameter.
		/// </summary>
        /// <typeparam name="P">Type of constructor parameter.</typeparam>
        [DefaultMember("Invoke")]
        public static class Constructor<P>
        {
            /// <summary>
			/// Returns constructor <typeparamref name="T"/> with single parameter of type <typeparamref name="P"/>.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with single parameter; or null, if it doesn't exist.</returns>
			public static Reflection.Constructor<Func<P, T>> Get(bool nonPublic = false)
                => Constructor.Get<Func<P, T>>(nonPublic);

            /// <summary>
            /// Returns constructor <typeparamref name="T"/> with single parameter of type <typeparamref name="P"/>.
            /// </summary>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Reflected constructor with single parameter.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static Reflection.Constructor<Func<P, T>> Require(bool nonPublic = false)
                => Get(nonPublic) ?? throw MissingConstructorException.Create<T, P>();

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg">Constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/> if constructor exists.</returns>
            public static Optional<T> TryInvoke(P arg, bool nonPublic = false)
            {
                Func<P, T> ctor = Get(nonPublic);
                return ctor is null ? Optional<T>.Empty : ctor(arg);
            }

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg">Constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static T Invoke(P arg, bool nonPublic = false) => Require(nonPublic).Invoke(arg);
        }

        /// <summary>
        /// Provides access to constructor of type <typeparamref name="T"/> with two parameters.
        /// </summary>
        /// <typeparam name="P1">Type of first constructor parameter.</typeparam>
		/// <typeparam name="P2">Type of second constructor parameter.</typeparam>
        [DefaultMember("Invoke")] 
        public static class Constructor<P1, P2>
        {
            /// <summary>
			/// Returns constructor <typeparamref name="T"/> with two 
			/// parameters of type <typeparamref name="P1"/> and <typeparamref name="P2"/>.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with two parameters; or null, if it doesn't exist.</returns>
			public static Reflection.Constructor<Func<P1, P2, T>> Get(bool nonPublic = false)
				=> Constructor.Get<Func<P1, P2, T>>(nonPublic);

			/// <summary>
			/// Returns constructor <typeparamref name="T"/> with two 
			/// parameters of type <typeparamref name="P1"/> and <typeparamref name="P2"/>.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with two parameters.</returns>
			/// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
			public static Reflection.Constructor<Func<P1, P2, T>> Require(bool nonPublic = false)
				=> Get(nonPublic) ?? throw MissingConstructorException.Create<T, P1, P2>();

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/> if constructor exists.</returns>
            public static Optional<T> TryInvoke(P1 arg1, P2 arg2, bool nonPublic = false)
            {
                Func<P1, P2, T> ctor = Get(nonPublic);
                return ctor is null ? Optional<T>.Empty : ctor(arg1, arg2);
            }

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static T Invoke(P1 arg1, P2 arg2, bool nonPublic = false) => Require(nonPublic).Invoke(arg1, arg2);
        }

        /// <summary>
        /// Provides access to constructor of type <typeparamref name="T"/> with three parameters.
        /// </summary>
        /// <typeparam name="P1">Type of first constructor parameter.</typeparam>
		/// <typeparam name="P2">Type of second constructor parameter.</typeparam>
		/// <typeparam name="P3">Type of third constructor parameter.</typeparam>
        [DefaultMember("Invoke")]
        public static class Constructor<P1, P2, P3>
        {
            /// <summary>
			/// Returns constructor <typeparamref name="T"/> with three 
			/// parameters of type <typeparamref name="P1"/>, <typeparamref name="P2"/> and <typeparamref name="P3"/>.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with three parameters; or null, if it doesn't exist.</returns>
			public static Reflection.Constructor<Func<P1, P2, P3, T>> Get(bool nonPublic = false)
				=> Constructor.Get<Func<P1, P2, P3, T>>(nonPublic);
			
			/// <summary>
			/// Returns constructor <typeparamref name="T"/> with three 
			/// parameters of type <typeparamref name="P1"/>, <typeparamref name="P2"/> and <typeparamref name="P3"/>.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with three parameters.</returns>
			/// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
			public static Reflection.Constructor<Func<P1, P2, P3, T>> Require(bool nonPublic = false)
				=> Get(nonPublic) ?? throw MissingConstructorException.Create<T, P1, P2, P3>();

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/> if constructor exists.</returns>
            public static Optional<T> TryInvoke(P1 arg1, P2 arg2, P3 arg3, bool nonPublic = false)
            {
                Func<P1, P2, P3, T> ctor = Get(nonPublic);
                return ctor is null ? Optional<T>.Empty : ctor(arg1, arg2, arg3);
            }

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static T Invoke(P1 arg1, P2 arg2, P3 arg3, bool nonPublic = false) => Require(nonPublic).Invoke(arg1, arg2, arg3);            
        }

        /// <summary>
        /// Provides access to constructor of type <typeparamref name="T"/> with four parameters.
        /// </summary>
        /// <typeparam name="P1">Type of first constructor parameter.</typeparam>
		/// <typeparam name="P2">Type of second constructor parameter.</typeparam>
		/// <typeparam name="P3">Type of third constructor parameter.</typeparam>
        /// <typeparam name="P4">Type of fourth constructor parameter.</typeparam>      
        [DefaultMember("Invoke")]
        public static class Constructor<P1, P2, P3, P4>
        {
            /// <summary>
			/// Returns constructor <typeparamref name="T"/> with four 
			/// parameters of type <typeparamref name="P1"/>, <typeparamref name="P2"/>, <typeparamref name="P3"/> and <typeparamref name="P4"/>.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with four parameters; or null, if it doesn't exist.</returns>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, T>> Get(bool nonPublic = false)
				=> Constructor.Get<Func<P1, P2, P3, P4, T>>(nonPublic);
			
			/// <summary>
			/// Returns constructor <typeparamref name="T"/> with four 
			/// parameters of type <typeparamref name="P1"/>, <typeparamref name="P2"/>, <typeparamref name="P3"/> and <typeparamref name="P4"/>.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with four parameters.</returns>
			/// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, T>> Require(bool nonPublic = false)
				=> Get(nonPublic) ?? throw MissingConstructorException.Create<T, P1, P2, P3, P4>();    

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/> if constructor exists.</returns>
            public static Optional<T> TryInvoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, bool nonPublic = false)
            {
                Func<P1, P2, P3, P4, T> ctor = Get(nonPublic);
                return ctor is null ? Optional<T>.Empty : ctor(arg1, arg2, arg3, arg4);
            }

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static T Invoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, bool nonPublic = false) => Require(nonPublic).Invoke(arg1, arg2, arg3, arg4);        
        }

        /// <summary>
        /// Provides access to constructor of type <typeparamref name="T"/> with five parameters.
        /// </summary>
        /// <typeparam name="P1">Type of first constructor parameter.</typeparam>
		/// <typeparam name="P2">Type of second constructor parameter.</typeparam>
		/// <typeparam name="P3">Type of third constructor parameter.</typeparam>
        /// <typeparam name="P4">Type of fourth constructor parameter.</typeparam>
        /// <typeparam name="P5">Type of fifth constructor parameter.</typeparam>             
        [DefaultMember("Invoke")]
        public static class Constructor<P1, P2, P3, P4, P5>
        {
            /// <summary>
			/// Returns constructor <typeparamref name="T"/> with five 
			/// parameters of type <typeparamref name="P1"/>, <typeparamref name="P2"/>, 
			/// <typeparamref name="P3"/>, <typeparamref name="P4"/> and <typeparamref name="P5"/>.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with five parameters; or null, if it doesn't exist.</returns>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, T>> Get(bool nonPublic = false)
				=> Constructor.Get<Func<P1, P2, P3, P4, P5, T>>(nonPublic);
			
			/// <summary>
			/// Returns constructor <typeparamref name="T"/> with five 
			/// parameters of type <typeparamref name="P1"/>, <typeparamref name="P2"/>, 
			/// <typeparamref name="P3"/>, <typeparamref name="P4"/> and <typeparamref name="P5"/>.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with five parameters.</returns>
			/// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, T>> Require(bool nonPublic = false)
				=> Get(nonPublic) ?? throw MissingConstructorException.Create<T, P1, P2, P3, P4, P5>();   

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/> if constructor exists.</returns>
            public static Optional<T> TryInvoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, bool nonPublic = false)
            {
                Func<P1, P2, P3, P4, P5, T> ctor = Get(nonPublic);
                return ctor is null ? Optional<T>.Empty : ctor(arg1, arg2, arg3, arg4, arg5);
            }

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static T Invoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, bool nonPublic = false) 
                => Require(nonPublic).Invoke(arg1, arg2, arg3, arg4, arg5);         
        }

        /// <summary>
        /// Provides access to constructor of type <typeparamref name="T"/> with six parameters.
        /// </summary>
        /// <typeparam name="P1">Type of first constructor parameter.</typeparam>
		/// <typeparam name="P2">Type of second constructor parameter.</typeparam>
		/// <typeparam name="P3">Type of third constructor parameter.</typeparam>
        /// <typeparam name="P4">Type of fourth constructor parameter.</typeparam>
        /// <typeparam name="P5">Type of fifth constructor parameter.</typeparam> 
        /// <typeparam name="P6">Type of sixth constructor parameter.</typeparam>              
        [DefaultMember("Invoke")]
        public static class Constructor<P1, P2, P3, P4, P5, P6>
        {
            /// <summary>
			/// Returns constructor <typeparamref name="T"/> with six parameters.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with six parameters; or null, if it doesn't exist.</returns>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, P6, T>> Get(bool nonPublic = false)
				=> Constructor.Get<Func<P1, P2, P3, P4, P5, P6, T>>(nonPublic);
			
			/// <summary>
			/// Returns constructor <typeparamref name="T"/> with six parameters.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with six parameters.</returns>
			/// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, P6, T>> Require(bool nonPublic = false)
				=> Get(nonPublic) ?? throw MissingConstructorException.Create<T, P1, P2, P3, P4, P5, P6>();

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="arg6">Sixth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/> if constructor exists.</returns>
            public static Optional<T> TryInvoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, P6 arg6, bool nonPublic = false)
            {
                Func<P1, P2, P3, P4, P5, P6, T> ctor = Get(nonPublic);
                return ctor is null ? Optional<T>.Empty : ctor(arg1, arg2, arg3, arg4, arg5, arg6);
            }

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="arg6">Sixth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static T Invoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, P6 arg6, bool nonPublic = false) 
                => Require(nonPublic).Invoke(arg1, arg2, arg3, arg4, arg5, arg6);      
        }

        /// <summary>
        /// Provides access to constructor of type <typeparamref name="T"/> with seven parameters.
        /// </summary>
        /// <typeparam name="P1">Type of first constructor parameter.</typeparam>
		/// <typeparam name="P2">Type of second constructor parameter.</typeparam>
		/// <typeparam name="P3">Type of third constructor parameter.</typeparam>
        /// <typeparam name="P4">Type of fourth constructor parameter.</typeparam>
        /// <typeparam name="P5">Type of fifth constructor parameter.</typeparam> 
        /// <typeparam name="P6">Type of sixth constructor parameter.</typeparam>      
        /// <typeparam name="P7">Type of sixth constructor parameter.</typeparam>         
        [DefaultMember("Invoke")]
        public static class Constructor<P1, P2, P3, P4, P5, P6, P7>
        {
            /// <summary>
			/// Returns constructor <typeparamref name="T"/> with seven parameters.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with seven parameters; or null, if it doesn't exist.</returns>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, P6, P7, T>> Get(bool nonPublic = false)
				=> Constructor.Get<Func<P1, P2, P3, P4, P5, P6, P7, T>>(nonPublic);
			
			/// <summary>
			/// Returns constructor <typeparamref name="T"/> with seven parameters.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with seven parameters.</returns>
			/// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, P6, P7, T>> Require(bool nonPublic = false)
				=> Get(nonPublic) ?? throw MissingConstructorException.Create<T, P1, P2, P3, P4, P5, P6, P7>();   

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="arg6">Sixth constructor argument.</param>
            /// <param name="arg7">Seventh constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/> if constructor exists.</returns>
            public static Optional<T> TryInvoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, P6 arg6, P7 arg7, bool nonPublic = false)
            {
                Func<P1, P2, P3, P4, P5, P6, P7, T> ctor = Get(nonPublic);
                return ctor is null ? Optional<T>.Empty : ctor(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="arg6">Sixth constructor argument.</param>
            /// <param name="arg7">Seventh constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static T Invoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, P6 arg6, P7 arg7, bool nonPublic = false) 
                => Require(nonPublic).Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7);         
        }

        /// <summary>
        /// Provides access to constructor of type <typeparamref name="T"/> with eight parameters.
        /// </summary>
        /// <typeparam name="P1">Type of first constructor parameter.</typeparam>
		/// <typeparam name="P2">Type of second constructor parameter.</typeparam>
		/// <typeparam name="P3">Type of third constructor parameter.</typeparam>
        /// <typeparam name="P4">Type of fourth constructor parameter.</typeparam>
        /// <typeparam name="P5">Type of fifth constructor parameter.</typeparam> 
        /// <typeparam name="P6">Type of sixth constructor parameter.</typeparam>      
        /// <typeparam name="P7">Type of sixth constructor parameter.</typeparam>  
        /// <typeparam name="P8">Type of eighth constructor parameter.</typeparam>        
        [DefaultMember("Invoke")]
        public static class Constructor<P1, P2, P3, P4, P5, P6, P7, P8>
        {
            /// <summary>
			/// Returns constructor <typeparamref name="T"/> with eight parameters.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with eight parameters; or null, if it doesn't exist.</returns>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, P6, P7, P8, T>> Get(bool nonPublic = false)
				=> Constructor.Get<Func<P1, P2, P3, P4, P5, P6, P7, P8, T>>(nonPublic);
			
			/// <summary>
			/// Returns constructor <typeparamref name="T"/> with eight parameters.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with eight parameters.</returns>
			/// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, P6, P7, P8, T>> Require(bool nonPublic = false)
				=> Get(nonPublic) ?? throw MissingConstructorException.Create<T, P1, P2, P3, P4, P5, P6, P7, P8>();   

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="arg6">Sixth constructor argument.</param>
            /// <param name="arg7">Seventh constructor argument.</param>
            /// <param name="arg8">Eighth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/> if constructor exists.</returns>
            public static Optional<T> TryInvoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, P6 arg6, P7 arg7, P8 arg8, bool nonPublic = false)
            {
                Func<P1, P2, P3, P4, P5, P6, P7, P8, T> ctor = Get(nonPublic);
                return ctor is null ? Optional<T>.Empty : ctor(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="arg6">Sixth constructor argument.</param>
            /// <param name="arg7">Seventh constructor argument.</param>
            /// <param name="arg8">Eighth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static T Invoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, P6 arg6, P7 arg7, P8 arg8, bool nonPublic = false) 
                => Require(nonPublic).Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);         
        }

        /// <summary>
        /// Provides access to constructor of type <typeparamref name="T"/> with nine parameters.
        /// </summary>
        /// <typeparam name="P1">Type of first constructor parameter.</typeparam>
		/// <typeparam name="P2">Type of second constructor parameter.</typeparam>
		/// <typeparam name="P3">Type of third constructor parameter.</typeparam>
        /// <typeparam name="P4">Type of fourth constructor parameter.</typeparam>
        /// <typeparam name="P5">Type of fifth constructor parameter.</typeparam> 
        /// <typeparam name="P6">Type of sixth constructor parameter.</typeparam>      
        /// <typeparam name="P7">Type of sixth constructor parameter.</typeparam>  
        /// <typeparam name="P8">Type of eighth constructor parameter.</typeparam>  
        /// <typeparam name="P9">Type of ninth constructor parameter.</typeparam>       
        [DefaultMember("Invoke")]
        public static class Constructor<P1, P2, P3, P4, P5, P6, P7, P8, P9>
        {
            /// <summary>
			/// Returns constructor <typeparamref name="T"/> with nine parameters.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with nine parameters; or null, if it doesn't exist.</returns>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, T>> Get(bool nonPublic = false)
				=> Constructor.Get<Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, T>>(nonPublic);
			
			/// <summary>
			/// Returns constructor <typeparamref name="T"/> with nine parameters.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with nine parameters.</returns>
			/// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, T>> Require(bool nonPublic = false)
				=> Get(nonPublic) ?? throw MissingConstructorException.Create<T, P1, P2, P3, P4, P5, P6, P7, P8, P9>();   

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="arg6">Sixth constructor argument.</param>
            /// <param name="arg7">Seventh constructor argument.</param>
            /// <param name="arg8">Eighth constructor argument.</param>
            /// <param name="arg9">Ninth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/> if constructor exists.</returns>
            public static Optional<T> TryInvoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, P6 arg6, P7 arg7, P8 arg8, P9 arg9, bool nonPublic = false)
            {
                Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, T> ctor = Get(nonPublic);
                return ctor is null ? Optional<T>.Empty : ctor(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="arg6">Sixth constructor argument.</param>
            /// <param name="arg7">Seventh constructor argument.</param>
            /// <param name="arg8">Eighth constructor argument.</param>
            /// <param name="arg9">Ninth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static T Invoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, P6 arg6, P7 arg7, P8 arg8, P9 arg9, bool nonPublic = false) 
                => Require(nonPublic).Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);         
        }

        /// <summary>
        /// Provides access to constructor of type <typeparamref name="T"/> with nine parameters.
        /// </summary>
        /// <typeparam name="P1">Type of first constructor parameter.</typeparam>
		/// <typeparam name="P2">Type of second constructor parameter.</typeparam>
		/// <typeparam name="P3">Type of third constructor parameter.</typeparam>
        /// <typeparam name="P4">Type of fourth constructor parameter.</typeparam>
        /// <typeparam name="P5">Type of fifth constructor parameter.</typeparam> 
        /// <typeparam name="P6">Type of sixth constructor parameter.</typeparam>      
        /// <typeparam name="P7">Type of sixth constructor parameter.</typeparam>  
        /// <typeparam name="P8">Type of eighth constructor parameter.</typeparam>  
        /// <typeparam name="P9">Type of ninth constructor parameter.</typeparam>  
        /// <typeparam name="P10">Type of tenth constructor parameter.</typeparam>      
        [DefaultMember("Invoke")]
        public static class Constructor<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>
        {
            /// <summary>
			/// Returns constructor <typeparamref name="T"/> with ten parameters.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with ten parameters; or null, if it doesn't exist.</returns>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, T>> Get(bool nonPublic = false)
				=> Constructor.Get<Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, T>>(nonPublic);
			
			/// <summary>
			/// Returns constructor <typeparamref name="T"/> with ten parameters.
			/// </summary>
			/// <param name="nonPublic">True to reflect non-public constructor.</param>
			/// <returns>Reflected constructor with ten parameters.</returns>
			/// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
			public static Reflection.Constructor<Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, T>> Require(bool nonPublic = false)
				=> Get(nonPublic) ?? throw MissingConstructorException.Create<T, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10>(); 

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="arg6">Sixth constructor argument.</param>
            /// <param name="arg7">Seventh constructor argument.</param>
            /// <param name="arg8">Eighth constructor argument.</param>
            /// <param name="arg9">Ninth constructor argument.</param>
            /// <param name="arg10">Tenth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/> if constructor exists.</returns>
            public static Optional<T> TryInvoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, P6 arg6, P7 arg7, P8 arg8, P9 arg9, P10 arg10, bool nonPublic = false)
            {
                Func<P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, T> ctor = Get(nonPublic);
                return ctor is null ? Optional<T>.Empty : ctor(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }

            /// <summary>
            /// Invokes constructor.
            /// </summary>
            /// <param name="arg1">First constructor argument.</param>
            /// <param name="arg2">Second constructor argument.</param>
            /// <param name="arg3">Third constructor argument.</param>
            /// <param name="arg4">Fourth constructor argument.</param>
            /// <param name="arg5">Fifth constructor argument.</param>
            /// <param name="arg6">Sixth constructor argument.</param>
            /// <param name="arg7">Seventh constructor argument.</param>
            /// <param name="arg8">Eighth constructor argument.</param>
            /// <param name="arg9">Ninth constructor argument.</param>
            /// <param name="arg10">Tenth constructor argument.</param>
            /// <param name="nonPublic">True to reflect non-public constructor.</param>
            /// <returns>Instance of <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingConstructorException">Constructor doesn't exist.</exception>
            public static T Invoke(P1 arg1, P2 arg2, P3 arg3, P4 arg4, P5 arg5, P6 arg6, P7 arg7, P8 arg8, P9 arg9, P10 arg10, bool nonPublic = false) 
                => Require(nonPublic).Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);           
        }

        /// <summary>
        /// Provides typed access to static declared in type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="P">Type of property.</typeparam>
        public sealed class StaticProperty<P> : Property<P>, IProperty<P>
        {
            private sealed class Cache : MemberCache<PropertyInfo, StaticProperty<P>>
            {
                private readonly BindingFlags flags;

                internal Cache(BindingFlags flags) => this.flags = flags;

                private protected override StaticProperty<P> Create(string propertyName)
                {
                    var property = RuntimeType.GetProperty(propertyName, flags);
                    return property == null ? null : new StaticProperty<P>(property, flags.HasFlag(BindingFlags.NonPublic));
                }
            }

            private static readonly Cache Public = new Cache(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            private static readonly Cache NonPublic = new Cache(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            private readonly MemberAccess<P> accessor;
            private StaticProperty(PropertyInfo property, bool nonPublic)
                : base(property)
            {
                var valueParam = Parameter(property.PropertyType.MakeByRefType());
                var actionParam = Parameter(typeof(MemberAction));

                var getter = property.GetGetMethod(nonPublic);
                var setter = property.GetSetMethod(nonPublic);

                if (getter is null) //write-only
                    accessor = Lambda<MemberAccess<P>>(MemberAccess.GetOrSetValue(actionParam, null, Call(null, setter, valueParam)),
                        valueParam,
                        actionParam).Compile();
                else if (setter is null) //read-only
                    accessor = Lambda<MemberAccess<P>>(MemberAccess.GetOrSetValue(actionParam, Assign(valueParam, Call(null, getter)), null),
                        valueParam,
                        actionParam).Compile();
                else //read-write
                    accessor = Lambda<MemberAccess<P>>(MemberAccess.GetOrSetValue(actionParam, Assign(valueParam, Call(null, getter)), Call(null, setter, valueParam)),
                        valueParam,
                        actionParam).Compile();
            }

            // public new Method<MemberAccess.Getter<P>> GetGetMethod(bool nonPublic)
            // {
            //     var getter = base.GetGetMethod(nonPublic);
            //     return getter == null ? null : StaticMethod<MemberAccess.Getter<P>>.Get(getter.Name, nonPublic);
            // }

            // public new Method<MemberAccess.Getter<P>> GetMethod
            // {
            //     get
            //     {
            //         var getter = base.GetMethod;
            //         return getter == null ? null : StaticMethod<MemberAccess.Getter<P>>.Get(getter.Name, !getter.IsPublic);
            //     }
            // }
            // public new Method<MemberAccess.Setter<P>> SetMethod
            // {
            //     get
            //     {
            //         var setter = base.SetMethod;
            //         return setter == null ? null : StaticMethod<MemberAccess.Setter<P>>.Get(setter.Name, !setter.IsPublic);
            //     }
            // }

            // public new Method<MemberAccess.Setter<P>> GetSetMethod(bool nonPublic)
            // {
            //     var setter = base.GetSetMethod(nonPublic);
            //     return setter == null ? null : StaticMethod<MemberAccess.Setter<P>>.Get(setter.Name, nonPublic);
            // }

            /// <summary>
            /// Gets or sets property value.
            /// </summary>
            public P Value
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => accessor.GetValue();
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => accessor.SetValue(value);
            }

            public static implicit operator MemberAccess<P>(StaticProperty<P> property) => property?.accessor;

            /// <summary>
            /// Gets static property.
            /// </summary>
            /// <param name="propertyName">Name of property.</param>
            /// <param name="nonPublic">True to reflect non-public property.</param>
            /// <returns>Static property; or null, if property doesn't exist.</returns>
            public static StaticProperty<P> Get(string propertyName, bool nonPublic = false)
                => (nonPublic ? NonPublic : Public).GetOrCreate(propertyName);

            /// <summary>
            /// Gets static property.
            /// </summary>
            /// <param name="propertyName">Name of property.</param>
            /// <param name="nonPublic">True to reflect non-public property.</param>
            /// <typeparam name="E">Type of exception to throw if property doesn't exist.</typeparam>
            /// <returns>Static property.</returns>
            public static StaticProperty<P> GetOrThrow<E>(string propertyName, bool nonPublic = false)
                where E : Exception, new()
                => Get(propertyName, nonPublic) ?? throw new E();

            /// <summary>
            /// Gets static property.
            /// </summary>
            /// <param name="propertyName">Name of property.</param>
            /// <param name="exceptionFactory">A factory used to produce exception.</param>
            /// <param name="nonPublic">True to reflect non-public property.</param>
            /// <typeparam name="E">Type of exception to throw if property doesn't exist.</typeparam>
            /// <returns>Static property.</returns>
            public static StaticProperty<P> GetOrThrow<E>(string propertyName, Func<string, E> exceptionFactory, bool nonPublic = false)
                where E : Exception
                => Get(propertyName, nonPublic) ?? throw exceptionFactory(propertyName);

            /// <summary>
            /// Gets static property.
            /// </summary>
            /// <param name="propertyName">Name of property.</param>
            /// <param name="nonPublic">True to reflect non-public property.</param>
            /// <returns>Static property.</returns>
            /// <exception cref="MissingPropertyException">Property doesn't exist.</exception>
            public static StaticProperty<P> GetOrThrow(string propertyName, bool nonPublic = false)
                => GetOrThrow(propertyName, MissingPropertyException.Create<T, P>, nonPublic);
        }

        /// <summary>
        /// Provides typed access to instance property declared in type <typeparamref name="T"/>.
        /// </summary>
		/// <typeparam name="P">Type of property.</typeparam>
        public sealed class InstanceProperty<P> : Property<P>, IProperty<T, P>
        {
            private sealed class Cache : MemberCache<PropertyInfo, InstanceProperty<P>>
            {
                private readonly BindingFlags flags;

                internal Cache(BindingFlags flags) => this.flags = flags;

                private protected override InstanceProperty<P> Create(string propertyName)
                {
                    var property = RuntimeType.GetProperty(propertyName, flags);
                    return property == null ? null : new InstanceProperty<P>(property, flags.HasFlag(BindingFlags.NonPublic));
                }
            }

            private static readonly Cache Public = new Cache(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            private static readonly Cache NonPublic = new Cache(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            private readonly MemberAccess<T, P> accessor;

            private InstanceProperty(PropertyInfo property, bool nonPublic)
                : base(property)
            {
                var instanceParam = Parameter(RuntimeType.MakeByRefType());
                var valueParam = Parameter(property.PropertyType.MakeByRefType());
                var actionParam = Parameter(typeof(MemberAction));

                var getter = property.GetGetMethod(nonPublic);
                var setter = property.GetSetMethod(nonPublic);

                if (getter is null) //write-only
                    accessor = Lambda<MemberAccess<T, P>>(MemberAccess.GetOrSetValue(actionParam, null, Call(instanceParam, setter, valueParam)),
                        instanceParam,
                        valueParam,
                        actionParam).Compile();
                else if (setter is null) //read-only
                    accessor = Lambda<MemberAccess<T, P>>(MemberAccess.GetOrSetValue(actionParam, Assign(valueParam, Call(instanceParam, getter)), null),
                    instanceParam,
                        valueParam,
                        actionParam).Compile();
                else //read-write
                    accessor = Lambda<MemberAccess<T, P>>(MemberAccess.GetOrSetValue(actionParam, Assign(valueParam, Call(instanceParam, getter)), Call(instanceParam, setter, valueParam)),
                        instanceParam,
                        valueParam,
                        actionParam).Compile();
            }

            // public new Method<MemberAccess.Getter<T, P>> GetGetMethod(bool nonPublic)
            // {
            //     var getter = base.GetGetMethod(nonPublic);
            //     return getter == null ? null : InstanceMethod<MemberAccess.Getter<T, P>>.Get(getter.Name, nonPublic);
            // }

            // public new Method<MemberAccess.Getter<T, P>> GetMethod
            // {
            //     get
            //     {
            //         var getter = base.GetMethod;
            //         return getter == null ? null : InstanceMethod<MemberAccess.Getter<T, P>>.Get(getter.Name, !getter.IsPublic);
            //     }
            // }
            // public new Method<MemberAccess.Setter<T, P>> SetMethod
            // {
            //     get
            //     {
            //         var setter = base.SetMethod;
            //         return setter == null ? null : InstanceMethod<MemberAccess.Setter<T, P>>.Get(setter.Name, !setter.IsPublic);
            //     }
            // }

            // public new Method<MemberAccess.Setter<T, P>> GetSetMethod(bool nonPublic)
            // {
            //     var setter = base.GetSetMethod(nonPublic);
            //     return setter == null ? null : InstanceMethod<MemberAccess.Setter<T, P>>.Get(setter.Name, nonPublic);
            // }

            public static implicit operator MemberAccess<T, P>(InstanceProperty<P> property) => property?.accessor;

            /// <summary>
            /// Gets or sets property value.
            /// </summary>
            /// <param name="owner">Property instance.</param>
            /// <returns>Property value.</returns>
            public P this[in T owner]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => accessor.GetValue(in owner);
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => accessor.SetValue(in owner, value);
            }

            /// <summary>
            /// Gets instance property.
            /// </summary>
            /// <param name="propertyName">Name of property.</param>
            /// <param name="nonPublic">True to reflect non-public property.</param>
            /// <returns>Static instance; or null, if property doesn't exist.</returns>
            public static InstanceProperty<P> Get(string propertyName, bool nonPublic = false)
                => (nonPublic ? NonPublic : Public).GetOrCreate(propertyName);

            /// <summary>
            /// Gets instance property.
            /// </summary>
            /// <param name="propertyName">Name of property.</param>
            /// <param name="nonPublic">True to reflect non-public property.</param>
            /// <typeparam name="E">Type of exception to throw if property doesn't exist.</typeparam>
            /// <returns>Instance property.</returns>
            public static InstanceProperty<P> GetOrThrow<E>(string propertyName, bool nonPublic = false)
                where E : Exception, new()
                => Get(propertyName, nonPublic) ?? throw new E();

            /// <summary>
            /// Gets instance property.
            /// </summary>
            /// <param name="propertyName">Name of property.</param>
            /// <param name="exceptionFactory">A factory used to produce exception.</param>
            /// <param name="nonPublic">True to reflect non-public property.</param>
            /// <typeparam name="E">Type of exception to throw if property doesn't exist.</typeparam>
            /// <returns>Instance property.</returns>
            public static InstanceProperty<P> GetOrThrow<E>(string propertyName, Func<string, E> exceptionFactory, bool nonPublic = false)
                where E : Exception
                => Get(propertyName, nonPublic) ?? throw exceptionFactory(propertyName);

            /// <summary>
            /// Gets instance property.
            /// </summary>
            /// <param name="propertyName">Name of property.</param>
            /// <param name="nonPublic">True to reflect non-public property.</param>
            /// <returns>Static property.</returns>
            /// <exception cref="MissingPropertyException">Property doesn't exist.</exception>
            public static InstanceProperty<P> GetOrThrow(string propertyName, bool nonPublic = false)
                => GetOrThrow(propertyName, MissingPropertyException.Create<T, P>, nonPublic);
        }

        /// <summary>
        /// Provides typed access to instance event declared in type <typeparamref name="T"/>.
        /// </summary>
		/// <typeparam name="H">Type of event handler.</typeparam>
        public static class Event<H>
            where H : MulticastDelegate
        {
            private sealed class InstanceEvents : MemberCache<EventInfo, Reflection.Event<T, H>>
            {
                internal static readonly InstanceEvents Public = new InstanceEvents(false);
                internal static readonly InstanceEvents NonPublic = new InstanceEvents(true);

                private readonly bool nonPublic;
                private InstanceEvents(bool nonPublic) => this.nonPublic = nonPublic;

                private protected override Reflection.Event<T, H> Create(string eventName) 
                    => Reflection.Event<T, H>.Reflect(eventName, nonPublic);
            }

            private sealed class StaticEvents : MemberCache<EventInfo, Reflection.Event<H>>
            {
                internal static readonly StaticEvents Public = new StaticEvents(false);
                internal static readonly StaticEvents NonPublic = new StaticEvents(true);
                private readonly bool nonPublic;
                private StaticEvents(bool nonPublic) => this.nonPublic = nonPublic;

                private protected override Reflection.Event<H> Create(string eventName) 
                    => Reflection.Event<H>.Reflect<T>(eventName, nonPublic);
            }

            /// <summary>
            /// Gets instane event.
            /// </summary>
            /// <param name="eventName">Name of event.</param>
            /// <param name="nonPublic">True to reflect non-public event.</param>
            /// <returns>Instance event; or null, if event doesn't exist.</returns>
            public static Reflection.Event<T, H> Get(string eventName, bool nonPublic = false)
                => (nonPublic ? InstanceEvents.NonPublic : InstanceEvents.Public).GetOrCreate(eventName);

            /// <summary>
            /// Gets instance event.
            /// </summary>
            /// <param name="eventName">Name of event.</param>
            /// <param name="nonPublic">True to reflect non-public event.</param>
            /// <returns>Instance event.</returns>
            /// <exception cref="MissingEventException">Event doesn't exist.</exception>
            public static Reflection.Event<T, H> Require(string eventName, bool nonPublic = false)
                => Get(eventName, nonPublic) ?? throw MissingEventException.Create<T, H>(eventName);

            /// <summary>
            /// Gets static event.
            /// </summary>
            /// <param name="eventName">Name of event.</param>
            /// <param name="nonPublic">True to reflect non-public event.</param>
            /// <returns>Static event; or null, if event doesn't exist.</returns>
            public static Reflection.Event<H> GetStatic(string eventName, bool nonPublic = false)
                => (nonPublic ? StaticEvents.NonPublic : StaticEvents.Public).GetOrCreate(eventName);

            /// <summary>
            /// Gets static event.
            /// </summary>
            /// <param name="eventName">Name of event.</param>
            /// <param name="nonPublic">True to reflect non-public event.</param>
            /// <returns>Static event.</returns>
            /// <exception cref="MissingEventException">Event doesn't exist.</exception>
            public static Reflection.Event<H> RequireStatic(string eventName, bool nonPublic = false)
                => GetStatic(eventName, nonPublic) ?? throw MissingEventException.Create<T, H>(eventName);
        }

        /// <summary>
        /// Provides typed access to the type attribute.
        /// </summary>
        /// <typeparam name="A">Type of attribute.</typeparam>
        public static class Attribute<A>
            where A : Attribute
        {
            /// <summary>
            /// Returns attribute associated with the type <typeparamref name="T"/>.
            /// </summary>
            /// <param name="inherit">True to find inherited attribute.</param>
            /// <param name="condition">Optional predicate to check attribute properties.</param>
            /// <returns>Attribute associated with type <typeparamref name="T"/>; or null, if attribute doesn't exist.</returns>
            public static A Get(bool inherit = false, Predicate<A> condition = null)
            {
                var attr = RuntimeType.GetCustomAttribute<A>(inherit);
                return attr is null || condition is null || condition(attr) ? attr : null;
            }

            /// <summary>
            /// Returns attribute associated with the type <typeparamref name="T"/>.
            /// </summary>
            /// <param name="inherit">True to find inherited attribute.</param>
            /// <param name="condition">Optional predicate to check attribute properties.</param>
            /// <returns>Attribute associated with type <typeparamref name="T"/>.</returns>
            /// <exception cref="MissingAttributeException">Event doesn't exist.</exception>
            public static A Require(bool inherit = false, Predicate<A> condition = null)
                => Get(inherit, condition) ?? throw MissingAttributeException.Create<T, A>();

            /// <summary>
            /// Get all custom attributes of type <typeparamref name="A"/>.
            /// </summary>
            /// <param name="inherit">True to find inherited attribute.</param>
            /// <param name="condition">Optional predicate to check attribute properties.</param>
            /// <returns>All attributes associated with type <typeparamref name="T"/>.</returns>
            public static IEnumerable<A> GetAll(bool inherit = false, Predicate<A> condition = null)
                => from attr in RuntimeType.GetCustomAttributes<A>(inherit)
                   where condition is null || condition(attr)
                   select attr;
        }

        /// <summary>
        /// Provides typed access to instance field declared in type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="F">Type of field value.</typeparam>
        public sealed class InstanceField<F> : Reflection.Field<F>, IField<T, F>
        {
            private sealed class PublicCache : MemberCache<FieldInfo, InstanceField<F>>
            {
                private protected override InstanceField<F> Create(string eventName)
                {
                    var field = RuntimeType.GetField(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    return field is null || field.FieldType != typeof(F) ?
                        null :
                        new InstanceField<F>(field);
                }
            }

            private sealed class NonPublicCache : MemberCache<FieldInfo, InstanceField<F>>
            {
                private protected override InstanceField<F> Create(string eventName)
                {
                    var field = RuntimeType.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    return field is null || field.FieldType != typeof(F) ?
                        null :
                        new InstanceField<F>(field);
                }
            }

            private static readonly MemberCache<FieldInfo, InstanceField<F>> Public = new PublicCache();
            private static readonly MemberCache<FieldInfo, InstanceField<F>> NonPublic = new NonPublicCache();

            private readonly MemberAccess<T, F> accessor;

            private InstanceField(FieldInfo field)
                : base(field)
            {
                var instanceParam = Parameter(field.DeclaringType.MakeArrayType());
                var valueParam = Parameter(field.FieldType.MakeByRefType());
                var actionParam = Parameter(typeof(MemberAction));
                if (field.Attributes.HasFlag(FieldAttributes.InitOnly))
                    accessor = Lambda<MemberAccess<T, F>>(MemberAccess.GetOrSetValue(actionParam, Assign(valueParam, Field(instanceParam, field)), null),
                        instanceParam,
                        valueParam,
                        actionParam).Compile();
                else
                    accessor = Lambda<MemberAccess<T, F>>(MemberAccess.GetOrSetValue(actionParam, Assign(valueParam, Field(instanceParam, field)), Assign(Field(instanceParam, field), valueParam)),
                        instanceParam,
                        valueParam,
                        actionParam).Compile();
            }

            public static implicit operator MemberAccess<T, F>(InstanceField<F> field) => field?.accessor;

            public F this[in T instance]
            {
                get => accessor.GetValue(in instance);
                set => accessor.SetValue(in instance, value);
            }

            /// <summary>
            /// Gets instane field.
            /// </summary>
            /// <param name="fieldName">Name of field.</param>
            /// <param name="nonPublic">True to reflect non-public field.</param>
            /// <returns>Instance field; or null, if field doesn't exist.</returns>
            public static InstanceField<F> Get(string fieldName, bool nonPublic = false)
                => (nonPublic ? NonPublic : Public).GetOrCreate(fieldName);

            /// <summary>
            /// Gets instance field.
            /// </summary>
            /// <param name="fieldName">Name of field.</param>
            /// <param name="nonPublic">True to reflect non-public field.</param>
            /// <typeparam name="E">Type of exception to throw if field doesn't exist.</typeparam>
            /// <returns>Instance field.</returns>
            public static InstanceField<F> GetOrThrow<E>(string fieldName, bool nonPublic = false)
                where E : Exception, new()
                => Get(fieldName, nonPublic) ?? throw new E();

            /// <summary>
            /// Gets instance field.
            /// </summary>
            /// <param name="fieldName">Name of field.</param>
            /// <param name="exceptionFactory">A factory used to produce exception.</param>
            /// <param name="nonPublic">True to reflect non-public field.</param>
            /// <typeparam name="E">Type of exception to throw if field doesn't exist.</typeparam>
            /// <returns>Instance field.</returns>
            public static InstanceField<F> GetOrThrow<E>(string fieldName, Func<string, E> exceptionFactory, bool nonPublic = false)
                where E : Exception
                => Get(fieldName, nonPublic) ?? throw exceptionFactory(fieldName);

            /// <summary>
            /// Gets instance field.
            /// </summary>
            /// <param name="fieldName">Name of field.</param>
            /// <param name="nonPublic">True to reflect non-public field.</param>
            /// <returns>Instance field.</returns>
            /// <exception cref="MissingEventException">Field doesn't exist.</exception>
            public static InstanceField<F> GetOrThrow(string fieldName, bool nonPublic = false)
                => GetOrThrow(fieldName, MissingFieldException.Create<T, F>, nonPublic);
        }

        /// <summary>
        /// Provides typed access to static field declared in type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="F">Type of field value.</typeparam>
        public sealed class StaticField<F> : Reflection.Field<F>, IField<F>
        {
            private sealed class PublicCache : MemberCache<FieldInfo, StaticField<F>>
            {
                private protected override StaticField<F> Create(string eventName)
                {
                    var field = RuntimeType.GetField(eventName, BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    return field is null || field.FieldType != typeof(F) ?
                        null :
                        new StaticField<F>(field);
                }
            }

            private sealed class NonPublicCache : MemberCache<FieldInfo, StaticField<F>>
            {
                private protected override StaticField<F> Create(string eventName)
                {
                    var field = RuntimeType.GetField(eventName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    return field is null || field.FieldType != typeof(F) ?
                        null :
                        new StaticField<F>(field);
                }
            }

            private static readonly MemberCache<FieldInfo, StaticField<F>> Public = new PublicCache();
            private static readonly MemberCache<FieldInfo, StaticField<F>> NonPublic = new NonPublicCache();

            private readonly MemberAccess<F> accessor;

            private StaticField(FieldInfo field)
                : base(field)
            {
                var valueParam = Parameter(field.FieldType.MakeByRefType());
                var actionParam = Parameter(typeof(MemberAction));
                if (field.Attributes.HasFlag(FieldAttributes.InitOnly))
                    accessor = Lambda<MemberAccess<F>>(MemberAccess.GetOrSetValue(actionParam, Assign(valueParam, Field(null, field)), null),
                        valueParam,
                        actionParam).Compile();
                else
                    accessor = Lambda<MemberAccess<F>>(MemberAccess.GetOrSetValue(actionParam, Assign(valueParam, Field(null, field)), Assign(Field(null, field), valueParam)),
                        valueParam,
                        actionParam).Compile();
            }

            /// <summary>
            /// Gets or sets field value.
            /// </summary>
            public F Value
            {
                get => accessor.GetValue();
                set => accessor.SetValue(value);
            }

            public static implicit operator MemberAccess<F>(StaticField<F> field) => field?.accessor;

            /// <summary>
            /// Gets static field.
            /// </summary>
            /// <param name="fieldName">Name of field.</param>
            /// <param name="nonPublic">True to reflect non-public field.</param>
            /// <returns>Static field; or null, if field doesn't exist.</returns>
            public static StaticField<F> Get(string fieldName, bool nonPublic = false)
                => (nonPublic ? NonPublic : Public).GetOrCreate(fieldName);

            /// <summary>
            /// Gets static field.
            /// </summary>
            /// <param name="fieldName">Name of field.</param>
            /// <param name="nonPublic">True to reflect non-public field.</param>
            /// <typeparam name="E">Type of exception to throw if field doesn't exist.</typeparam>
            /// <returns>Static field.</returns>
            public static StaticField<F> GetOrThrow<E>(string fieldName, bool nonPublic = false)
                where E : Exception, new()
                => Get(fieldName, nonPublic) ?? throw new E();

            /// <summary>
            /// Gets static field.
            /// </summary>
            /// <param name="fieldName">Name of field.</param>
            /// <param name="exceptionFactory">A factory used to produce exception.</param>
            /// <param name="nonPublic">True to reflect non-public field.</param>
            /// <typeparam name="E">Type of exception to throw if field doesn't exist.</typeparam>
            /// <returns>Static field.</returns>
            public static StaticField<F> GetOrThrow<E>(string fieldName, Func<string, E> exceptionFactory, bool nonPublic = false)
                where E : Exception
                => Get(fieldName, nonPublic) ?? throw exceptionFactory(fieldName);

            /// <summary>
            /// Gets static field.
            /// </summary>
            /// <param name="fieldName">Name of field.</param>
            /// <param name="nonPublic">True to reflect non-public field.</param>
            /// <returns>Static field.</returns>
            /// <exception cref="MissingEventException">Field doesn't exist.</exception>
            public static StaticField<F> GetOrThrow(string fieldName, bool nonPublic = false)
                => GetOrThrow(fieldName, MissingFieldException.Create<T, F>, nonPublic);
        }

        /// <summary>
        /// Provides access to methods declared in type <typeparamref name="T"/>.
        /// </summary>
        public static class Method
        {
            private sealed class InstanceMethods<D> : MemberCache<MethodInfo, Reflection.Method<D>>
                where D: Delegate
            {
                internal static readonly InstanceMethods<D> Public = new InstanceMethods<D>(false);
                internal static readonly InstanceMethods<D> NonPublic = new InstanceMethods<D>(true);

                private readonly bool nonPublic;
                private InstanceMethods(bool nonPublic) => this.nonPublic = nonPublic;

                private protected override Reflection.Method<D> Create(string methodName) 
                    => Reflection.Method<D>.Reflect(methodName, nonPublic)?.OfType<T>();
            }

            private sealed class StaticMethods<D> : MemberCache<MethodInfo, Reflection.Method<D>>
                where D: Delegate
            {
                internal static readonly StaticMethods<D> Public = new StaticMethods<D>(false);
                internal static readonly StaticMethods<D> NonPublic = new StaticMethods<D>(true);
                private readonly bool nonPublic;
                private StaticMethods(bool nonPublic) => this.nonPublic = nonPublic;

                private protected override Reflection.Method<D> Create(string eventName) 
                    => Reflection.Method<D>.Reflect<T>(eventName, nonPublic);
            }

            public static Reflection.Method<D> Get<D>(string methodName, bool nonPublic = false)
                where D: Delegate
                => (nonPublic ? InstanceMethods<D>.NonPublic : InstanceMethods<D>.Public).GetOrCreate(methodName);

            public static Reflection.Method<D> GetStatic<D>(string methodName, bool nonPublic = false)
                where D: Delegate
                => (nonPublic ? StaticMethods<D>.NonPublic : StaticMethods<D>.Public).GetOrCreate(methodName);
        }
        
        /// <summary>
        /// Provides access to methods with single parameter declared in type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="P">Type of method parameter.</typeparam>
        public static class Method<P>
        {
            public static Reflection.Method<Action<T, P>> Get(string methodName, bool nonPublic = false)
                => Method.Get<Action<T, P>>(methodName, nonPublic);

            public static Reflection.Method<Action<T, P>> Require(string methodName, bool nonPublic = false)
                => Get(methodName, nonPublic) ?? throw MissingMethodException.CreateAction<T, P>(methodName);

            public static Reflection.Method<Action<P>> GetStatic(string methodName, bool nonPublic = false)
                => Method.GetStatic<Action<P>>(methodName, nonPublic);

            public static Reflection.Method<Action<P>> RequireStatic(string methodName, bool nonPublic = false)
                => GetStatic(methodName, nonPublic) ?? throw MissingMethodException.CreateAction<T, P>(methodName);
            
            public static Reflection.Method<Func<T, P, R>> Get<R>(string methodName, bool nonPublic = false)
                => Method.Get<Func<T, P, R>>(methodName, nonPublic);

            public static Reflection.Method<Func<T, P, R>> Require<R>(string methodName, bool nonPublic = false)
                => Get<R>(methodName, nonPublic) ?? throw MissingMethodException.CreateAction<T, P>(methodName);

            public static Reflection.Method<Func<P, R>> GetStatic<R>(string methodName, bool nonPublic = false)
                => Method.GetStatic<Func<P, R>>(methodName, nonPublic);

            public static Reflection.Method<Func<P, R>> RequireStatic<R>(string methodName, bool nonPublic = false)
                => GetStatic<R>(methodName, nonPublic) ?? throw MissingMethodException.CreateAction<T, P>(methodName);
        }

        /// <summary>
        /// Represents unary operator applicable to type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="R">Type of unary operator result.</typeparam>
        public static class UnaryOperator<R>
        {
            private sealed class Operators : Cache<UnaryOperator, UnaryOperator<T, R>>
            {
                private static readonly Cache<UnaryOperator, UnaryOperator<T, R>> Instance = new Operators();
                private Operators()
                {
                }

                private protected override UnaryOperator<T, R> Create(UnaryOperator @operator) => UnaryOperator<T, R>.Reflect(@operator);

                internal static new UnaryOperator<T, R> GetOrCreate(UnaryOperator @operator) => Instance.GetOrCreate(@operator);
            }

            /// <summary>
            /// Gets unary operator. 
            /// </summary>
            /// <param name="op">Unary operator type.</param>
            /// <returns>Unary operator.</returns>
            public static UnaryOperator<T, R> Get(UnaryOperator op) => Operators.GetOrCreate(op);

            public static UnaryOperator<T, R> Require(UnaryOperator op) => Get(op) ?? throw MissingOperatorException.Create<T>(op);
        }
    }
}