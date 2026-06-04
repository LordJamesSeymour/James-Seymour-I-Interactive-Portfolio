using System;
using UnityEngine;

namespace Portfolio.Core
{
    public sealed class WebLinkOpener : MonoBehaviour
    {
        public void OpenUrl(string url)
        {
            Open(url);
        }

        public static void Open(string url)
        {
            if (!IsSafeWebUrl(url))
            {
                Debug.LogWarning($"Blocked invalid or unsupported portfolio URL: {url}");
                return;
            }

            Application.OpenURL(url.Trim());
        }

        public static bool IsSafeWebUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri uri))
            {
                return false;
            }

            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
