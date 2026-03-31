using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyVCamera : MonoBehaviour
{
    [Header("Virtual Camera Target")]
    public Transform lookAt;

    [Header("Orbit Settings")]
    public float distance = 5f;
    public float rotationSpeed = 180f;
    public bool requireMouseButton = true;

    float currentAngle;
    bool isDragging;
    Vector3 lastMousePosition;

    void Start()
    {
        if (lookAt == null)
        {
            Debug.LogWarning("LobbyVCamera: lookAt Transform is not assigned.");
            return;
        }

        Vector3 fromTarget = transform.position - lookAt.position;
        distance = fromTarget.magnitude;

        // 初始水平角度
        currentAngle = Mathf.Atan2(fromTarget.x, fromTarget.z) * Mathf.Rad2Deg;
        UpdateCameraTransform();
    }

    void Update()
    {
        if (lookAt == null)
            return;

        bool dragging = !requireMouseButton || Input.GetMouseButton(0);

        if (dragging)
        {
            if (!isDragging)
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }

            Vector3 delta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            currentAngle += delta.x * (rotationSpeed * Time.deltaTime);
            UpdateCameraTransform();
        }
        else
        {
            isDragging = false;
        }
    }

    void UpdateCameraTransform()
    {
        if (lookAt == null)
            return;

        float rad = currentAngle * Mathf.Deg2Rad;
        distance = 3.5f; // 固定距离
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * distance;
        Vector3 targetPosition = lookAt.position + offset;

        transform.position = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        transform.LookAt(lookAt.position);
    }
}
