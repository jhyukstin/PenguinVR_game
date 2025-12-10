using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CountdownSpriteUI : MonoBehaviour
{
    [Header("Targets")]
    public CanvasGroup canvasGroup;     // Panel의 CanvasGroup
    public RectTransform animTarget;    // Panel (스케일 애니 대상)
    public Image imageTarget;           // Panel/Image (스프라이트 표시)

    [Header("Sprites (3,2,1,GO 순)")]
    public Sprite[] sprites;            // 길이 4 권장

    [Header("Optional Audio")]
    public AudioSource audioSource;     // 선택
    public AudioClip beepClip;          // 3/2/1
    public AudioClip goClip;            // GO!

    [Header("Timing")]
    public float popIn = 0.15f;        // 팝인(스케일업 + 페이드인)
    public float hold = 0.45f;        // 유지
    public float fadeOut = 0.20f;       // 페이드아웃

    [Header("Animation (Scale Multipliers)")]
    // 절대 스케일이 아니라 "기준 스케일 × 배수"로 애니메이션
    public Vector3 startScaleMultiplier = new Vector3(0.2f, 0.2f, 1f);
    public Vector3 endScaleMultiplier = Vector3.one;
    public AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Events")]
    public UnityEvent onCountdownFinished;

    [Header("Auto")]
    public bool playOnEnable = false;

    // runtime
    bool running;
    Vector3 baseScale; // 실행 시점 animTarget의 원래 스케일(예: 0.001,0.001,0.001)

    void Reset()
    {
        canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        animTarget = GetComponentInChildren<RectTransform>(true);
        imageTarget = GetComponentInChildren<Image>(true);
        audioSource = GetComponentInChildren<AudioSource>(true);
    }

    void OnEnable()
    {
        if (playOnEnable) Play();
    }

    public void Play()
    {
        if (running) return;
        gameObject.SetActive(true);
        StartCoroutine(CoPlay());
    }

    IEnumerator CoPlay()
    {
        running = true;

        if (!canvasGroup) canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        if (!animTarget) animTarget = GetComponentInChildren<RectTransform>(true);
        if (!imageTarget) imageTarget = GetComponentInChildren<Image>(true);

        // 기준 스케일 저장(월드 스페이스 캔버스 0.001 스케일 등 유지)
        baseScale = animTarget.localScale;

        canvasGroup.alpha = 0f;
        animTarget.localScale = Vector3.Scale(baseScale, startScaleMultiplier);

        int count = sprites != null ? sprites.Length : 0;
        for (int i = 0; i < count; i++)
        {
            // 이번 프레임 스프라이트
            var sp = sprites[i];
            imageTarget.sprite = sp;
            imageTarget.enabled = (sp != null);

            // 사운드
            if (audioSource)
            {
                var clip = (i == count - 1 && goClip != null) ? goClip : beepClip;
                if (clip) audioSource.PlayOneShot(clip);
            }

            // 팝인(스케일 + 페이드인)
            yield return FadeAndPop(true, popIn);

            // 유지
            yield return new WaitForSeconds(hold);

            // 페이드아웃
            yield return FadeAndPop(false, fadeOut);
        }

        onCountdownFinished?.Invoke();
        gameObject.SetActive(false);
        running = false;
    }

    IEnumerator FadeAndPop(bool fadeIn, float duration)
    {
        duration = Mathf.Max(0.0001f, duration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);

            if (fadeIn)
            {
                canvasGroup.alpha = a;
                float k = popCurve.Evaluate(a);

                // 기준 스케일 × (배수 보간)
                Vector3 mul = Vector3.LerpUnclamped(startScaleMultiplier, endScaleMultiplier, k);
                animTarget.localScale = Vector3.Scale(baseScale, mul);
            }
            else
            {
                canvasGroup.alpha = 1f - a;
                // 필요 시 페이드아웃 중 스케일 유지(변경 없음)
            }

            yield return null;
        }

        // 엔드 스냅
        canvasGroup.alpha = fadeIn ? 1f : 0f;
        if (fadeIn)
        {
            animTarget.localScale = Vector3.Scale(baseScale, endScaleMultiplier);
        }
        else
        {
            // 꺼질 때 굳이 스케일 되돌릴 필요 없지만,
            // 다음 스텝 시작이 항상 startScaleMultiplier에서 시작하도록 유지하려면 아래 라인 사용:
            // animTarget.localScale = Vector3.Scale(baseScale, startScaleMultiplier);
        }
    }
}
