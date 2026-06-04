using Portfolio.Core;
using Portfolio.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.UI
{
    [DisallowMultipleComponent]
    public sealed class ProjectPopupUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text shortDescriptionText;
        [SerializeField] private Text longDescriptionText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button videoButton;
        [SerializeField] private Button playableButton;
        [SerializeField] private Button githubButton;
        [SerializeField] private Button itchButton;
        [SerializeField] private Button downloadButton;

        private ProjectEntry currentEntry;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            WireButton(videoButton, () => currentEntry != null ? currentEntry.VideoUrl : "");
            WireButton(playableButton, () => currentEntry != null ? currentEntry.PlayableUrl : "");
            WireButton(githubButton, () => currentEntry != null ? currentEntry.GithubUrl : "");
            WireButton(itchButton, () => currentEntry != null ? currentEntry.ItchUrl : "");
            WireButton(downloadButton, () => currentEntry != null ? currentEntry.DownloadUrl : "");

            Hide();
        }

        public void Show(ProjectEntry entry)
        {
            currentEntry = entry;

            SetText(titleText, entry != null ? entry.ProjectTitle : "");
            SetText(shortDescriptionText, entry != null ? entry.ShortDescription : "");
            SetText(longDescriptionText, entry != null ? entry.LongDescription : "");
            SetText(statusText, entry != null ? entry.Status.ToString() : "");

            SetButtonVisible(videoButton, entry != null && WebLinkOpener.IsSafeWebUrl(entry.VideoUrl));
            SetButtonVisible(playableButton, entry != null && WebLinkOpener.IsSafeWebUrl(entry.PlayableUrl));
            SetButtonVisible(githubButton, entry != null && WebLinkOpener.IsSafeWebUrl(entry.GithubUrl));
            SetButtonVisible(itchButton, entry != null && WebLinkOpener.IsSafeWebUrl(entry.ItchUrl));
            SetButtonVisible(downloadButton, entry != null && WebLinkOpener.IsSafeWebUrl(entry.DownloadUrl));

            SetPanelVisible(true);
        }

        public void Hide()
        {
            SetPanelVisible(false);
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private static void WireButton(Button button, System.Func<string> getUrl)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(() => WebLinkOpener.Open(getUrl()));
        }
    }
}
