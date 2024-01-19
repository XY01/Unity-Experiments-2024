using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Flaim.Utils
{
    /// <summary>
    /// This script raycasts from the camera to the world and sets the transform position to the hit point.
    /// </summary>
    public class CameraRaycastPosition : MonoBehaviour
    {
        public float smoothing = 10f; // Smoothing factor for movement
        private Vector3 previousPosition; // To store the previous position
        public Vector3 Velocity { get; private set; } // The velocity of the transform

        void Start()
        {
            // Initialize previous position to the current transform position
            previousPosition = transform.position;
        }

        void Update()
        {
            // Check if the left mouse button is clicked
            if (Input.GetMouseButton(0))
            {
                RaycastFromCameraToWorld();
            }

            // Calculate velocity
            Velocity = (transform.position - previousPosition) / Time.deltaTime;

            // Update the previous position
            previousPosition = transform.position;
        }

        private void RaycastFromCameraToWorld()
        {
            // Get the main camera
            Camera camera = Camera.main;

            // Create a ray from the camera through the mouse position
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            // Perform the raycast
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Set the transform position to the hit point with smoothing
                transform.position = Vector3.Lerp(transform.position, hit.point, smoothing * Time.deltaTime);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, .1f);
            // draw velocity
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + Velocity * .3f);
        }
    }
}
