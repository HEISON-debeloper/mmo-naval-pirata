# Historial de versiones

Cada versión del juego queda guardada aquí y en GitHub, en la rama `versiones/vX.Y.Z` (por ejemplo `versiones/v0.18.0`).

> Las versiones 0.2.0 a 0.16.0 se reconstruyeron de memoria al crear este repositorio (las horas son aproximadas, hora de Colombia).
> El código de esas versiones no se guardó por separado: todo lo que traían está dentro de la versión 0.17.0.
> La 0.1.0 (prototipo web) sí tiene su código original. Desde la 0.17.0, cada versión tiene su propio código en GitHub.

## 0.24.0 — Interiores  (2026-11-08)
- Interiores: en cubierta, F junto a la escotilla baja por una escalera a las cubiertas de cañones (una por cada fila de portas) y, en cascos hondos, a la bodega. F al pie de la escalera sube.
- Cubiertas de cañones por dentro: los cañones sobre su cureña con ruedas, las portas abiertas por donde entra la luz, pilas de balas, vigas, faroles con luz cálida y los mástiles que bajan hasta el fondo.
- Bodega con barriles, cajas y sacos.
- Camarote del capitán dentro del castillo de popa: F frente a la puerta. Mesa con mapa, sillas, cama, cofre, alfombra, farol y las ventanas de popa.
- Adentro la cámara pasa a primera persona y se mueve con el barco.
- Todo el interior se arma la primera vez que pisas un barco y solo se muestra cuando estás abajo, para no gastar rendimiento.
- La primera fila de portas baja 40 cm para que quepa una persona de pie en la cubierta de cañones.
- Al soltar el timón quedas a un lado (al frente está la puerta del camarote).
- El foque y la cangreja recogidos ya no se ven como una tabla.

## 0.23.0 — Proa y popa de guerra  (2026-11-08)
- Espolón de bronce en la proa (modelado en Blender): si embistes yendo hacia adelante, el otro barco recibe mucho más daño (más si eres más grande) y el tuyo casi nada.
- Dos cañones de proa en cubierta, uno a cada lado, que solo disparan al frente.
- Barriles en llamas en la popa (Tab → Cañones y bodega): mira atrás y haz clic para soltar uno; flota y explota si lo toca otro barco o una bala, y hace explotar a los barriles cercanos.
- Velas realistas: cada mástil lleva de 1 a 4 velas una encima de otra según su altura, más bauprés con foque en la proa y cangreja en el mástil de popa. Todo se adapta al largo, ancho y alto del barco.
- Recarga según los cañones: cada cañón más en un lado alarga su recarga un 4,5 % (10 cañones ≈ 1,4 veces; 64 ≈ 3,8 veces).
- Cañones según el tamaño: cada vez que alargas el barco crece 3 m (una estación) y lleva exactamente un cañón más por costado en cada piso. Al subir un piso, cada piso lleva los cañones que le caben según la forma del casco.
- Portas en el casco: los cañones de los pisos asoman por huecos con marco y tapa abierta, fila por fila. Los barcos de casco bajo llevan los cañones en cubierta, con cureña.
- Las anclas van delante de las portas.

## 0.22.0 — Barcos con forma  (2026-11-08)
- Cascos de verdad: proa en punta, fondo curvo, popa plana con ventanas y borda (baranda) alrededor de la cubierta. Se arman por código y se adaptan a los 15 moldes y a todos los tamaños.
- Velas curvas, infladas por el viento.
- Piezas modeladas en Blender: cañón con cureña y ruedas, timón de rueda, anclas, farol de popa que brilla, barril y 4 mascarones de proa (calavera, sirena, dragón, león) para el taller que viene.
- Las balsas también llevan los cañones con cureña.
- Los archivos de Blender quedan en la carpeta Blender del proyecto.

## 0.21.0 — Pintura del barco  (2026-11-08)
- Nueva página Pintura en el menú Tab (a bordo de tu barco): color del casco, de las velas y de la bandera, 12 colores.
- Cada cambio cuesta 5 de oro (gratis en creativo); "Colores originales" quita la pintura sin costo.
- La pintura se conserva al cambiar de molde o tamaño y al guardar la partida.

## 0.20.0 — Abordajes piratas  (2026-11-08)
- Algunos piratas (4 de cada 10) van directo a abordarte: se pegan a tu barco y sus piratas saltan a tu cubierta, hasta 3 a la vez.
- Peleas con la espada; si cae toda su tripulación, su barco queda capturado y su botín va a tu mochila.
- Arreglo: los piratas sobre una balsa ya no se traban con un camarote que no existe y llegan hasta ti.
- Tecla de prueba V: el pirata más cercano viene a abordarte.

## 0.19.0 — Mercados en el mapa  (2026-11-08)
- Las islas con mercader salen con una moneda dorada en la brújula y en el mapa (M), y el aviso al descubrirlas lo dice.
- La partida guardada recuerda qué islas tienen mercader.

## 0.18.0 — Oro y mercaderes  (2026-11-08)
- Moneda nueva: oro. Los piratas traen oro en su botín.
- Mercaderes en la playa: siempre en la isla inicial y en una de cada tres islas. Junto a ellos, Tab abre Vender y Comprar.
- Cada isla tiene sus precios (como en Albion): lo que es barato en una se vende caro en otra. Vender mucho de lo mismo baja el precio un rato.
- Tecla de prueba N: ir al mercader más cercano.

