using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Cinemachine; // Added Cinemachine namespace
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Systems")]
    [SerializeField] private TurnSystem turnSystem;

    [Header("UI")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject defeatScreen;
    
    [Header("World UI (for TacticalMovementController)")]
    public Canvas canvasWorldObject;
    public UnityEngine.UI.Button endTurnButton;
    
    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private UnityEngine.Object virtualCamera; // Puede ser CinemachineCamera o CinemachineFreeLook
    [SerializeField] private float cameraHeightOffset = 1.5f; // Altura adicional para el target de la cámara
    [SerializeField] private string cameraTargetChildName = "CameraTarget"; // Nombre del hijo a buscar como target
    [SerializeField] private float cameraDistanceOffset = 25f; // Distancia adicional para alejar la cámara del personaje (ajustable en inspector)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (turnSystem == null) turnSystem = FindFirstObjectByType<TurnSystem>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (virtualCamera == null) 
        {
            // Intentar encontrar primero FreeLook, luego Camera normal
            var freeLook = FindFirstObjectByType<Unity.Cinemachine.CinemachineFreeLook>();
            if (freeLook != null)
            {
                virtualCamera = freeLook;
                Debug.Log("[CAMERA] Encontrada FreeLook Camera");
            }
            else
            {
                virtualCamera = FindFirstObjectByType<CinemachineCamera>();
                if (virtualCamera != null)
                {
                    Debug.Log("[CAMERA] Encontrada CinemachineCamera");
                }
            }
        }
        
        // NO ajustar la distancia aquí - se ajustará cuando se asigne el primer target
        // Esto asegura que la cámara tenga un target antes de ajustar la distancia
        if (virtualCamera != null)
        {
            Debug.Log($"[CAMERA] Tipo de cámara detectado: {virtualCamera.GetType().Name}. La distancia se ajustará cuando se asigne el primer target.");
        }
    }
    
    void Start()
    {
        // Ajustar la distancia de la cámara al inicio si ya tiene un target asignado
        // Esto asegura que la distancia sea correcta desde el principio
        if (virtualCamera != null)
        {
            bool hasTarget = false;
            if (virtualCamera is CinemachineCamera cam && cam.Follow != null)
            {
                hasTarget = true;
            }
            else if (virtualCamera is Unity.Cinemachine.CinemachineFreeLook freeLook && freeLook.Follow != null)
            {
                hasTarget = true;
            }
            
            if (hasTarget)
            {
                Debug.Log("[CAMERA] La cámara ya tiene un target asignado al inicio. Ajustando distancia...");
                // Usar el mismo método que se usa cuando se actualiza el target
                StartCoroutine(AdjustCameraDistanceDelayed());
            }
            else
            {
                // Si no tiene target, ajustar la distancia de todas formas para cuando se asigne
                Debug.Log("[CAMERA] La cámara no tiene target al inicio. La distancia se ajustará cuando se asigne el primer target.");
            }
        }
    }

    void OnEnable()
    {
        PauseService.OnPauseChanged += HandlePauseChanged;

        if (turnSystem != null)
        {
            turnSystem.OnBattleEnded += HandleBattleEnded;
            turnSystem.OnTurnStarted += HandleTurnStarted; // Subscribe to turn started event
        }
    }

    void OnDisable()
    {
        PauseService.OnPauseChanged -= HandlePauseChanged;

        if (turnSystem != null)
        {
            turnSystem.OnBattleEnded -= HandleBattleEnded;
            turnSystem.OnTurnStarted -= HandleTurnStarted; // Unsubscribe from turn started event
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseService.TogglePause();
        }
    }
    
    public Camera GetMainCamera()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        return mainCamera;
    }

    private void HandlePauseChanged(bool isPaused)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
        }
    }

    private void HandleBattleEnded(Team winner)
    {
        Debug.Log($"[v0] GameManager: Battle ended, winner is {winner}");
        
        if (winner == Team.Player)
        {
            ShowVictory();
        }
        else
        {
            ShowDefeat();
        }
    }

    public void ReturnToMenu()
    {
        PauseService.SetPaused(false);
        SceneManager.LoadScene("Menu");
    }
    public void TogglePause()
    {
        PauseService.TogglePause();
    }

    public void ShowVictory()
    {
        if (victoryScreen != null) victoryScreen.SetActive(true);
    }
    public void GoToNextLevel()
    {
        PauseService.SetPaused(false);

        if (SceneManager.GetActiveScene().buildIndex + 1 >= SceneManager.sceneCountInBuildSettings)
        {
            ReturnToMenu();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    public void ShowDefeat()
    {
        if (defeatScreen != null) defeatScreen.SetActive(true);
    }

    public void RestartBattle()
    {
        PauseService.SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void HandleTurnStarted(Team team, CharacterActor actor)
    {
        // Update camera for both player and enemy teams
        // Solo actualizar si hay un actor válido (ignorar llamadas con null)
        // IMPORTANTE: Para el equipo Player, NO actualizar la cámara automáticamente al inicio de fase
        // La cámara se actualizará cuando el usuario seleccione manualmente un personaje
        if (virtualCamera != null && actor != null)
        {
            // Solo actualizar la cámara automáticamente para enemigos
            // Para jugadores, la cámara se actualizará cuando seleccionen manualmente un personaje
            if (team == Team.Enemy)
        {
            Transform cameraTarget = GetCameraTarget(actor.transform);
                if (cameraTarget != null)
                {
            UpdateCameraTarget(cameraTarget);
            Debug.Log($"[CAMERA] Following {team} character: {actor.CharacterName}");
                }
                else
                {
                    Debug.LogWarning($"[CAMERA] No se pudo encontrar target de cámara para {actor.CharacterName}");
                }
            }
            else
            {
                // Para jugadores, no actualizar automáticamente (se actualizará al seleccionar manualmente)
                Debug.Log($"[CAMERA] Turno de {actor.CharacterName} iniciado (cámara NO actualizada automáticamente para jugadores)");
            }
        }
    }

    private Transform GetCameraTarget(Transform characterTransform)
    {
        if (characterTransform == null) return null;

        // Primero intentar buscar un hijo específico para la cámara
        Transform cameraTargetChild = characterTransform.Find(cameraTargetChildName);
        if (cameraTargetChild != null)
        {
            return cameraTargetChild;
        }

        // Si no existe, buscar por nombre común alternativo
        foreach (Transform child in characterTransform)
        {
            if (child.name.Contains("Camera") || child.name.Contains("Head") || child.name.Contains("LookAt"))
            {
                return child;
            }
        }

        // Si no hay hijo específico, crear o usar un Transform con offset
        // Buscar si ya existe un objeto temporal para este personaje
        GameObject offsetObject = GameObject.Find($"CameraTarget_{characterTransform.name}");
        if (offsetObject == null)
        {
            offsetObject = new GameObject($"CameraTarget_{characterTransform.name}");
            offsetObject.transform.SetParent(characterTransform);
            // Offset solo con altura (Y) - la distancia se ajustará en los componentes de Cinemachine
            offsetObject.transform.localPosition = new Vector3(0, cameraHeightOffset, 0);
        }
        else
        {
            // Actualizar el offset si ya existe
            offsetObject.transform.localPosition = new Vector3(0, cameraHeightOffset, 0);
        }
        
        return offsetObject.transform;
    }

    private void UpdateCameraTarget(Transform target)
    {
        if (virtualCamera == null || target == null) return;
        
        // Actualizar Follow y LookAt dependiendo del tipo de cámara
        if (virtualCamera is CinemachineCamera cam)
        {
            cam.Follow = target;
            cam.LookAt = target;
        }
        else if (virtualCamera is Unity.Cinemachine.CinemachineFreeLook freeLook)
        {
            freeLook.Follow = target;
            freeLook.LookAt = target;
        }
        
        // Ajustar la distancia de la cámara DESPUÉS de asignar el target
        // Usar coroutine para asegurar que la cámara procese el cambio de target primero
        StartCoroutine(AdjustCameraDistanceAfterTargetUpdate());
    }
    
    private IEnumerator AdjustCameraDistanceAfterTargetUpdate()
    {
        // Esperar varios frames para asegurar que la cámara procese el cambio de target
        yield return null;
        yield return null;
        yield return null;
        
        AdjustCameraDistance();
        
        // Ajustar nuevamente después de más tiempo para asegurar que se mantenga
        yield return new WaitForSeconds(0.2f);
        AdjustCameraDistance();
    }
    
    private void AdjustCameraDistance()
    {
        if (virtualCamera == null) 
        {
            Debug.LogWarning("[CAMERA] virtualCamera es null, no se puede ajustar la distancia");
            return;
        }
        
        Debug.Log($"[CAMERA] Ajustando distancia de cámara. Valor objetivo: {cameraDistanceOffset}m");
        bool distanceAdjusted = false;
        
        // Verificar si es un CinemachineFreeLook
        if (virtualCamera is Unity.Cinemachine.CinemachineFreeLook freeLook)
        {
            Debug.Log($"[CAMERA] Detectada FreeLook Camera, intentando ajustar distancia a {cameraDistanceOffset}m");
            
            // Intentar usar SerializedObject para modificar los valores
            #if UNITY_EDITOR
            SerializedObject so = new SerializedObject(freeLook);
            SerializedProperty orbitsProp = so.FindProperty("m_Orbits");
            
            if (orbitsProp != null && orbitsProp.isArray)
            {
                for (int i = 0; i < orbitsProp.arraySize; i++)
                {
                    SerializedProperty orbitProp = orbitsProp.GetArrayElementAtIndex(i);
                    SerializedProperty radiusProp = orbitProp.FindPropertyRelative("m_Radius");
                    
                    if (radiusProp != null)
                    {
                        float oldRadius = radiusProp.floatValue;
                        radiusProp.floatValue = cameraDistanceOffset;
                        Debug.Log($"[CAMERA] Órbita {i}: Radio cambiado de {oldRadius} a {cameraDistanceOffset}");
                    }
                }
                so.ApplyModifiedProperties();
                distanceAdjusted = true;
                Debug.Log($"[CAMERA] ✓ Distancia de FreeLook Camera ajustada exitosamente a {cameraDistanceOffset}m");
            }
            else
            {
                Debug.LogWarning("[CAMERA] No se encontró la propiedad m_Orbits en FreeLook Camera");
            }
            #else
            // En build, usar reflection como fallback
            var freeLookType = typeof(Unity.Cinemachine.CinemachineFreeLook);
            var orbitsField = freeLookType.GetField("m_Orbits", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (orbitsField != null)
            {
                var orbits = orbitsField.GetValue(freeLook);
                if (orbits != null)
                {
                    var orbitArray = orbits as System.Array;
                    if (orbitArray != null && orbitArray.Length > 0)
                    {
                        for (int i = 0; i < orbitArray.Length; i++)
                        {
                            var orbit = orbitArray.GetValue(i);
                            var radiusField = orbit.GetType().GetField("m_Radius");
                            if (radiusField != null)
                            {
                                radiusField.SetValue(orbit, cameraDistanceOffset);
                                orbitArray.SetValue(orbit, i);
                            }
                        }
                        orbitsField.SetValue(freeLook, orbits);
                        distanceAdjusted = true;
                        Debug.Log($"[CAMERA] ✓ Distancia de FreeLook Camera ajustada a {cameraDistanceOffset}m (runtime)");
                    }
                }
            }
            #endif
        }
        
        // Si no es FreeLook, intentar con CinemachineCamera usando SerializedObject
        if (!distanceAdjusted && virtualCamera is CinemachineCamera cam)
        {
            Debug.Log($"[CAMERA] Detectada CinemachineCamera, intentando ajustar distancia a {cameraDistanceOffset}m");
            
            #if UNITY_EDITOR
            SerializedObject so = new SerializedObject(cam);
            
            // Intentar buscar propiedades comunes que controlan la distancia
            string[] possibleProperties = { "m_Body", "m_FollowOffset", "m_Standoff", "m_Radius" };
            
            foreach (string propName in possibleProperties)
            {
                SerializedProperty prop = so.FindProperty(propName);
                if (prop != null)
                {
                    Debug.Log($"[CAMERA] Encontrada propiedad: {propName}, tipo: {prop.propertyType}");
                    
                    // Si es un Vector3, ajustar el componente Z
                    if (prop.propertyType == SerializedPropertyType.Vector3)
                    {
                        Vector3 currentValue = prop.vector3Value;
                        currentValue.z = -cameraDistanceOffset; // Negativo para alejar
                        prop.vector3Value = currentValue;
                        Debug.Log($"[CAMERA] Ajustado {propName}.z de {currentValue.z + cameraDistanceOffset} a {currentValue.z}");
                        distanceAdjusted = true;
                    }
                    // Si es un float (radio), ajustarlo directamente
                    else if (prop.propertyType == SerializedPropertyType.Float)
                    {
                        float oldValue = prop.floatValue;
                        prop.floatValue = cameraDistanceOffset;
                        Debug.Log($"[CAMERA] Ajustado {propName} de {oldValue} a {cameraDistanceOffset}");
                        distanceAdjusted = true;
                    }
                }
            }
            
            // Buscar en los componentes hijos (Body, Aim, etc.)
            SerializedProperty bodyProp = so.FindProperty("m_Body");
            if (bodyProp != null && bodyProp.objectReferenceValue != null)
            {
                SerializedObject bodySo = new SerializedObject(bodyProp.objectReferenceValue);
                SerializedProperty offsetProp = bodySo.FindProperty("m_FollowOffset");
                if (offsetProp != null && offsetProp.propertyType == SerializedPropertyType.Vector3)
                {
                    Vector3 currentOffset = offsetProp.vector3Value;
                    currentOffset.z = -cameraDistanceOffset;
                    offsetProp.vector3Value = currentOffset;
                    bodySo.ApplyModifiedProperties();
                    Debug.Log($"[CAMERA] ✓ Ajustada distancia en Body component: m_FollowOffset.z = {currentOffset.z}");
                    distanceAdjusted = true;
                }
            }
            
            if (distanceAdjusted)
            {
                so.ApplyModifiedProperties();
                Debug.Log($"[CAMERA] ✓ Distancia de CinemachineCamera ajustada exitosamente a {cameraDistanceOffset}m");
            }
            #endif
            
            // Si no se ajustó con SerializedObject, intentar con componentes
            if (!distanceAdjusted)
            {
                MonoBehaviour cameraMono = cam as MonoBehaviour;
                if (cameraMono != null)
                {
                    var bodyComponents = cameraMono.GetComponents<Unity.Cinemachine.CinemachineComponentBase>();
                    Debug.Log($"[CAMERA] Encontrados {bodyComponents.Length} componentes de body");
                    
                    foreach (var bodyComponent in bodyComponents)
                    {
                        if (bodyComponent == null) continue;
                        
                        Debug.Log($"[CAMERA] Componente encontrado: {bodyComponent.GetType().Name}");
                        
                        // Intentar ajustar OrbitalFollow (el más común en Unity 6)
                        if (bodyComponent is Unity.Cinemachine.CinemachineOrbitalFollow orbitalFollow)
                        {
                            Debug.Log($"[CAMERA] Intentando ajustar OrbitalFollow...");
                            
                            #if UNITY_EDITOR
                            SerializedObject orbitalSo = new SerializedObject(orbitalFollow);
                            
                            // Intentar diferentes nombres de propiedades
                            string[] possibleRadiusProps = { "m_Radius", "Radius", "radius", "m_Distance", "Distance", "distance" };
                            
                            foreach (string propName in possibleRadiusProps)
                            {
                                SerializedProperty radiusProp = orbitalSo.FindProperty(propName);
                                if (radiusProp != null)
                                {
                                    Debug.Log($"[CAMERA] Encontrada propiedad '{propName}', tipo: {radiusProp.propertyType}");
                                    
                                    if (radiusProp.propertyType == SerializedPropertyType.Float)
                                    {
                                        float oldRadius = radiusProp.floatValue;
                                        
                                        // SIEMPRE actualizar el radio, incluso si es el mismo valor
                                        // Esto fuerza a la cámara a recalcular su posición
                                        radiusProp.floatValue = cameraDistanceOffset;
                                        
                                        // Forzar actualización inmediata
                                        #if UNITY_EDITOR
                                        UnityEditor.EditorUtility.SetDirty(orbitalFollow);
                                        #endif
                                        
                                        orbitalSo.ApplyModifiedProperties();
                                        
                                        Debug.Log($"[CAMERA] ✓ Distancia ajustada usando OrbitalFollow ({propName}): Radio cambiado de {oldRadius} a {cameraDistanceOffset}m");
                                        distanceAdjusted = true;
                                        
                                        // Ajustar también otros parámetros relacionados si existen
                                        SerializedProperty heightProp = orbitalSo.FindProperty("m_Height");
                                        if (heightProp != null && heightProp.propertyType == SerializedPropertyType.Float)
                                        {
                                            float currentHeight = heightProp.floatValue;
                                            // Aumentar la altura proporcionalmente si es necesario
                                            if (currentHeight < cameraDistanceOffset * 0.3f)
                                            {
                                                heightProp.floatValue = cameraDistanceOffset * 0.3f;
                                                Debug.Log($"[CAMERA] Altura ajustada de {currentHeight} a {heightProp.floatValue}");
                                            }
                                        }
                                        
                                        orbitalSo.ApplyModifiedProperties();
                                        break;
                                    }
                                }
                            }
                            
                            if (!distanceAdjusted)
                            {
                                Debug.LogWarning("[CAMERA] No se encontró ninguna propiedad de radio en OrbitalFollow usando SerializedObject");
                            }
                            #endif
                            
                            // Fallback con reflection - intentar múltiples nombres de campos
                            if (!distanceAdjusted)
                            {
                                string[] possibleFieldNames = { "m_Radius", "Radius", "radius", "m_Distance", "Distance", "distance", "_radius", "_Radius" };
                                var orbitalType = typeof(Unity.Cinemachine.CinemachineOrbitalFollow);
                                
                                foreach (string fieldName in possibleFieldNames)
                                {
                                    var radiusField = orbitalType.GetField(fieldName, 
                                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                    
                                    if (radiusField != null)
                                    {
                                        float oldRadius = (float)radiusField.GetValue(orbitalFollow);
                                        radiusField.SetValue(orbitalFollow, cameraDistanceOffset);
                                        Debug.Log($"[CAMERA] ✓ Distancia ajustada usando OrbitalFollow (reflection, campo '{fieldName}'): Radio cambiado de {oldRadius} a {cameraDistanceOffset}m");
                                        distanceAdjusted = true;
                                        break;
                                    }
                                }
                                
                                if (!distanceAdjusted)
                                {
                                    Debug.LogWarning("[CAMERA] No se encontró ningún campo de radio en OrbitalFollow usando reflection. Intentando propiedades...");
                                    
                                    // Intentar con propiedades públicas
                                    var radiusProp = orbitalType.GetProperty("Radius", 
                                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                    if (radiusProp != null && radiusProp.CanWrite)
                                    {
                                        float oldRadius = (float)radiusProp.GetValue(orbitalFollow);
                                        radiusProp.SetValue(orbitalFollow, cameraDistanceOffset);
                                        Debug.Log($"[CAMERA] ✓ Distancia ajustada usando OrbitalFollow (propiedad 'Radius'): Radio cambiado de {oldRadius} a {cameraDistanceOffset}m");
                                        distanceAdjusted = true;
                                    }
                                }
                            }
                            
                            if (distanceAdjusted)
                            {
                                break;
                            }
                        }
                        
                        // Intentar ajustar ThirdPersonFollow
                        if (bodyComponent is Unity.Cinemachine.CinemachineThirdPersonFollow thirdPerson)
                        {
                            var standoffField = typeof(Unity.Cinemachine.CinemachineThirdPersonFollow).GetField("m_Standoff", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (standoffField != null)
                            {
                                var currentStandoff = (Vector3)standoffField.GetValue(thirdPerson);
                                currentStandoff.z = cameraDistanceOffset;
                                standoffField.SetValue(thirdPerson, currentStandoff);
                                Debug.Log($"[CAMERA] ✓ Distancia ajustada a {cameraDistanceOffset}m usando ThirdPersonFollow");
                                distanceAdjusted = true;
                                break;
                            }
                        }
                        
                        // Intentar ajustar Transposer
                        if (bodyComponent is Unity.Cinemachine.CinemachineTransposer transposer)
                        {
                            var offsetField = typeof(Unity.Cinemachine.CinemachineTransposer).GetField("m_FollowOffset", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (offsetField != null)
                            {
                                var currentOffset = (Vector3)offsetField.GetValue(transposer);
                                currentOffset.z = -cameraDistanceOffset;
                                offsetField.SetValue(transposer, currentOffset);
                                Debug.Log($"[CAMERA] ✓ Distancia ajustada a {cameraDistanceOffset}m usando Transposer");
                                distanceAdjusted = true;
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        if (!distanceAdjusted)
        {
            Debug.LogWarning($"[CAMERA] ⚠ No se pudo ajustar la distancia automáticamente. Tipo: {virtualCamera.GetType().Name}. " +
                           $"Ajusta manualmente en el editor o aumenta 'cameraDistanceOffset' (actual: {cameraDistanceOffset})");
        }
    }
    
    // Método público para actualizar la cámara desde otros scripts
    public void UpdateCameraToActor(CharacterActor actor)
    {
        if (actor == null) return;
        
        Transform cameraTarget = GetCameraTarget(actor.transform);
        if (cameraTarget != null)
        {
            UpdateCameraTarget(cameraTarget);
            Debug.Log($"[CAMERA] Cámara actualizada a {actor.CharacterName}");
            
            // Asegurar que la distancia se ajuste también cuando se actualiza manualmente
            // Esto es especialmente importante al inicio del juego
            StartCoroutine(AdjustCameraDistanceDelayed());
        }
    }
    
    // Coroutine para ajustar la distancia de la cámara después de un frame
    private IEnumerator AdjustCameraDistanceDelayed()
    {
        // Esperar varios frames para asegurar que la cámara esté completamente inicializada
        yield return null;
        yield return null;
        yield return null;
        
        AdjustCameraDistance();
        
        // Ajustar nuevamente después de más tiempo para asegurar que se mantenga
        yield return new WaitForSeconds(0.2f);
        AdjustCameraDistance();
        
        // Un último ajuste después de un poco más de tiempo
        yield return new WaitForSeconds(0.3f);
        AdjustCameraDistance();
    }
}