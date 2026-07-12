using UnityEngine;
using UnityEngine.InputSystem;

public class BallMovement : MonoBehaviour
{
    public float moveForce = 10f;
    public float jumpForce = 7f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;

        if (kb == null)
            return;

        moveInput = Vector2.zero;

        if (kb.wKey.isPressed) moveInput.y += 1;
        if (kb.sKey.isPressed) moveInput.y -= 1;
        if (kb.aKey.isPressed) moveInput.x -= 1;
        if (kb.dKey.isPressed) moveInput.x += 1;

        if (kb.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void FixedUpdate()
    {
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        rb.AddForce(move * moveForce);
    }

    void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}