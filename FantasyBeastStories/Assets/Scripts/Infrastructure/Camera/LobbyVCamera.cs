using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Domain.Event;
using Presentation.PlayerInput;

namespace Infrastructure.Camera
{
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
    bool blockRotation;
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

      currentAngle = Mathf.Atan2(fromTarget.x, fromTarget.z) * Mathf.Rad2Deg;
      UpdateCameraTransform();
    }

    void OnEnable()
    {
      if (EventChannelLocator.MainContainer != null)
        EventChannelLocator.MainContainer.changeCanRotateChannel.RegisterListener(OnChangeCanRotate);
    }

    void OnDisable()
    {
      if (EventChannelLocator.MainContainer != null)
        EventChannelLocator.MainContainer.changeCanRotateChannel.UnregisterListener(OnChangeCanRotate);
    }

    private void OnChangeCanRotate(bool canRotate)
    {
      blockRotation = canRotate;
    }

    void Update()
    {
      if (lookAt == null || blockRotation)
        return;

      bool dragging = !requireMouseButton || MobileInputHelper.GetPointerHeld();

      if (dragging)
      {
        if (!isDragging)
        {
          isDragging = true;
          lastMousePosition = MobileInputHelper.GetScreenPosition();
        }

        Vector3 currentPos = MobileInputHelper.GetScreenPosition();
        Vector3 delta = currentPos - lastMousePosition;
        lastMousePosition = MobileInputHelper.GetScreenPosition();

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
      distance = 3f;
      Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * distance;
      Vector3 targetPosition = lookAt.position + offset;

      transform.position = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
      transform.LookAt(lookAt.position);
    }
  }
}