using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player; // assign the player GameObject (the one with PlayerMove)
    public Vector3 headOffset = new Vector3(0f, 1.6f, 0f); // camera offset from player origin in local space
    public float mouseSensitivity = 2f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    float pitch = 0f;

    // Internal
    private Rigidbody playerRb;
    private float yawDelta = 0f; // yaw to apply during next FixedUpdate

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (player == null)
        {
            Debug.LogWarning("CameraFollow: assign player Transform.");
            return;
        }

        playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            // Prevent the physics system from tipping the player over. Keep Y rotation free so we can rotate the player with the mouse.
            playerRb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Read mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Store yaw; apply in FixedUpdate (if a Rigidbody exists we'll use MoveRotation there to be physics-friendly)
        yawDelta = mouseX;

        // Pitch rotates the camera locally (clamped)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // Make camera face the same yaw as the player while keeping its own pitch so you can look left/right and up/down
        float playerYaw = player.eulerAngles.y;
        transform.rotation = Quaternion.Euler(pitch, playerYaw, 0f);

        // Follow player position + head offset (uses player's transform so it follows rotation)
        transform.position = player.TransformPoint(headOffset);
    }

    void FixedUpdate()
    {
        if (player == null) return;

        if (playerRb != null && !playerRb.isKinematic)
        {
            // Apply yaw using Rigidbody.MoveRotation for smooth physics interaction
            Quaternion target = playerRb.rotation * Quaternion.Euler(0f, yawDelta, 0f);
            playerRb.MoveRotation(target);
        }
        else
        {
            // No Rigidbody: rotate transform directly
            player.Rotate(Vector3.up, yawDelta);
        }

        // reset yawDelta until next mouse input
        yawDelta = 0f;
    }
}
