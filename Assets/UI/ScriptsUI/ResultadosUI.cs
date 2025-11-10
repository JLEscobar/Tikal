using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[ExecuteAlways]
public class ResultadosUI : MonoBehaviour
{
    [System.Serializable]
    public class Objetivo
    {
        [Header("Configuración")]
        public string nombre = "Objetivo";
        public bool cumplido = false;
        public int puntos = 0;

        [Header("Elementos a colorear para este objetivo")]
        [Tooltip("Arrastra aquí los elementos (Image, SVGImage, TextMeshProUGUI, etc.) que deben volverse NARANJA si NO se cumple este objetivo.")]
        public Graphic[] elementos = new Graphic[0];

        [Header("Texto que muestra los puntos (solo número)")]
        public TextMeshProUGUI textoPuntos; // mostrará "0" o "30" etc.
    }

    [Header("Objetivos (configura desde Inspector)")]
    public Objetivo objetivoPrincipal = new Objetivo { nombre = "Principal", puntos = 30 };
    public Objetivo objetivoSecundario = new Objetivo { nombre = "Secundario", puntos = 15 };
    public Objetivo objetivoOculto = new Objetivo { nombre = "Oculto", puntos = 10 };

    [Header("Textos generales")]
    public TextMeshProUGUI tituloPrincipal;         // texto grande: Nivel Completado / Nivel Fallido
    public TextMeshProUGUI textoExperienciaTotal;   // mostrará solo el número (ej. "45")

    [Header("Colores (editable)")]
    [Tooltip("Color cuando un objetivo está cumplido (si quieres que se pongan así)")]
    public Color colorExito = new Color32(0x59, 0xF7, 0xDD, 0xFF); // #59F7DD
    [Tooltip("Color que se aplicará cuando un objetivo NO esté cumplido")]
    public Color colorFallo = new Color32(0xFF, 0x75, 0x4A, 0xFF); // #FF754A

    [Header("Qué mostrar si gana / si pierde")]
    public GameObject[] mostrarSiGana;
    public GameObject[] mostrarSiPierde;

    // Guardamos colores originales por objetivo (para restaurar)
    private Color[][] coloresOriginalesPorObjetivo;

    void OnEnable()
    {
        GuardarColoresOriginales();
        AplicarEstado();
    }

    void Start()
    {
        GuardarColoresOriginales();
        AplicarEstado();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Cuando cambias checkboxes en Inspector, actualiza en Editor inmediatamente
        GuardarColoresOriginales();
        AplicarEstado();
    }
#endif

    void GuardarColoresOriginales()
    {
        Objetivo[] objetivos = { objetivoPrincipal, objetivoSecundario, objetivoOculto };
        coloresOriginalesPorObjetivo = new Color[objetivos.Length][];

        for (int i = 0; i < objetivos.Length; i++)
        {
            var elems = objetivos[i].elementos;
            if (elems == null) elems = new Graphic[0];
            coloresOriginalesPorObjetivo[i] = new Color[elems.Length];

            for (int j = 0; j < elems.Length; j++)
            {
                if (elems[j] != null)
                    coloresOriginalesPorObjetivo[i][j] = elems[j].color;
                else
                    coloresOriginalesPorObjetivo[i][j] = Color.white;
            }
        }
    }

    /// <summary>
    /// Aplica los colores, textos y visibilidades según los checks en el inspector.
    /// </summary>
    public void AplicarEstado()
    {
        // 1) Actualizar cada objetivo por separado (si NO se cumple -> naranja, si sí -> restaurar o colorExito)
        ActualizarObjetivoUI(objetivoPrincipal, 0);
        ActualizarObjetivoUI(objetivoSecundario, 1);
        ActualizarObjetivoUI(objetivoOculto, 2);

        // 2) Calcular XP total (suma de puntos de objetivos cumplidos)
        int totalXP = 0;
        if (objetivoPrincipal.cumplido) totalXP += objetivoPrincipal.puntos;
        if (objetivoSecundario.cumplido) totalXP += objetivoSecundario.puntos;
        if (objetivoOculto.cumplido) totalXP += objetivoOculto.puntos;

        if (textoExperienciaTotal != null)
            textoExperienciaTotal.text = totalXP.ToString(); // SOLO número (sin "XP")

        // 3) Título según objetivo principal
        bool nivelGanado = objetivoPrincipal.cumplido;
        if (tituloPrincipal != null)
        {
            tituloPrincipal.text = nivelGanado ? "Nivel Completado" : "Nivel Fallido";
            tituloPrincipal.color = nivelGanado ? colorExito : colorFallo;
        }

        // 4) Mostrar/ocultar objetos de ganancia/derrota
        SetActiveArray(mostrarSiGana, nivelGanado);
        SetActiveArray(mostrarSiPierde, !nivelGanado);
    }

    void ActualizarObjetivoUI(Objetivo obj, int objetivoIndex)
    {
        // colorDestino: si cumplido = colorExito (opcional), si NO cumplido = colorFallo
        Color colorDestino = obj.cumplido ? colorExito : colorFallo;

        // Aplicar color a cada elemento de ese objetivo
        if (obj.elementos != null)
        {
            for (int i = 0; i < obj.elementos.Length; i++)
            {
                var el = obj.elementos[i];
                if (el == null) continue;

                // Aplica color destino
                el.color = colorDestino;
            }
        }

        // Actualizar el texto de puntos del objetivo (solo número)
        if (obj.textoPuntos != null)
        {
            obj.textoPuntos.text = obj.cumplido ? obj.puntos.ToString() : "0";
        }
    }

    void SetActiveArray(GameObject[] arr, bool activo)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] != null)
                arr[i].SetActive(activo);
        }
    }

    // Métodos públicos para cambiar desde otros scripts
    public void SetPrincipal(bool val) { objetivoPrincipal.cumplido = val; AplicarEstado(); }
    public void SetSecundario(bool val) { objetivoSecundario.cumplido = val; AplicarEstado(); }
    public void SetOculto(bool val) { objetivoOculto.cumplido = val; AplicarEstado(); }

    // Resetea todo
    public void ResetResultados()
    {
        objetivoPrincipal.cumplido = false;
        objetivoSecundario.cumplido = false;
        objetivoOculto.cumplido = false;
        AplicarEstado();
    }
}
