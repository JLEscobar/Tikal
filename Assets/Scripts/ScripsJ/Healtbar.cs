using UnityEngine;
using UnityEngine.UI;

public class Healtbar : MonoBehaviour
{
    [Tooltip("Slider encargado de la vida en la UI")]
    [SerializeField] Slider healthSlider;
    /*[SerializeField] Transform player;
    [SerializeField] Vector3 offset = new Vector3(0, -1f, 0);*/
    //se mueve con el personaje y la camara debajo del objetivo
    void LateUpdate()
    {
        /*transform.position = player.position + offset;
        transform.forward = Camera.main.transform.forward;*/

    }

    public void SetHealth(float current, float max)
    {
        healthSlider.value = current / max;
    }
}
