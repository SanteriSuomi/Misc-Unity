using System;

namespace Spellplague.Utility
{
    /// <summary>
    /// Static factory class that can generate any objects with generic type and parameters.
    /// </summary>
    public static class GenericFactory
    {
        /// <summary>
        /// Return an object with no constructor parameters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static T1 Create<T1>() where T1 : class, new() 
            => Activator.CreateInstance(typeof(T1)) as T1;

        /// <summary>
        /// Return an object with params T[] constructor parameters.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static T1 Create<T1, T2>(params T2[] parameters) where T1 : class, new() 
            => Activator.CreateInstance(typeof(T1), parameters) as T1;
    }
}