# Battle Tanks Last Standing

**Battle Tanks Last Standing** es un videojuego de combate multijugador online diseñado para dispositivos móviles y entornos de red dinámicos. Los jugadores toman el control de tanques con estética *arcade/caricaturesca*, enfrentándose en arenas cerradas estratégicas donde el uso del entorno, la precisión y la supervivencia son la clave para la victoria.

El proyecto está desarrollado sobre **Unity 6 (6000.6.0f1)** e implementa una arquitectura multijugador cliente-servidor nativa, escalable y containerizada, con soporte tanto para GameObjects como para ECS/Entities.

---

## Descripción del Juego

En **Battle Tanks Last Standing**, cada jugador controla un tanque personalizable (con colores distintivos, sombreros y accesorios) en partidas competitivas en tiempo real.

* **Objetivo:** Sobrevivir a los oponentes eliminando los tanques rivales mediante proyectiles y tácticas defensivas.
* **Escenarios:** Arenas rectangulares equipadas con cobertura dinámica (muros, barreras y estructuras destructibles) que permiten planificar emboscadas o protegerse de los ataques.
* **Estilo Visual:** Arte cartoon/arcade vibrante con efectos visuales (*VFX*) para disparos, explosiones e impactos en el escenario.
* **Plataforma Objetivo:** Dispositivos móviles (iOS / Android) con controles táctiles optimizados.

---

## Arquitectura Técnica y Stack Tecnológico

El proyecto utiliza las últimas tecnologías de red y renderizado dentro de Unity para garantizar rendimiento y baja latencia:

* **Motor:** Unity `6000.6.0f1`
* **Render Pipeline:** Universal Render Pipeline (URP) `17.6.0`
* **Sistema de Inputs:** Unity Input System `1.20.0`
* **Red y Multijugador:**
  * **Netcode for GameObjects (NGO) `2.13.3`:** Implementación orientada a objetos para lógica rápida de cliente/servidor.
  * **Netcode for Entities (NFE) / DOTS `6.6.0`:** Arquitectura de datos optimizada (ECS) para alta densidad de entidades (proyectiles, tanques, partículas) con predicción en cliente y reconciliación de servidor.
  * **Protocolo de Red:** UDP / UnityTransport en puertos dedicados (`7979` / `7980`).
* **Infraestructura de Servidores:**
  * Servidor dedicado en **Linux Server Build** (x86-64).
  * Envasado y despliegue mediante **Docker**, **Kubernetes** y orchestración con **Agones**.

---

## Controles y Gameplay

### Entorno Local / Pruebas
| Acción | Jugador 1 | Jugador 2 | Jugador 3 | Jugador 4 |
| :--- | :--- | :--- | :--- | :--- |
| **Movimiento / Apuntado** | W A S D | Flechas | I J K L | Numpad 8, 4, 5, 6 |
| **Disparo / Habilidad** | Espacio | Ctrl Derecho | O | Numpad 0 |

### Dispositivos Móviles
* **Joystick Izquierdo:** Desplazamiento y rotación del chasis del tanque.
* **Joystick Derecho / Botones:** Apuntado de la torreta y disparo de proyectiles.

---

## Estructura del Proyecto

```text
Assets/
 ├── Scenes/
 │    ├── LocalGameplayBaseline.unity  # Escena offline/local para pruebas de física y controles.
 │    ├── NGOGameplay.unity             # Escena multijugador basada en Netcode for GameObjects.
 │    └── NFEGameplay.unity             # Escena multijugador masiva basada en Netcode for Entities (ECS).
 ├── Prefabs/                           # Prefabs de tanques, proyectiles, muros y cosméticos.
 ├── Scripts/                           # Lógica de red, sistemas ECS, RPCs y validación del servidor.
 └── Infrastructure/                    # Dockerfiles, manifiestos de Kubernetes y scripts de Agones.
```

---

## Primeros Pasos

### Requisitos Previos
* **Unity 6000.6.0f1** con módulos para Linux Dedicated Server Build e iOS/Android.
* **Git** para la gestión de código fuente.
* Enorno **Docker / Kubernetes / Agones** (opcional para despliegue de servidores dedicados en la nube).

### Ejecución en Local
1. Clona este repositorio:
   ```bash
   git clone https://github.com/tu-usuario/tank-arena-multiplayer.git
   ```
2. Abre el proyecto en **Unity Hub** con la versión `6000.6.0f1`.
3. Navega a `Assets/Scenes/LocalGameplayBaseline.unity` y presiona **Play** para probar la mecánica básica de tanques y disparos.
4. Para probar la conectividad en red, abre `NGOGameplay.unity` o `NFEGameplay.unity`, ajusta la dirección IP/Puerto en la interfaz e inicia una instancia en modo **Host / Server / Client**.

---

## Despliegue de Servidores Dedicados

El proyecto incluye archivos de configuración listos para la automatización e infraestructura:

1. **Build del Servidor:** Generar una Build desde Unity seleccionando la plataforma **Dedicated Server (Linux)**.
2. **Contenedor:** Ejecutar la imagen de Docker utilizando el `Dockerfile` incluido en la raíz.
3. **Orquestación:** Utilizar los archivos de manifiesto para desplegar los pods en un clúster de **Kubernetes** administrado por **Agones**.