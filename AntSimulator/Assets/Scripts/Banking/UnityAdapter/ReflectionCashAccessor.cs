using System;
using System.Reflection;
using UnityEngine;

namespace Banking.UnityAdapter
{
    public static class ReflectionCashAccessor
    {
        public static bool TryGetCash(Component target, string memberName, out long cash)
        {
            cash = 0;
            if (target == null || string.IsNullOrWhiteSpace(memberName))
            {
                return false;
            }

            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var field = type.GetField(memberName, flags);
            if (field != null)
            {
                return TryConvertToLong(field.GetValue(target), out cash);
            }

            var prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.CanRead)
            {
                return TryConvertToLong(prop.GetValue(target), out cash);
            }

            return false;
        }

        public static bool TrySetCash(Component target, string memberName, long newValue)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
            {
                return false;
            }

            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var field = type.GetField(memberName, flags);
            if (field != null && !field.IsInitOnly)
            {
                return TryAssignNumeric(field.FieldType, newValue, value => field.SetValue(target, value));
            }

            var prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.CanWrite)
            {
                return TryAssignNumeric(prop.PropertyType, newValue, value => prop.SetValue(target, value));
            }

            return false;
        }

        private static bool TryConvertToLong(object val, out long result)
        {
            result = 0;
            if (val == null)
            {
                return false;
            }

            try
            {
                switch (val)
                {
                    case long l:
                        result = l;
                        return true;
                    case int i:
                        result = i;
                        return true;
                    case short s:
                        result = s;
                        return true;
                    case byte b:
                        result = b;
                        return true;
                    case float f:
                        result = (long)f;
                        return true;
                    case double d:
                        result = (long)d;
                        return true;
                    case decimal m:
                        result = (long)m;
                        return true;
                    case string str when long.TryParse(str, out var parsed):
                        result = parsed;
                        return true;
                    default:
                        result = Convert.ToInt64(val);
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TryAssignNumeric(Type memberType, long newValue, Action<object> assign)
        {
            try
            {
                if (memberType == typeof(long))
                {
                    assign(newValue);
                    return true;
                }

                if (memberType == typeof(int))
                {
                    assign((int)newValue);
                    return true;
                }

                if (memberType == typeof(short))
                {
                    assign((short)newValue);
                    return true;
                }

                if (memberType == typeof(byte))
                {
                    assign((byte)newValue);
                    return true;
                }

                if (memberType == typeof(float))
                {
                    assign((float)newValue);
                    return true;
                }

                if (memberType == typeof(double))
                {
                    assign((double)newValue);
                    return true;
                }

                if (memberType == typeof(decimal))
                {
                    assign((decimal)newValue);
                    return true;
                }

                if (memberType == typeof(string))
                {
                    assign(newValue.ToString());
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
