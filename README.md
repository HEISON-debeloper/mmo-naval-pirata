# MMO Naval Pirata

Juego de piratas en Unity 6 (URP) de **HEISON**: la mecánica de *Assassin's Creed IV: Black Flag*, la historia de *Piratas del Caribe* y la economía de *Albion Online*.
Por ahora es un prototipo con formas simples; se va mejorando por versiones.

## Cómo abrirlo
1. Unity Hub → **Añadir** → elegir esta carpeta. Versión del editor: **6000.3.27f2**.
2. Menú **Pirata → Crear escena (manos vacías)** y luego **Play**.
3. En el juego, **F1** muestra todos los controles y la versión.

## Versiones
- Formato `0.X.Y`: la **X** sube con cada cosa nueva (una mecánica, un sistema); la **Y** con arreglos pequeños.
- Al tener la primera versión completa para publicar, pasará a `1.0.0`.
- El número está en `Assets/Scripts/Version.cs` y en la ayuda del juego (F1).
- Cada versión con código tiene su rama en GitHub (`versiones/v0.17.0`, `versiones/v0.18.0`, …) y su explicación en [CHANGELOG.md](CHANGELOG.md). La rama `main` siempre es la última.
- Para volver a una versión vieja: en GitHub, botón de ramas (arriba a la izquierda, dice `main`) → elegir `versiones/…` → **Code → Download ZIP**.

## Carpetas
- `Assets/Scripts` — todo el código del juego (en español).
- `Assets/Scripts/Editor/ConstructorEscena.cs` — arma la escena completa desde el menú Pirata.
- `Assets/Materiales` — colores de los objetos.
- `prototipo-web/index.html` — la primera versión (0.1.0), jugable en el navegador.
