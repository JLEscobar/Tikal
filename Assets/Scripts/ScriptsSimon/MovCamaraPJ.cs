using UnityEngine;

public class CharacterLookAtCamera : MonoBehaviour
{
    public Transform cameraTransform;

    void Update()
    {
        // Dirección hacia donde la cámara mira, pero solo en el plano horizontal
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        // Rotar personaje hacia esa dirección
        transform.rotation = Quaternion.LookRotation(camForward);
    }
}
