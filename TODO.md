# Tareas Pendientes (TODO)

## 1. Metodología de Red y Análisis de Riesgos en RepoKit
- [x] **Incorporar Checklist de Arquitectura de Red y Autoridad a RepoKit:**
  - Agregar sección en `REPO_MODS_METHODOLOGY.md` para evaluar antes de programar si un mod puede ser puramente Host-Only o si requiere instalación en el cliente.
  - Guía de 4 preguntas de validación:
    1. *¿Quién procesa el Input (pulsación de teclas)?* (Detección de `heldByLocalPlayer` y acciones locales).
    2. *¿Existe sincronización nativa en REPO (RPC/NetworkView)?* (Física/vida/daño vs Shaders/colores de rayo/materiales locales).
    3. *¿La máquina de estados del objeto base es binaria (ON/OFF) o extensible?* (Evitar rebotes de toggle).
    4. *¿Qué llamadas automáticas del juego base intervienen?* (Inventario, desequipamiento, soltar objeto, temporizadores de batería).
  - Clasificación estándar de proyectos modding de REPO:
    - **Categoría 1 (Host-Only):** Spawns de objetos vanilla, cambios de IA/mundo, toggles nativos.
    - **Categoría 2 (Client-Synced):** Nuevas teclas/keybinds, nuevos modelos 3D/assets, UIs y HUDs personalizados.
    - **Categoría 3 (Híbrido Asimétrico / Graceful Degradation):** Jugabilidad y mecánicas 100% funcionales en Host-Only con fallback visual; efectos audiovisuales completos en clientes con mod.

## 2. Ajustes en ReversibleBatteryDrone
- [x] **Etiqueta dinámica en mano (In-hand prompt label):**
  - Modificar el texto mostrado en pantalla cuando el jugador sostiene el dron (`ItemAttributes` / `ItemToggle`).
  - En lugar de mostrar siempre `"Recharge Droid (E)"`, reflejar dinámicamente el modo activo o el siguiente modo al encender (ej. `"Drain Droid (E)"` o `"Recharge Droid (E)"`).
