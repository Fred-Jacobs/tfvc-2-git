using System;

namespace Tfvc2Git.Core.Extensions
{
    internal static class Guard
    {
        internal static void IsTrue(bool value, string message)
        {
            if (!value)
                throw new ArgumentException(message);
        }

        internal static void IsFalse(bool value, string message)
        {
            if (value)
                throw new ArgumentException(message);
        }

        internal static void IsNotNull(object value, string message)
        {
            if (null == value)
                throw new ArgumentNullException(message);
        }
    }
}