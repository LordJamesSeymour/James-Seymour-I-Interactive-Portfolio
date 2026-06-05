using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Attach this to a TextMeshPro UI object, or assign a TMP_Text reference in the Inspector.
/// The final message should stay in the TextMeshPro text field; this script reveals it once
/// with TMP's maxVisibleCharacters so rich text tags are not manually sliced.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/TMP Typewriter Vibration")]
public sealed class TMPTypewriterVibration : MonoBehaviour
{
    [Header("Text")]
    [Tooltip("TextMeshPro component to animate. If empty, the script looks for TMP_Text on this GameObject.")]
    [SerializeField] private TMP_Text targetText;

    [Header("Typing")]
    [Tooltip("How long the full typewriter reveal should take, in seconds.")]
    [SerializeField, Min(0.01f)] private float totalTypingDuration = 15f;

    [Tooltip("Starts the typewriter animation automatically when this component is enabled in Play Mode.")]
    [SerializeField] private bool autoPlayOnStart = true;

    [Tooltip("Delay before autoplay starts. Useful for WebGL so loading overlays disappear before the reveal begins.")]
    [SerializeField, Min(0f)] private float autoPlayDelay = 0.75f;

    [Tooltip("When enabled, the text returns to the start of the typewriter animation every time this component is enabled.")]
    [SerializeField] private bool resetOnEnable = true;

    [Header("Letter Vibration")]
    [Tooltip("How long each newly revealed character vibrates before settling.")]
    [SerializeField, Min(0f)] private float vibrationDurationPerCharacter = 0.25f;

    [Tooltip("How far each character can move while vibrating. Keep this low for readable UI text.")]
    [SerializeField, Min(0f)] private float vibrationStrength = 1.2f;

    private const float MinimumTypingDuration = 0.01f;
    private const float VibrationFrequency = 38f;

    private string fullText = string.Empty;
    private bool hasCachedFullText;
    private bool hasPreparedText;
    private bool isPlaying;
    private bool hasWarnedAboutMissingText;
    private bool hasWarnedAboutEmptyText;
    private Coroutine autoPlayRoutine;
    private float elapsedTime;
    private int totalCharacterCount;
    private int visibleCharacterCount;

    /// <summary>
    /// Starts or resumes the animation. If the animation has already finished, it starts again from the beginning.
    /// </summary>
    public void Play()
    {
        if (!EnsureFullTextCached())
        {
            return;
        }

        if (!hasPreparedText || visibleCharacterCount >= totalCharacterCount)
        {
            if (!PrepareFromCurrentText(resetProgress: true))
            {
                return;
            }
        }

        isPlaying = true;

        if (totalCharacterCount == 0)
        {
            FinishAnimation();
        }
    }

    /// <summary>
    /// Stores the current TMP text as the full message, hides it, and plays from the beginning.
    /// </summary>
    public void Restart()
    {
        if (!PrepareFromCurrentText(resetProgress: true))
        {
            return;
        }

        isPlaying = true;

        if (totalCharacterCount == 0)
        {
            FinishAnimation();
        }
    }

    /// <summary>
    /// Immediately ends the animation and leaves the full text cleanly visible.
    /// </summary>
    public void SkipToEnd()
    {
        StopAndShowFullText();
    }

    /// <summary>
    /// Stops the animation, removes any vibration offsets, and shows the full stored text.
    /// </summary>
    public void StopAndShowFullText()
    {
        if (!EnsureFullTextCached())
        {
            return;
        }

        if (!hasPreparedText && !PrepareFromCurrentText(resetProgress: false))
        {
            return;
        }

        isPlaying = false;
        elapsedTime = GetSafeTypingDuration();
        visibleCharacterCount = totalCharacterCount;
        ShowFullCleanText();
    }

    private void Reset()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        if (TryResolveText())
        {
            CacheFullTextFromTarget();
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying || !TryResolveText())
        {
            return;
        }

        if (!hasCachedFullText)
        {
            CacheFullTextFromTarget();
        }

