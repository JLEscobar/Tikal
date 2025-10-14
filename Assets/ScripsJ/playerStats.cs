using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class playerStats : MonoBehaviour, I_Damageable
{
    [Tooltip("Script ligado a la barra de vida del peon")]
    [SerializeField] private Healtbar healthbar;
    [Tooltip("GameObject que representa la vida maxima que va a tener el peon")]
    [SerializeField] int maxHealth = 100;
    [Tooltip("Script encargado de controlar al peon")]
    [SerializeField] private MonoBehaviour movementScript;

    private int currentHealth;

    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        healthbar.SetHealth(currentHealth, maxHealth);
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); 
        healthbar.SetHealth(currentHealth, maxHealth);
        if (currentHealth <= 0) Die();
    }

    public void Die()
    {
        animator.SetTrigger("Die");
        movementScript.enabled = false;
        StartCoroutine(WaitForDeathAnimation());
    }

    private IEnumerator WaitForDeathAnimation()
    {
        yield return new WaitForSeconds(2f);
        //SceneManager.LoadScene(0);
        //Reload el selector de personaje
    }
}
