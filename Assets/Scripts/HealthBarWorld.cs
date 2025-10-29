using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarWorld : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Health healthComponent;
    [SerializeField] private CharacterActor characterActor;

    [Header("UI")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI rangeText;

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);

    private Camera _mainCamera;
    private TurnSystem _turnSystem;
    private Vector3 _initialScale;

    void Awake()
    {
        _mainCamera = Camera.main;

        if (healthComponent == null)
        {
            healthComponent = GetComponentInParent<Health>();
        }

        if (characterActor == null)
        {
            characterActor = GetComponentInParent<CharacterActor>();
        }

        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(healthComponent.CurrentHealth, healthComponent.MaxHealth);
        }

        if (worldCanvas != null)
        {
            _initialScale = worldCanvas.transform.localScale;

            // If initial scale is zero, set a default
            if (_initialScale == Vector3.zero)
            {
                _initialScale = Vector3.one;
                worldCanvas.transform.localScale = _initialScale;
            }

            // Always keep canvas active
            worldCanvas.gameObject.SetActive(true);
        }
    }

    void Start()
    {
        _turnSystem = FindFirstObjectByType<TurnSystem>();
        if (_turnSystem != null)
        {
            _turnSystem.OnTurnStarted += OnTurnChanged;
            // Set initial scale
            UpdateScale(_turnSystem.CurrentActor);
        }

        if (worldCanvas != null)
        {
            worldCanvas.gameObject.SetActive(true);
        }

        nameText.text = characterActor != null ? characterActor.CharacterName : "Unknown Name";
        attackText.text = characterActor != null ? "" + characterActor.AttackPower.ToString() : "0";
        speedText.text = characterActor != null ? characterActor.MovementRange.ToString("F1") : "0";
        rangeText.text = characterActor != null ? characterActor.Stats.movementRange.ToString("F1") : "0";

        attackText.text = "Ataque: " + attackText.text;
        speedText.text = "Vel: " + speedText.text;
        rangeText.text = "Rango Mov: " + rangeText.text;
    }

    void OnDestroy()
    {
        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged -= UpdateHealthBar;
        }

        if (_turnSystem != null)
        {
            _turnSystem.OnTurnStarted -= OnTurnChanged;
        }
    }

    void LateUpdate()
    {
        if (worldCanvas != null && _mainCamera != null)
        {
            if (!worldCanvas.gameObject.activeSelf)
            {
                worldCanvas.gameObject.SetActive(true);
            }

            // Position above character
            worldCanvas.transform.position = healthComponent.transform.position + offset;

            // Face camera
            worldCanvas.transform.rotation = Quaternion.LookRotation(
                worldCanvas.transform.position - _mainCamera.transform.position
            );
        }
    }

    private void UpdateHealthBar(int current, int max)
    {
        if (healthSlider == null) return;

        healthSlider.maxValue = max;
        healthSlider.value = current;

        if (worldCanvas != null)
        {
            worldCanvas.gameObject.SetActive(true);
        }
    }

    private void OnTurnChanged(Team team, CharacterActor activeActor)
    {
        UpdateScale(activeActor);
    }

    private void UpdateScale(CharacterActor activeActor)
    {
        if (worldCanvas == null || characterActor == null) return;

        if (_initialScale == Vector3.zero)
        {
            _initialScale = Vector3.one;
        }

        Vector3 targetScale;

        // Check if this is the active character
        if (activeActor == characterActor)
        {
            // Active character: full scale
            targetScale = _initialScale;
        }
        else if (characterActor.Team == Team.Enemy)
        {
            // Enemy: medium scale (initial / 1.5)
            targetScale = _initialScale / 1.5f;
        }
        else
        {
            // Ally not active: small scale (initial / 2)
            targetScale = _initialScale / 2f;
        }

        worldCanvas.transform.localScale = targetScale;

        worldCanvas.gameObject.SetActive(true);
    }
}
