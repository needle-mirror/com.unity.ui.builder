using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.UI.Builder
{
    internal class BuilderNotifications : VisualElement
    {
        VisualTreeAsset m_NotificationEntryVTA;
        int m_PendingNotifications;

        public new class UxmlFactory : UxmlFactory<BuilderNotifications, UxmlTraits> { }

        public bool hasPendingNotifications => m_PendingNotifications > 0;

        public BuilderNotifications()
        {
            m_NotificationEntryVTA = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/BuilderNotificationEntry.uxml");
            CheckNotificationWorthyStates();
        }

        public void ResetNotifications()
        {
            BuilderProjectSettings.hideNotificationAboutMissingUITKPackage = false;

            ClearNotifications();
            CheckNotificationWorthyStates();
        }

        public void ClearNotifications()
        {
            Clear();
        }

        void AddNotification(string message, string detailsURL, Action closeAction)
        {
            var newNotification = m_NotificationEntryVTA.CloneTree();
            newNotification.AddToClassList("unity-builder-notification-entry");

            var icon = newNotification.Q("icon");
            icon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("console.infoicon.sml").image;

            var messageLabel = newNotification.Q<Label>("message");
#if !UNITY_2019_4
            // Cannot use USS because no way to do version checks in USS.
            // This is not available in 2019.4.
            messageLabel.style.textOverflow = TextOverflow.Ellipsis;
#endif
            messageLabel.text = message;

            newNotification.Q<Button>("details").clickable.clicked +=
                () => Application.OpenURL(detailsURL);

            newNotification.Q<Button>("dismiss").clickable.clicked +=
                () => { newNotification.RemoveFromHierarchy(); closeAction(); };

            Add(newNotification);
        }

        void CheckNotificationWorthyStates()
        {
            m_PendingNotifications = 0;

#if UNITY_2020_1 || UNITY_2020_2 || UNITY_2020_3
            // Handle the missing UI Toolkit package case.
            var uitkPackageInfo = PackageInfo.FindForAssetPath("Packages/" + BuilderConstants.UIToolkitPackageName);
            if (uitkPackageInfo == null)
            {
                m_PendingNotifications++;
                if (!BuilderProjectSettings.hideNotificationAboutMissingUITKPackage)
                    AddNotification(
                        BuilderConstants.NoUIToolkitPackageInstalledNotification,
                        "https://forum.unity.com/threads/ui-toolkit-1-0-preview-available.927822/",
                        () => BuilderProjectSettings.hideNotificationAboutMissingUITKPackage = true);
            }
#endif
        }
    }
}
