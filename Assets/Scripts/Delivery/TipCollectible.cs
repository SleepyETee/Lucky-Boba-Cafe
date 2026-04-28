// ============================================================
// FILE: TipCollectible.cs
// DESCRIPTION: Tip coins scattered on roads
// Player collects for bonus money
// ============================================================
using UnityEngine;

public class TipCollectible : MonoBehaviour
{
    [Header("Value")]
    public int value = 5;
    
    [Header("Animation")]
    public float bobSpeed = 3f;
    public float bobAmount = 0.15f;
    public float spinSpeed = 90f;
    
    [Header("Collection")]
    public GameObject collectEffect;
    public float collectScale = 1.5f;
    public float collectDuration = 0.2f;
    
    private Vector3 startPos;
    private bool collected = false;
    private SpriteRenderer cachedSR;
    
    void Start()
    {
        startPos = transform.position;
        cachedSR = GetComponent<SpriteRenderer>();
    }
    
    void Update()
    {
        if (collected) return;
        
        // Bob up and down
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = startPos + Vector3.up * bob;
        
        // Spin
        transform.Rotate(0, 0, spinSpeed * Time.deltaTime);
    }
    
    public void Collect()
    {
        if (collected) return;
        collected = true;
        
        // Spawn effect
        if (collectEffect)
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        
        // Quick scale up then destroy
        StartCoroutine(CollectAnimation());
    }
    
    System.Collections.IEnumerator CollectAnimation()
    {
        float timer = 0;
        Vector3 originalScale = transform.localScale;
        
        while (timer < collectDuration)
        {
            timer += Time.deltaTime;
            float t = timer / collectDuration;
            
            transform.localScale = originalScale * Mathf.Lerp(1f, collectScale, t);
            
            if (cachedSR)
            {
                Color c = cachedSR.color;
                c.a = 1f - t;
                cachedSR.color = c;
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
