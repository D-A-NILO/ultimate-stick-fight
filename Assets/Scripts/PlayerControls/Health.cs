using UnityEngine;
using UnityEngine.InputSystem.Utilities;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 5f;

    private float currentHealth;
    private Rigidbody2D rb;

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection = default)
    {
        currentHealth -= damage;

        Debug.Log(gameObject.name + " took " + damage + " damage!");

        // Apply knockback
        if (rb != null)
        {
            rb.AddForce(
                knockbackDirection.normalized * knockbackForce,
                ForceMode2D.Impulse
            );

            Debug.Log("Knockback applied: " + knockbackForce);
            Debug.Log("Velocity: " + rb.linearVelocity);
        }
        else
        {
            Debug.LogError("Enemy Rigidbody2D not found!");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " died!");

        Destroy(gameObject);
    }
}
