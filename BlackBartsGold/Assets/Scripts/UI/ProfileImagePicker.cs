// ============================================================================
// ProfileImagePicker.cs
// Black Bart's Gold - Mobile profile image picker
// Path: Assets/Scripts/UI/ProfileImagePicker.cs
// ============================================================================

using System;
using System.Reflection;
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
            NativeGallery.GetImageFromGallery(
                path =>
                {
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        onComplete?.Invoke(null, "Gallery access denied or no image selected.");
                        return;
                    }

                    HandlePickedPath(path, onComplete);
                },
                "Select a profile photo",
                "image/*"
            );
#else
            onComplete?.Invoke(null, "Gallery is only available on mobile builds.");
#endif
        }

        public static void PickFromCamera(Action<Texture2D, string> onComplete)
        {
#if UNITY_ANDROID || UNITY_IOS
            // This NativeGallery version does not expose GetImageFromCamera, so
            // we safely fall back to image selection from gallery.
            MethodInfo cameraMethod = typeof(NativeGallery).GetMethod("GetImageFromCamera", BindingFlags.Public | BindingFlags.Static);
            if (cameraMethod != null)
            {
                try
                {
                    NativeGallery.MediaPickCallback callback = path =>
                    {
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            onComplete?.Invoke(null, "Camera access denied or no image selected.");
                            return;
                        }

                        HandlePickedPath(path, onComplete);
                    };

                    cameraMethod.Invoke(null, new object[] { callback, "Take profile photo", "bbg-profile.jpg", MAX_IMAGE_SIZE });
                    return;
                }
                catch
                {
                    // If reflection invocation fails, continue to gallery fallback below.
                }
            }

            NativeGallery.GetImageFromGallery(
                path =>
                {
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        onComplete?.Invoke(null, "Camera mode unavailable in this plugin version and no image was selected.");
                        return;
                    }

                    HandlePickedPath(path, onComplete);
                },
                "Select profile photo",
                "image/*"
            );
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
