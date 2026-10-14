using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 장소별 BGM을 AudioSource 하나로 재생한다. 장소 ↔ 곡 연결은 Inspector 목록에서만 바꾼다.
// 같은 곡이 지정된 장소끼리 이동하면 끊지 않고 계속 재생하고, 다른 곡이면 페이드 아웃 → 페이드 인,
// 곡이 없는 장소로 가면 페이드 아웃 후 정지한다.
public class BGMManager : MonoBehaviour
{
    [System.Serializable]
    private class LocationBGM
    {
        public LocationData location;
        public AudioClip clip;
    }

    [SerializeField] private AudioSource source;

    [Tooltip("장소마다 재생할 곡. 목록에 없는 장소로 이동하면 BGM이 페이드 아웃되어 멈춘다. 여러 장소에 같은 곡을 넣으면 그 사이를 이동해도 처음부터 다시 재생하지 않는다")]
    [SerializeField] private List<LocationBGM> locationBGMs = new List<LocationBGM>();

    [Range(0f, 1f)] [SerializeField] private float volume = 0.35f;

    [Tooltip("곡을 바꿀 때 이전 곡이 사라지는 시간, 새 곡이 커지는 시간 (각각, 초)")]
    [SerializeField] private float fadeDuration = 0.6f;

    private AudioClip targetClip;
    private Coroutine fade;

    private void Awake()
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
    }

    // 페이드 중이 아닐 때는 Inspector의 Volume 변경을 바로 반영한다
    private void Update()
    {
        if (fade == null && source.clip != null)
            source.volume = volume;
    }

    public void PlayForLocation(LocationData location)
    {
        AudioClip clip = null;
        foreach (LocationBGM entry in locationBGMs)
        {
            if (entry.location == location)
            {
                clip = entry.clip;
                break;
            }
        }
        Play(clip);
    }

    // clip이 null이면 페이드 아웃 후 정지
    public void Play(AudioClip clip)
    {
        if (clip == targetClip)
            return;
        targetClip = clip;
        if (fade != null)
            StopCoroutine(fade);
        fade = StartCoroutine(SwitchTo(clip));
    }

    private IEnumerator SwitchTo(AudioClip clip)
    {
        // 다른 곡이 나오고 있으면 먼저 줄인다 (같은 곡으로 돌아온 경우는 줄이지 않고 이어서 키운다)
        if (source.clip != null && source.clip != clip)
        {
            yield return FadeVolume(0f);
            source.Stop();
        }

        if (clip == null)
        {
            source.clip = null;
            fade = null;
            yield break;
        }

        if (source.clip != clip)
        {
            source.clip = clip;
            source.volume = 0f;
            source.Play();
        }
        yield return FadeVolume(volume);
        fade = null;
    }

    private IEnumerator FadeVolume(float to)
    {
        float from = source.volume;
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        source.volume = to;
    }
}
