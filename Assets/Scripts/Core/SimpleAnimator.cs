// ============================================================
// FILE: SimpleAnimator.cs
// AUTHOR: Long + Claude
// DESCRIPTION: Lightweight code-driven animation for sprites.
//              Provides idle bobbing, walk bobbing, happy bounce,
//              and angry shake without requiring .anim/.controller
//              assets. Attach to Player or Customer as a fallback
//              when no Animator Controller is assigned.
// ============================================================
using UnityEngine;
using System.Collections;

public class SimpleAnimator : MonoBehaviour
{
    public enum AnimState { Idle, Walk, Happy, Angry, Interact }

    [Header("Idle")]
    [SerializeField] private float idleBobAmount = 0.02f;
    [SerializeField] private float idleBobSpeed = 1.5f;

    [Header("Walk")]
    [SerializeField] private float walkBobAmount = 0.045f;
    [SerializeField] private float walkBobSpeed = 4f;
    [SerializeField] private float walkTiltAmount = 0.02f;

    [Header("Reactions")]
    [SerializeField] private float bounceMagnitude = 0.15f;
    [SerializeField] private float shakeMagnitude = 0.08f;
    [SerializeField] private float reactionDuration = 0.5f;

    private AnimState currentState = AnimState.Idle;
    private Vector3 baseScale;
    private Vector3 baseLocalPos;
    private float timer;
    private bool reactionPlaying;

    void Awake()
    {
        baseScale = transform.localScale;
        baseLocalPos = transform.localPosition;
    }

    void OnEnable()
    {
        RecaptureBasePosition();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        reactionPlaying = false;
        transform.localScale = baseScale;
        transform.localPosition = baseLocalPos;
        currentState = AnimState.Idle;
    }

    public void RecaptureBasePosition()
    {
        baseScale = transform.localScale;
        baseLocalPos = transform.localPosition;
    }

    void Update()
    {
        if (reactionPlaying) return;

        timer += Time.deltaTime;

        switch (currentState)
        {
            case AnimState.Idle:
                ApplyBob(idleBobAmount, idleBobSpeed, 0f);
                break;
            case AnimState.Walk:
                ApplyBob(walkBobAmount, walkBobSpeed, walkTiltAmount);
                break;
        }
    }

    // ==================== PUBLIC API ====================

    public void SetState(AnimState state)
    {
        if (reactionPlaying) return;
        if (currentState == state) return;

        currentState = state;
        timer = 0f;

        if (state == AnimState.Happy)
            StartCoroutine(PlayBounce());
        else if (state == AnimState.Angry)
            StartCoroutine(PlayShake());
        else if (state == AnimState.Interact)
            StartCoroutine(PlaySquash());
    }

    public void SetMoving(bool moving)
    {
        SetState(moving ? AnimState.Walk : AnimState.Idle);
    }

    // ==================== CONTINUOUS ANIMATIONS ====================

    void ApplyBob(float amount, float speed, float tilt)
    {
        float yOffset = Mathf.Sin(timer * speed * Mathf.PI * 2f) * amount;
        transform.localScale = new Vector3(
            baseScale.x + (tilt > 0 ? Mathf.Sin(timer * speed * Mathf.PI * 4f) * tilt : 0f),
            baseScale.y + yOffset,
            baseScale.z
        );
    }

    // ==================== ONE-SHOT REACTIONS ====================

    IEnumerator PlayBounce()
    {
        reactionPlaying = true;
        float elapsed = 0f;

        while (elapsed < reactionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / reactionDuration;
            float bounce = Mathf.Sin(t * Mathf.PI * 4f) * bounceMagnitude * (1f - t);

            transform.localScale = new Vector3(
                baseScale.x - bounce * 0.5f,
                baseScale.y + bounce,
                baseScale.z
            );
            yield return null;
        }

        transform.localScale = baseScale;
        reactionPlaying = false;
        currentState = AnimState.Idle;
    }

    IEnumerator PlayShake()
    {
        reactionPlaying = true;
        float elapsed = 0f;

        while (elapsed < reactionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / reactionDuration;
            float shake = Mathf.Sin(elapsed * 50f) * shakeMagnitude * (1f - t);

            transform.localPosition = baseLocalPos + new Vector3(shake, 0f, 0f);
            yield return null;
        }

        transform.localPosition = baseLocalPos;
        reactionPlaying = false;
        currentState = AnimState.Idle;
    }

    IEnumerator PlaySquash()
    {
        reactionPlaying = true;
        float d = 0.25f;
        float elapsed = 0f;

        while (elapsed < d)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / d;

            float squashY;
            float squashX;
            if (t < 0.4f)
            {
                float st = t / 0.4f;
                squashY = 1f - 0.15f * st;
                squashX = 1f + 0.08f * st;
            }
            else
            {
                float st = (t - 0.4f) / 0.6f;
                squashY = 0.85f + 0.15f * st;
                squashX = 1.08f - 0.08f * st;
            }

            transform.localScale = new Vector3(
                baseScale.x * squashX,
                baseScale.y * squashY,
                baseScale.z
            );
            yield return null;
        }

        transform.localScale = baseScale;
        reactionPlaying = false;
        currentState = AnimState.Idle;
    }
}
