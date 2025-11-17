using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[Serializable]
public class PersonajeData
{
    [Header("Datos básicos")]
    public string nombre = "Nombre";
    public string rol = "Rol";
    public int numero = 1;

    [Header("Imagen / retrato")]
    public Sprite retrato;

    [Header("Barra de vida")]
    [Tooltip("Prefab (GameObject) que contiene la barra de vida (por ejemplo un empty con 'BarraDeVidaSVG' como componente).")]
    public GameObject prefabBarraVida;

    [Header("Vida (valores iniciales -- se pueden mantener también en el prefab)")]
    public float vidaActual = 300;
    public float vidaMaxima = 300;

    [Header("Habilidades (GameObject que se activará si este personaje está como principal)")]
    public GameObject habilidadesGO;
}

