using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Utility class to manage the Game View size
    /// </summary>
    /// <remarks>
    /// This class is used to set the Game View size to a custom size, get the current Game View size,
    /// add a custom size to the list of available sizes, and select a Game View size.
    /// It also provides methods to backup and restore the Game View size.
    /// </remarks>
    [InitializeOnLoad]
    public static class GameViewSize
    {
        static object s_InitialSizeObj;

        const int k_MiscSize = 1; // Used when no main GameView exists (ex: batchmode)

        static bool s_Initialized;
        static Type s_GameViewType;
        static Type s_GameViewSizesType;
        static Type s_GameViewSizeObjType;
        static Type s_GameViewSizeTypeEnum;

        static MethodInfo s_GetMainPlayModeView;

        // Resolved against the runtime type of the game view instance (e.g. GameView),
        // not the base PlayModeView type, because these members are defined on subclasses.
        static Type s_GameViewInstanceType;
        static PropertyInfo s_TargetSizeProp;
        static MethodInfo s_SizeSelectionCallbackMethod;
        static PropertyInfo s_CurrentGameViewSizeProp;

        static PropertyInfo s_SizesInstanceProp;
        static PropertyInfo s_CurrentGroupProp;

        static Type s_GroupType;
        static FieldInfo s_CustomField;
        static FieldInfo s_BuiltinField;
        static MethodInfo s_IndexOfMethod;
        static MethodInfo s_GetBuiltinCountMethod;
        static MethodInfo s_AddCustomSizeMethod;
        static MethodInfo s_BuiltinContainsMethod;
        static MethodInfo s_CustomGetEnumeratorMethod;

        static FieldInfo s_SizeWidthField;
        static FieldInfo s_SizeHeightField;
        static FieldInfo s_SizeBaseTextField;
        static ConstructorInfo s_SizeCtor;

        // Called eagerly from the static constructor ([InitializeOnLoad]) and again
        // on first public-method use. Safe despite running early: all reflection lookups
        // use null-conditional propagation, so missing types/members yield null without
        // throwing. Public methods re-check via LogErrorIfMissing before accessing any member.
        static void EnsureInitialized()
        {
            if (s_Initialized)
                return;
            s_Initialized = true;

            s_GameViewType = Type.GetType("UnityEditor.PlayModeView,UnityEditor");
            s_GameViewSizesType = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            s_GameViewSizeObjType = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            s_GameViewSizeTypeEnum = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");

            s_GetMainPlayModeView =
                s_GameViewType?.GetMethod("GetMainPlayModeView", BindingFlags.NonPublic | BindingFlags.Static);

            s_SizesInstanceProp =
                s_GameViewSizesType?.BaseType?.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            s_CurrentGroupProp =
                s_GameViewSizesType?.GetProperty("currentGroup", BindingFlags.Public | BindingFlags.Instance);

            s_GroupType = s_CurrentGroupProp?.PropertyType;
            s_CustomField =
                s_GroupType?.GetField("m_Custom", BindingFlags.NonPublic | BindingFlags.Instance);
            s_BuiltinField =
                s_GroupType?.GetField("m_Builtin", BindingFlags.NonPublic | BindingFlags.Instance);
            s_IndexOfMethod =
                s_GroupType?.GetMethod("IndexOf", BindingFlags.Public | BindingFlags.Instance);
            s_GetBuiltinCountMethod =
                s_GroupType?.GetMethod("GetBuiltinCount");
            s_AddCustomSizeMethod =
                s_GroupType?.GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.Instance);
            s_BuiltinContainsMethod =
                s_BuiltinField?.FieldType?.GetMethod("Contains");
            s_CustomGetEnumeratorMethod =
                s_CustomField?.FieldType?.GetMethod("GetEnumerator");

            s_SizeWidthField =
                s_GameViewSizeObjType?.GetField("m_Width", BindingFlags.NonPublic | BindingFlags.Instance);
            s_SizeHeightField =
                s_GameViewSizeObjType?.GetField("m_Height", BindingFlags.NonPublic | BindingFlags.Instance);
            s_SizeBaseTextField =
                s_GameViewSizeObjType?.GetField("m_BaseText", BindingFlags.NonPublic | BindingFlags.Instance);
            s_SizeCtor =
                s_GameViewSizeObjType?.GetConstructor(new[] { s_GameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) });
        }

        static GameViewSize()
        {
            EnsureInitialized();
        }

        static readonly HashSet<string> k_ErrorMembers = new();

        static bool LogErrorIfMissing(object member, string name)
        {
            if (member != null)
                return true;

            if (k_ErrorMembers.Add(name))
            {
                GraphicsTestLogger.Log(
                    LogType.Error,
                    $"GameViewSize reflection failed: '{name}' could not be resolved. " +
                    "The internal Unity Editor API may have changed. " +
                    "Game View size operations will use fallback values."
                );
            }

            return false;
        }

        static void EnsureInstanceMembersResolved(EditorWindow gameView)
        {
            var runtimeType = gameView.GetType();
            if (s_GameViewInstanceType == runtimeType)
                return;
            s_GameViewInstanceType = runtimeType;
            s_TargetSizeProp =
                runtimeType.GetProperty("targetSize", BindingFlags.NonPublic | BindingFlags.Instance);
            s_SizeSelectionCallbackMethod =
                runtimeType.GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.Instance);
            s_CurrentGameViewSizeProp =
                runtimeType.GetProperty("currentGameViewSize", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        static EditorWindow GetMainGameView()
        {
            EnsureInitialized();
            if (s_GetMainPlayModeView == null)
            {
                GraphicsTestLogger.Log(
                    LogType.Error,
                    $"Can't find the main Game View : GetMainPlayModeView function was not found in {s_GameViewType} type ! Did API change ?"
                );
                return null;
            }
            var res = s_GetMainPlayModeView.Invoke(null, null);
            return (EditorWindow)res;
        }

        /// <summary>
        /// Sets the Game View size to a custom size
        /// </summary>
        /// <param name="width">The width to set the Game View to</param>
        /// <param name="height">The height to set the Game View to</param>
        public static void SetGameViewSize(int width, int height)
        {
            var size = SetCustomSize(width, height);
            SelectSize(size);
        }

        /// <summary>
        /// Get the Game View size
        /// </summary>
        /// <param name="width">The width of the Game View</param>
        /// <param name="height">The height of the Game View</param>
        /// <remarks>
        /// This method retrieves the size of the Game View by accessing the targetSize property
        /// of the Game View class.
        /// </remarks>
        public static void GetGameRenderSize(out int width, out int height)
        {
            var gameView = GetMainGameView();

            if (gameView == null)
            {
                width = height = k_MiscSize;
                return;
            }

            EnsureInstanceMembersResolved(gameView);
            if (!LogErrorIfMissing(s_TargetSizeProp, "targetSize"))
            {
                width = height = k_MiscSize;
                return;
            }
            var size = (Vector2)s_TargetSizeProp.GetValue(gameView, new object[] { });
            width = (int)size.x;
            height = (int)size.y;
        }

        static object Group()
        {
            EnsureInitialized();
            if (!LogErrorIfMissing(s_SizesInstanceProp, "GameViewSizes.instance") ||
                !LogErrorIfMissing(s_CurrentGroupProp, "GameViewSizes.currentGroup"))
                return null;
            var instance = s_SizesInstanceProp.GetValue(null, new object[] { });
            var group = s_CurrentGroupProp.GetValue(instance, new object[] { });
            return group;
        }

        /// <summary>
        /// Create and set a custom size for the Game View
        /// </summary>
        /// <param name="width">The width of the Game View</param>
        /// <param name="height">The height of the Game View</param>
        /// <remarks>
        /// This method creates a new Game View size object and sets its width and height
        /// properties to the specified values. It then adds the new size object to the
        /// list of available sizes in the Game View.
        /// </remarks>
        /// <returns>The new size object</returns>
        public static object SetCustomSize(int width, int height)
        {
            var sizeObj = FindRecorderSizeObj();
            if (sizeObj != null)
            {
                if (!LogErrorIfMissing(s_SizeWidthField, "m_Width") ||
                    !LogErrorIfMissing(s_SizeHeightField, "m_Height"))
                    return null;
                s_SizeWidthField.SetValue(sizeObj, width);
                s_SizeHeightField.SetValue(sizeObj, height);
            }
            else
            {
                sizeObj = AddSize(width, height);
            }

            return sizeObj;
        }

        static object FindRecorderSizeObj()
        {
            var group = Group();
            if (group == null)
                return null;
            if (!LogErrorIfMissing(s_CustomField, "m_Custom") ||
                !LogErrorIfMissing(s_CustomGetEnumeratorMethod, "m_Custom.GetEnumerator") ||
                !LogErrorIfMissing(s_SizeBaseTextField, "m_BaseText"))
                return null;
            var customs = s_CustomField.GetValue(group);
            var itr = (IEnumerator)s_CustomGetEnumeratorMethod.Invoke(customs, new object[] { });
            while (itr.MoveNext())
            {
                var txt = (string)s_SizeBaseTextField.GetValue(itr.Current);
                if (txt == "BackBufferCapture")
                    return itr.Current;
            }

            return null;
        }

        static int IndexOf(object sizeObj)
        {
            var group = Group();
            if (group == null)
                return -1;
            if (!LogErrorIfMissing(s_IndexOfMethod, "GameViewSizeGroup.IndexOf") ||
                !LogErrorIfMissing(s_BuiltinField, "m_Builtin") ||
                !LogErrorIfMissing(s_BuiltinContainsMethod, "m_Builtin.Contains") ||
                !LogErrorIfMissing(s_GetBuiltinCountMethod, "GetBuiltinCount"))
                return -1;
            var index = (int)s_IndexOfMethod.Invoke(group, new[] { sizeObj });

            var builtinList = s_BuiltinField.GetValue(group);
            if ((bool)s_BuiltinContainsMethod.Invoke(builtinList, new[] { sizeObj }))
                return index;

            index += (int)s_GetBuiltinCountMethod.Invoke(group, new object[] { });
            return index;
        }

        static object NewSizeObj(int width, int height)
        {
            if (!LogErrorIfMissing(s_SizeCtor, "GameViewSize constructor"))
                return null;
            var sizeObj = s_SizeCtor.Invoke(new object[] { 1, width, height, "BackBufferCapture" });
            return sizeObj;
        }

        /// <summary>
        /// Add a custom game view size to the list of available sizes
        /// </summary>
        /// <param name="width">The width of the Game View</param>
        /// <param name="height">The height of the Game View</param>
        /// <remarks>
        /// This method creates a new Game View size object and adds it to the list of
        /// available sizes in the Game View. The new size object is created with the specified
        /// width and height values.
        /// </remarks>
        /// <returns>The new size object</returns>
        public static object AddSize(int width, int height)
        {
            var sizeObj = NewSizeObj(width, height);
            if (sizeObj == null)
                return null;

            var group = Group();
            if (group == null || !LogErrorIfMissing(s_AddCustomSizeMethod, "AddCustomSize"))
                return null;
            s_AddCustomSizeMethod.Invoke(group, new[] { sizeObj });

            return sizeObj;
        }

        /// <summary>
        /// Select a Game View size
        /// </summary>
        /// <param name="size">The size to select</param>
        /// <remarks>
        /// This method selects a Game View size by invoking the SizeSelectionCallback method
        /// of the Game View class. The size parameter is the size object to select.
        /// </remarks>
        public static void SelectSize(object size)
        {
            if (size == null)
                return;
            var index = IndexOf(size);
            if (index < 0)
                return;

            var gameView = GetMainGameView();
            if (gameView == null)
                return;
            EnsureInstanceMembersResolved(gameView);
            if (!LogErrorIfMissing(s_SizeSelectionCallbackMethod, "SizeSelectionCallback"))
                return;
            s_SizeSelectionCallbackMethod.Invoke(gameView, new[] { index, size });
        }

        /// <summary>
        /// Current size of the Game View
        /// </summary>
        /// <remarks>
        /// This property retrieves the current size of the Game View by accessing the
        /// currentGameViewSize property of the Game View class. The size is returned as an
        /// object that contains the width and height values.
        /// </remarks>
        /// <value>The current size of the Game View</value>
        public static object CurrentSize
        {
            get
            {
                var gv = GetMainGameView();
                if (gv == null)
                    return new[] { k_MiscSize, k_MiscSize };
                EnsureInstanceMembersResolved(gv);
                if (!LogErrorIfMissing(s_CurrentGameViewSizeProp, "currentGameViewSize"))
                    return new[] { k_MiscSize, k_MiscSize };
                return s_CurrentGameViewSizeProp.GetValue(gv, new object[] { });
            }
        }

        /// <inheritdoc cref="CurrentSize"/>
        [Obsolete("currentSize is deprecated. Use CurrentSize instead. (UnityUpgradable) -> CurrentSize", true)]
        public static object currentSize => CurrentSize;

        /// <summary>
        /// Backup the current size of the Game View
        /// </summary>
        /// <remarks>
        /// This method backs up the current size of the Game View by storing it in a
        /// static variable. This allows the size to be restored later using <see cref="RestoreSize"/>
        /// </remarks>
        public static void BackupCurrentSize()
        {
            s_InitialSizeObj = CurrentSize;
        }

        /// <summary>
        /// Restore the Game View to its initial size
        /// </summary>
        /// <remarks>
        /// This method restores the Game View to its initial size by selecting the size
        /// object stored earlier by <see cref="BackupCurrentSize"/>. This allows the Game View to
        /// return to its original size after a custom size has been set.
        /// </remarks>
        public static void RestoreSize()
        {
            SelectSize(s_InitialSizeObj);
            s_InitialSizeObj = null;
        }
    }
}
