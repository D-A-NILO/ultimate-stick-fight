using UnityEngine;

public class PlayerBlock : MonoBehaviour
{
    [SerializeField] private Health playerHealth;

    [Header("Defense Settings")]
    [SerializeField] private float parryDuration = 0.2f;
    [SerializeField] private float blockDamageMultiplier = 0.25f;

    private bool isBlocking;
    private bool isParrying;

    public bool IsBlocking => isBlocking;
    public bool IsParrying => isParrying;

    private float parryTimer;

    private void Update()
    {
        HandleDefense();

        if (isParrying)
        {
            Debug.Log("PARRYING");
        }
        else if (isBlocking)
        {
            Debug.Log("BLOCKING");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TakeHit(10f, gameObject);
        }
    }

    private void HandleDefense()
    {
        // Right click pressed
        if (Input.GetMouseButtonDown(1))
        {
            isBlocking = true;
            isParrying = true;

            parryTimer = parryDuration;
        }

        // Parry timer
        if (isParrying)
        {
            parryTimer -= Time.deltaTime;

            if (parryTimer <= 0f)
            {
                isParrying = false;
            }
        }

        // Right click released
        if (Input.GetMouseButtonUp(1))
        {
            isBlocking = false;
            isParrying = false;
        }
    }

    public void TakeHit(float damage, GameObject attacker)
    {
        if (isParrying)
        {
            Debug.Log("Successful Parry!");

            // We'll add enemy stun/knockback here later.
            return;
        }

        if (isBlocking)
        {
            damage *= blockDamageMultiplier;

            Debug.Log("Blocked attack! Reduced damage.");
        }

        Debug.Log("Player took " + damage + " damage.");
        playerHealth.TakeDamage(damage);
    }
}

