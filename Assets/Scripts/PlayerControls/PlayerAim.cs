using UnityEngine;

public class PlayerAim : MonoBehaviour
{
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackDistance = 0.8f;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        Aim();
    }

    private void Aim()
    {
        // Get mouse position in world space
        Vector3 mousePosition = Input.mousePosition;

        mousePosition.z = Mathf.Abs(
            mainCamera.transform.position.z - transform.position.z
        );

        mousePosition = mainCamera.ScreenToWorldPoint(mousePosition);

        // Calculate direction from player to mouse
        Vector2 direction = (mousePosition - transform.position).normalized;

        // Position attack point around player
        attackPoint.position = transform.position +
            (Vector3)(direction * attackDistance);

        // Rotate attack point towards mouse
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        attackPoint.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
