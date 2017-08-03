using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Fastnet.Core.Extensions
{
    public static class extensions
    {
        internal static T GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
        {
            var typeInfo = enumVal.GetType().GetTypeInfo();
            var v = typeInfo.DeclaredMembers.First(x => x.Name == enumVal.ToString());
            return v.GetCustomAttribute<T>();
        }
        public static T Set<T>(this Enum value, T flag)
        {
            return (T)(object)((int)(object)value | (int)(object)flag);
        }
        public static T Unset<T>(this Enum value, T flag)
        {
            return (T)(object)((int)(object)value & ~(int)(object)flag);
        }
        /// <summary>
        /// Gets the text of the description attribute on an enum value
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="val">Enum value</param>
        /// <returns></returns>
        public static string ToDescription<T>(this T val) where T : struct
        {
            if (typeof(T).GetTypeInfo().IsEnum)
            {
                var list = (DescriptionAttribute[])typeof(T).GetTypeInfo().GetField(val.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
                return list?.Length > 0 ? list.First().Description : val.ToString();
            }
            throw new ArgumentException("Not an enum type");
        }
        public static T To<T>(this string text) where T : struct
        {
            if (typeof(T).GetTypeInfo().IsEnum)
            {
                var values = Enum.GetNames(typeof(T));
                if (values.Contains(text, StringComparer.CurrentCultureIgnoreCase)) //.InvariantCultureIgnoreCase))
                {
                    return (T)Enum.Parse(typeof(T), text);
                }
                else
                {
                    foreach (var s in values)
                    {
                        var list = (DescriptionAttribute[])typeof(T).GetTypeInfo().GetField(s).GetCustomAttributes(typeof(DescriptionAttribute), false);
                        if (list.Length > 0)
                        {
                            if (string.Compare(list[0].Description, text, true) == 0)
                            {
                                return (T)Enum.Parse(typeof(T), s);
                            }
                        }
                    }
                }
                throw new ArgumentOutOfRangeException($"{text} not valid");
            }
            throw new ArgumentException("Not an enum type");
        }
        public static string ToRoman(this int number)
        {
            if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("Value must be between 1 and 3999");
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900); //EDIT: i've typed 400 instead 900
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            throw new ArgumentOutOfRangeException("Value must be between 1 and 3999");
        }
        public static string ToDefault(this DateTime d)
        {
            return d.ToString("ddMMMyyyy");
        }
        public static DateTime RoundUp(this DateTime dt, int m)
        {
            var od = dt;
            DateTime rd;
            if (m > 60)
            {
                od = dt.AddMinutes(-dt.Minute).AddSeconds(-dt.Second);
                var h = (int)Math.Round((double)m / 60);
                var ts = TimeSpan.FromHours(od.Hour);
                var target = TimeSpan.FromHours(h * Math.Ceiling((ts.TotalHours + 0.5) / h));
                var x = target.TotalHours - ts.TotalHours;
                rd = od.AddHours(x);
            }
            else
            {
                int minutes = Math.Min(60, m);
                var ts = TimeSpan.FromMinutes(od.Minute);
                //var target = ts.RoundUp(minutes);
                var target = TimeSpan.FromMinutes(minutes * Math.Ceiling((ts.TotalMinutes + 0.5) / minutes));
                var x = target.TotalMinutes - ts.TotalMinutes;
                rd = od.AddMinutes(x).AddSeconds(-od.Second);
            }
            return rd;
        }
        public static void LogError(this ILogger logger, Exception xe)
        {
            logger.LogError($"{xe.Message}\n{xe.StackTrace}");
            if (xe.InnerException != null)
            {
                logger.LogError(xe.InnerException);
            }
        }
    }
}
