# Cómo funciona el checkbox "cumplido" en ResultadosUI

## Componentes clave:

### 1. **Clase Serializable `Objetivo`**
```csharp
[System.Serializable]
public class Objetivo
{
    public bool cumplido = false;  // ← Este es el checkbox que ves en el Inspector
    public int puntos = 0;
    // ...
}
```
- `[System.Serializable]` permite que Unity muestre esta clase en el Inspector
- `public bool` se muestra automáticamente como un checkbox

### 2. **`[ExecuteAlways]` (línea 9)**
```csharp
[ExecuteAlways]
public class ResultadosUI : MonoBehaviour
```
- Hace que el script se ejecute TANTO en Play Mode como en Edit Mode
- Permite ver cambios en tiempo real sin necesidad de ejecutar el juego

### 3. **`OnValidate()` (líneas 62-69)**
```csharp
#if UNITY_EDITOR
void OnValidate()
{
    GuardarColoresOriginales();
    AplicarEstado();  // ← Se llama cada vez que cambias algo en el Inspector
}
#endif
```
- Se ejecuta automáticamente cuando cambias CUALQUIER valor en el Inspector
- Solo compila en el Editor (no en builds)
- Llama a `AplicarEstado()` que actualiza toda la UI

### 4. **`AplicarEstado()` (líneas 95-122)**
Este método:
- Lee el valor de `cumplido` de cada objetivo
- Aplica colores según si está cumplido o no
- Actualiza textos de puntos
- Muestra/oculta GameObjects según victoria/derrota
- Calcula la experiencia total

## Flujo completo:

```
1. Usuario marca/desmarca checkbox "cumplido" en Inspector
   ↓
2. Unity detecta el cambio → llama OnValidate()
   ↓
3. OnValidate() → GuardarColoresOriginales() + AplicarEstado()
   ↓
4. AplicarEstado() lee el nuevo valor de cumplido
   ↓
5. Actualiza colores, textos y visibilidad INMEDIATAMENTE
   ↓
6. Ves los cambios en tiempo real en la Scene View (gracias a ExecuteAlways)
```

## Por qué funciona tan bien:

- ✅ **Serializable**: Unity puede mostrar la clase en el Inspector
- ✅ **ExecuteAlways**: Se ejecuta en Edit Mode, ves cambios sin Play
- ✅ **OnValidate**: Detecta cambios automáticamente
- ✅ **Public bool**: Se muestra como checkbox nativo de Unity

