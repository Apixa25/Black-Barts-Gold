// ============================================================================
// ProfileImagePicker.cs
// Black Bart's Gold - Mobile profile image picker
// Path: Assets/Scripts/UI/ProfileImagePicker.cs
// ============================================================================

using System;
using UnityEngine;

namespace BlackBartsGold.UI
{
    /// <summary>
    /// Wraps camera/gallery image picking for profile photos.
    /// </summary>
    public static class ProfileImagePicker
    {
        private const int MAX_IMAGE_SIZE = 1024;

        public static void PickFromGallery(Action<Texture2D, string> onComplete)
        {
#if UNITY_ANDROID || UNITY_IOS
            var permission = NativeGallery.GetImageFromGallery(
                path => HandlePickedPath(path, onComplete),
                "Select a profile photo",
                "image/*"
            );

            if (permission == NativeGallery.Permission.Denied)
            {
                onComplete?.Invoke(null, "Gallery permission denied.");
            }
#else
            onComplete?.Invoke(null, "Gallery is only available on mobile builds.");
#endif
        }

        public static void PickFromCamera(Action<Texture2D, string> onComplete)
        {
#if UNITY_ANDROID || UNITY_IOS
            var permission = NativeGallery.GetImageFromCamera(
                path => HandlePickedPath(path, onComplete),
                "Take profile photo",
                "bbg-profile.jpg",
                MAX_IMAGE_SIZE
            );

            if (permission == NativeGallery.Permission.Denied)
            {
                onComplete?.Invoke(null, "Camera permission denied.");
            }
#else
            onComplete?.Invoke(null, "Camera is only available on mobile builds.");
#endif
        }

        private static void HandlePickedPath(string path, Action<Texture2D, string> onComplete)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                onComplete?.Invoke(null, "No image selected.");
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            Texture2D texture = NativeGallery.LoadImageAtPath(path, MAX_IMAGE_SIZE, false, false, true);
            if (texture == null)
            {
                onComplete?.Invoke(null, "Unable to load selected image.");
                return;
            }

            onComplete?.Invoke(texture, null);
#else
            onComplete?.Invoke(null, "Image loading is only available on mobile builds.");
#endif
        }
    }
}
