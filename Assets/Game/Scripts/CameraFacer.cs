using UnityEngine;

public class CameraFacer : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            //transform.LookAt(Camera.main.transform);

            // Calculate the direction away from the camera
            Vector3 directionAwayFromCamera = transform.position - Camera.main.transform.position;

            // Rotate the GameObject to face away from the camera
            transform.rotation = Quaternion.LookRotation(directionAwayFromCamera);


        }
    }
}