        if (autoPlayOnStart)
        {
            QueueAutoPlay();
        }
        else if (resetOnEnable)
        {
            ShowFullCleanText();
        }
    }

    private void OnDisable()
    {
        isPlaying = false;
        CancelAutoPlay();

        if (Application.isPlaying && targetText != null)
        {
            RestoreCleanMeshForCurrentVisibility();
        }
    }

    private void LateUpdate()
    {
        if (!isPlaying || targetText == null)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        float safeDuration = GetSafeTypingDuration();
        float revealDuration = GetRevealDuration(safeDuration);
        int nextVisibleCount = GetVisibleCharacterCount(revealDuration);

        if (elapsedTime >= safeDuration)
        {
            FinishAnimation();
            return;
        }

        SetVisibleCharacterCount(nextVisibleCount);
        ApplyLetterVibration(revealDuration);
    }

    private bool TryResolveText()
    {
        if (targetText != null)
        {
            return true;
        }

        targetText = GetComponent<TMP_Text>();
        if (targetText != null)
        {
            return true;
        }

        if (!hasWarnedAboutMissingText)
        {
            Debug.LogWarning(
                $"{nameof(TMPTypewriterVibration)} on '{name}' needs a TextMeshPro UI component. " +
                "Assign a TMP_Text reference or place the script on the same GameObject as TextMeshProUGUI.",
                this);
            hasWarnedAboutMissingText = true;
        }

        return false;
    }

    private void QueueAutoPlay()
    {
        CancelAutoPlay();

        if (resetOnEnable && hasCachedFullText)
        {
            PrepareFromCurrentText(resetProgress: true);
        }

        if (autoPlayDelay <= 0f)
        {
            StartAutoPlay();
            return;
        }

        autoPlayRoutine = StartCoroutine(StartAutoPlayAfterDelay());
    }

    private IEnumerator StartAutoPlayAfterDelay()
    {
        yield return new WaitForSeconds(autoPlayDelay);
        autoPlayRoutine = null;
        StartAutoPlay();
    }

    private void StartAutoPlay()
    {
        if (resetOnEnable)
        {
            Restart();
        }
        else
        {
            Play();
        }
    }

    private void CancelAutoPlay()
    {
        if (autoPlayRoutine == null)
        {
            return;
        }

        StopCoroutine(autoPlayRoutine);
        autoPlayRoutine = null;
    }

    private bool EnsureFullTextCached()
    {
        if (!TryResolveText())
        {
            return false;
        }

        if (!hasCachedFullText || string.IsNullOrEmpty(fullText))
        {
            CacheFullTextFromTarget();
        }

        return hasCachedFullText;
    }

    private void CacheFullTextFromTarget()
    {
        if (!TryResolveText())
        {
            return;
        }

        string candidateText = targetText.text ?? string.Empty;
        if (string.IsNullOrEmpty(candidateText) && !hasWarnedAboutEmptyText)
        {
            Debug.LogWarning(
                $"{nameof(TMPTypewriterVibration)} on '{name}' found empty dialogue text. " +
                "The TextMeshPro text field should contain the full message to reveal.",
                this);
            hasWarnedAboutEmptyText = true;
        }

        fullText = candidateText;
        hasCachedFullText = true;
    }

    private bool PrepareFromCurrentText(bool resetProgress)
    {
        if (!EnsureFullTextCached())
        {
            return false;
        }

        targetText.text = fullText;
        targetText.maxVisibleCharacters = int.MaxValue;
        targetText.ForceMeshUpdate();

        totalCharacterCount = targetText.textInfo.characterCount;
        hasPreparedText = true;

        if (resetProgress)
        {
            elapsedTime = 0f;
            SetVisibleCharacterCount(0);
        }
        else
        {
            visibleCharacterCount = totalCharacterCount;
            ShowFullCleanText();
        }

        return true;
    }

    private void SetVisibleCharacterCount(int characterCount)
    {
        visibleCharacterCount = Mathf.Clamp(characterCount, 0, totalCharacterCount);
        targetText.maxVisibleCharacters = visibleCharacterCount;
        targetText.ForceMeshUpdate();
    }

    private int GetVisibleCharacterCount(float revealDuration)
    {
        if (totalCharacterCount == 0 || elapsedTime <= 0f)
        {
            return 0;
        }

        if (totalCharacterCount == 1)
        {
            return 1;
        }

        float progress = Mathf.Clamp01(elapsedTime / revealDuration);
        return Mathf.Clamp(1 + Mathf.FloorToInt(progress * (totalCharacterCount - 1)), 0, totalCharacterCount);
    }

    private void ApplyLetterVibration(float revealDuration)
    {
        if (vibrationDurationPerCharacter <= 0f || vibrationStrength <= 0f || visibleCharacterCount <= 0)
        {
            return;
        }

        TMP_TextInfo textInfo = targetText.textInfo;
        int characterLimit = Mathf.Min(visibleCharacterCount, textInfo.characterCount);

        for (int characterIndex = 0; characterIndex < characterLimit; characterIndex++)
        {
            TMP_CharacterInfo characterInfo = textInfo.characterInfo[characterIndex];
            if (!characterInfo.isVisible)
            {
                continue;
            }

            float revealTime = GetCharacterRevealTime(characterIndex, revealDuration);
            float characterAge = elapsedTime - revealTime;
            if (characterAge < 0f || characterAge > vibrationDurationPerCharacter)
            {
                continue;
            }

            Vector3 offset = GetVibrationOffset(characterIndex, characterAge);
            OffsetCharacterVertices(textInfo, characterInfo, offset);
        }

        targetText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    private float GetCharacterRevealTime(int characterIndex, float revealDuration)
    {
        if (totalCharacterCount <= 1)
        {
            return 0f;
        }

        return revealDuration * characterIndex / (totalCharacterCount - 1f);
    }

    private Vector3 GetVibrationOffset(int characterIndex, float characterAge)
    {
        float normalizedAge = Mathf.Clamp01(characterAge / vibrationDurationPerCharacter);
        float fade = 1f - normalizedAge;
        float seed = characterIndex * 19.371f;
        float x = Mathf.PerlinNoise(seed, characterAge * VibrationFrequency) - 0.5f;
        float y = Mathf.PerlinNoise(seed + 91.7f, characterAge * (VibrationFrequency * 1.13f)) - 0.5f;

        return new Vector3(x, y, 0f) * (vibrationStrength * 2f * fade);
    }

    private static void OffsetCharacterVertices(TMP_TextInfo textInfo, TMP_CharacterInfo characterInfo, Vector3 offset)
    {
        int materialIndex = characterInfo.materialReferenceIndex;
        if (materialIndex < 0 || materialIndex >= textInfo.meshInfo.Length)
        {
            return;
        }

        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
        int vertexIndex = characterInfo.vertexIndex;
        if (vertices == null || vertexIndex < 0 || vertexIndex + 3 >= vertices.Length)
        {
            return;
        }

        vertices[vertexIndex] += offset;
        vertices[vertexIndex + 1] += offset;
        vertices[vertexIndex + 2] += offset;
        vertices[vertexIndex + 3] += offset;
    }

    private void FinishAnimation()
    {
        isPlaying = false;
        elapsedTime = GetSafeTypingDuration();
        visibleCharacterCount = totalCharacterCount;
        ShowFullCleanText();
    }

    private void ShowFullCleanText()
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text = fullText;
        targetText.maxVisibleCharacters = int.MaxValue;
        targetText.ForceMeshUpdate();
    }

    private void RestoreCleanMeshForCurrentVisibility()
    {
        targetText.ForceMeshUpdate();
    }

    private float GetSafeTypingDuration()
    {
        return Mathf.Max(MinimumTypingDuration, totalTypingDuration);
    }

    private float GetRevealDuration(float safeDuration)
    {
        return Mathf.Max(MinimumTypingDuration, safeDuration - vibrationDurationPerCharacter);
    }

    private void OnValidate()
    {
        totalTypingDuration = Mathf.Max(MinimumTypingDuration, totalTypingDuration);
        autoPlayDelay = Mathf.Max(0f, autoPlayDelay);
        vibrationDurationPerCharacter = Mathf.Max(0f, vibrationDurationPerCharacter);
        vibrationStrength = Mathf.Max(0f, vibrationStrength);

        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }
}
