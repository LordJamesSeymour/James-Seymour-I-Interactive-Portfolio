using Portfolio.Data;
using Portfolio.Player;
using Portfolio.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Portfolio.Interaction
{
    [DisallowMultipleComponent]
    public sealed class ProjectBuilding : MonoBehaviour
    {
        [SerializeField] private ProjectEntry projectEntry;
        [SerializeField] private ProjectPopupUI popupUI;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private string playerTag = "Player";

        private bool playerInRange;

        public ProjectEntry ProjectEntry
        {
            get => projectEntry;
            set => projectEntry = value;
        }

        public ProjectPopupUI PopupUI
        {
            get => popupUI;
            set => popupUI = value;
        }

        public GameObject InteractionPrompt
        {
            get => interactionPrompt;
            set => interactionPrompt = value;
        }

        private void Awake()
        {
            SetPromptVisible(false);
        }

        private void Update()
        {
            if (playerInRange && WasInteractPressed())
            {
                ShowProject();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            playerInRange = true;
            SetPromptVisible(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            playerInRange = false;
            SetPromptVisible(false);
        }

        private bool IsPlayer(Component other)
        {
            return other.GetComponentInParent<PortfolioAvatarController>() != null || other.CompareTag(playerTag);
        }

        private void ShowProject()
        {
            if (projectEntry == null)
            {
                Debug.LogWarning($"{name} has no ProjectEntry assigned.", this);
                return;
            }

            if (popupUI == null)
            {
                popupUI = FindFirstObjectByType<ProjectPopupUI>();
            }

            if (popupUI == null)
            {
                Debug.LogWarning("No ProjectPopupUI found in the scene.", this);
                return;
            }

            popupUI.Show(projectEntry);
        }

        private void SetPromptVisible(bool visible)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(visible);
            }
        }

        private static bool WasInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }
    }
}