## 0.17.0 — Sistema de versiones  (2026-11-08)
- El proyecto completo de Unity queda guardado en GitHub; cada versión nueva lleva su etiqueta y su código.
- Número de versión en el juego (Version.cs), visible en la ayuda F1.
- Este historial (CHANGELOG.md) con todas las versiones desde la primera.

## 0.16.0 — Botín  (2026-11-08)
- Al hundir un pirata suelta cajas que flotan y se recogen solas al pasar cerca.
- Al capturarlo, todo su botín va a la mochila. Tecla L (prueba): ir a la caja más cercana.

## 0.15.0 — Guardado de partida  (2026-11-08)
- Se guarda solo cada 30 s y al salir; F5 guarda y F8 borra.
- Guarda posición, vida, hambre, sed, mochila, tu barco completo, hornos e islas descubiertas.

## 0.14.0 — Cámara de apuntado estilo Black Flag  (2026-11-08)
- Al apuntar la cámara queda junto a ti en el timón mirando al costado y solo se inclina con el alcance.
- Se quitó el bergantín que aparecía junto a la isla inicial.

## 0.13.0 — Optimización  (2026-11-08)
- Lo pequeño deja de dibujarse de lejos, sombras solo cerca y los piratas lejanos duermen (sin física ni IA).

## 0.12.0 — Piratas con moldes  (2026-11-08)
- Los piratas usan la misma lógica de moldes y tamaños que tu barco: desde balsas con cañones cerca del inicio hasta navíos lejos.
- Zonas de peligro que suben con la distancia; zona segura de 450 m alrededor del inicio.

## 0.11.0 — Mundo infinito  (2026-11-08)
- Islas generadas sin fin alrededor de ti, siempre iguales (misma semilla), con nombres piratas.

## 0.10.0 — Brújula y mapa  (2026-11-08)
- Brújula arriba de la pantalla con islas, tu barco y enemigos cercanos.
- Mapa con M (zoom, arrastrar, volver a ti); las islas se descubren al acercarse.

## 0.9.0 — Arreglos de puntería y calidad de vida  (2026-11-07)
- El fantasma ya no se dispara con muchos cañones; la cámara se engancha solo al mirar hacia la zona de caída.
- Velas proporcionales a cualquier tamaño de barco.
- Página Creativo con atajos de prueba y mejoras en los menús (Shift ×5, faltantes en rojo, "alcanza para N").

## 0.8.0 — Moldes históricos y tamaños  (2026-11-07)
- 15 moldes de barco reales: balsa, bote, chalupa, pinaza, cúter, balandro, goleta, bergantín, corbeta, fragata, galeón, navíos de cuarta, de línea, de primera y de cuatro puentes.
- Tamaño aparte del molde: alargar, ensanchar y subir pisos (hasta 10 cada uno); el molde siguiente cuesta más si el barco es grande.
- Cañones siempre del mismo tamaño, timón a popa en todos los barcos, zoom desde el timón hasta ver el barco pequeño.
- Las velas se vuelven transparentes si tapan la vista.

## 0.7.0 — La balsa crece, puntería y modo creativo  (2026-11-07)
- La balsa es tu barco desde el principio y se mejora; cañones en la balsa (uno o ambos lados).
- F recoge a mano, clic izquierdo golpea con hacha o pico.
- Puntería tipo Black Flag con clic derecho: una barra con un cañón, una cinta que se abre con varios.
- Modo creativo (F2): todo gratis y sin daño, para probar.

## 0.6.0 — Interfaz ordenada  (2026-11-07)
- HUD único sin nada encimado: vida/hambre/sed, mochila, tarjeta del barco, enemigo cercano, avisos, ayuda F1.
- La playa quedó 2 m sobre el mar y las olas se calman junto a las islas (el agua ya no inunda el pasto).

## 0.5.0 — Fabricación y mejoras  (2026-11-07)
- Menú de fabricación (Tab): cuerda, hacha, pico, horno de piedra, fundir hierro, cañones, balsa.
- Mejoras del barco por piezas (casco y velas) y bodega para llevar comida.

## 0.4.0 — Islas y supervivencia  (2026-11-07)
- Islas con palmas, rocas, fibra, ramas, piedras, arbustos de frutas y cocos.
- Inventario, hambre y sed; nadar; empezar en una isla sin barco y armar una balsa.

## 0.3.0 — Personaje y abordaje  (2026-11-07)
- El pirata camina por la cubierta del barco aunque el barco se mueva, toma y suelta el timón con F.
- Tripulantes enemigos con espada; abordaje y captura del barco cuando cae su tripulación.

## 0.2.0 — Proyecto en Unity  (2026-11-07)
- Proyecto Unity 6 (URP) con la escena armada por código desde el menú Pirata.
- Mar con olas, flotación física de los barcos, balandro del jugador, cámara en tercera persona.
- Cañones por costado (Q/E) y un bergantín enemigo con IA que patrulla, se pone de costado y dispara.

## 0.1.0 — Prototipo web  (2026-11-07)
- Primer prototipo jugable en el navegador (prototipo-web/index.html): un barco en un mar 3D con olas, velas, timón y cañones.
- Sirvió para probar la idea antes de pasar a Unity.
