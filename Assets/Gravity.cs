using UnityEngine;

public class CustomGravity : MonoBehaviour
{
    private Rigidbody rb;
    // Adjust this multiplier to change gravity strength (e.g., 2.0 to double it)
    public float gravityMultiplier = 2.0f; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // ForceMode.Acceleration ensures mass does not alter how fast it falls
        Vector3 customGravityForce = Physics.gravity * gravityMultiplier;
        rb.AddForce(customGravityForce, ForceMode.Acceleration);
    }
}