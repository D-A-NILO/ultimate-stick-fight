using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Punch Settings")]
    [SerializeField] private float punchDamage = 10f;
    [SerializeField] private float punchDuration = 0.15f;
    [SerializeField] private float punchCooldown = 0.4f;
    [SerializeField] private float upwardsKB = 0.3f;

    [Header("References")]
    [SerializeField] private Collider2D attackCollider;

    private bool canPunch = true;

    private HashSet<Health> hitTargets = new HashSet<Health>();

    private void Start()
    {
        //attackCollider.enabled = false;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && canPunch)
        {
            StartCoroutine(Punch());
        }
    }

    private IEnumerator Punch()
    {
        canPunch = false;

        // Reset targets hit by the previous punch
        hitTargets.Clear();

        // Activate punch hitbox
        attackCollider.enabled = true;

        yield return new WaitForSeconds(punchDuration);

        // Deactivate punch hitbox
        attackCollider.enabled = false;

        // Wait for remaining cooldown
        yield return new WaitForSeconds(
            Mathf.Max(0f, punchCooldown - punchDuration)
        );

        canPunch = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!attackCollider.enabled)
            return;

        Health target = other.GetComponentInParent<Health>();

        if (target == null || hitTargets.Contains(target))
            return;

        hitTargets.Add(target);

        Vector2 knockbackDirection = new Vector2(Mathf.Sign(other.transform.position.x - transform.position.x),upwardsKB).normalized;

        target.TakeDamage(punchDamage, knockbackDirection);
    }
}
