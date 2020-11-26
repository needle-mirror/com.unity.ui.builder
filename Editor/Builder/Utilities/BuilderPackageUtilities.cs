using System.IO;
using UnityEngine;
using UnityEditor;
using System;
#if UI_BUILDER_PACKAGE && UNITY_EDITOR
using System.Reflection;
using System.Linq;
#endif

namespace Unity.UI.Builder
{
    internal static class BuilderPackageUtilities
    {
        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object
        {
#if UI_BUILDER_PACKAGE
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return EditorGUIUtility.Load(path) as T;
#endif
        }

#if !UI_BUILDER_PACKAGE
        public static bool HasFlag(this Enum value, Enum flag)
        {
            if (flag == null)
                throw new ArgumentNullException("flag");
            if (value.GetTypeCode() != flag.GetTypeCode())
                throw new ArgumentException("Enum types don't match.");

            var enumType = value.GetTypeCode();

            if (enumType == TypeCode.SByte || enumType == TypeCode.Int16 || enumType == TypeCode.Int32 || enumType == TypeCode.Int64)
                return (Convert.ToInt64(value) & Convert.ToInt64(flag)) != 0;
            if (enumType == TypeCode.Byte || enumType == TypeCode.UInt16 || enumType == TypeCode.UInt32 || enumType == TypeCode.UInt64)
                return (Convert.ToUInt64(value) & Convert.ToUInt64(flag)) != 0;

            throw new Exception("Enum type not supported.");
        }
#endif
    }
}
