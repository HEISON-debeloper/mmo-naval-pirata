# Historial de versiones

Cada versión del juego queda guardada aquí y en GitHub, en la rama `versiones/vX.Y.Z` (por ejemplo `versiones/v0.18.0`).

> Las versiones 0.2.0 a 0.16.0 se reconstruyeron de memoria al crear este repositorio (las horas son aproximadas, hora de Colombia).
> El código de esas versiones no se guardó por separado: todo lo que traían está dentro de la versión 0.17.0.
> La 0.1.0 (prototipo web) sí tiene su código original. Desde la 0.17.0, cada versión tiene su propio código en GitHub.

## 0.37.0 — Arreglos de la auditoría  (2026-11-09)
- El mar no entra en el barco (comprobado bajando por las 4 cubiertas de cañones y la bodega del navío de 4 puentes).
- Tecla de prueba Z: baja un nivel dentro del barco donde estás (solo en el editor).
- Taller: Cancelar ya no te regala el mascarón que probaste; en el teléfono el taller ya no se queda abierto si pierdes el barco.
- Después de repintar, las velas que se transparentan para no tapar la vista salen con el color nuevo.
- El HUD ya no rehace las herramientas 8 veces por segundo (menos trabajo en el teléfono).
- Los nombres de las teclas salen como en tu teclado (Ñ, ´, +…) y no con los del teclado de EE. UU.
- Teléfono: la zona segura (el notch) se recalcula al girar el teléfono, y Opciones ya no muestra la pestaña Controles (no hay teclado y se quedaba bloqueada).
- Escribir en un buscador ya no deja el teclado del juego bloqueado al cerrar la ventana; Esc suelta el buscador.
- Un sonido nuevo ya no corta a otro que sigue sonando: busca un canal libre.
- Un recurso pequeño agotado (invisible) ya no tapa al de al lado.
- Un barril de llamas estalla también si el barco ya lo estaba tocando cuando se armó.
- El espolón solo te protege al embestir barcos, no al chocar contra rocas.
- Los piratas que saltaron a tu barco desaparecen con su barco (ya no quedan huérfanos).
- Bajo cubierta, los muebles y la escalera ya no te empujan dentro de la pared.

## 0.36.0 — Mundo que recuerda  (2026-11-09)
- El mundo recuerda: si hundes o capturas al pirata de una zona, al volver no aparece otro (se acabó el farmeo infinito). Se guarda con la partida.
- Los piratas hundidos del mundo se van al fondo en vez de reaparecer en el mismo sitio.
- Un pirata que te persigue en combate ya no desaparece al cruzar de zona: te sigue (y se va solo si lo dejas a más de 1,5 km).
- Un barco capturado que dejas atrás ya no desaparece.
- Cada mercado recuerda cuánto le vendiste o compraste aunque te alejes, y se recupera con el tiempo como antes; también se guarda con la partida (antes alejarse y volver reiniciaba los precios).

## 0.35.0 — Barcos livianos y secos  (2026-11-09)
- El mar ya no entra en los barcos: el agua tiene su propio shader que no se dibuja dentro del casco de los barcos cercanos (hasta 8). Antes se veía el agua por las portas, bajo cubierta y en la bodega, como si el barco se estuviera inundando. Un barco que se hunde sí se llena.
- La fila de portas más baja sube 20 cm: queda unos 60 cm sobre el agua en calma.
- Barcos livianos: el tubo del cañón pasa de 2.046 a 148 vértices (un navío de 4 puentes llevaba más de 300 mil vértices solo en tubos).
- Las cureñas de los cañones de adentro solo se dibujan cuando estás bajo cubierta (antes cientos de piezas ocultas se dibujaban siempre).
- Máximo 2 luces de verdad por cubierta (con alcance mayor); los faroles se siguen viendo todos.
- Las mallas hechas para cada barco (jarcia, detalles, tablones, portas, interior) se destruyen cuando el barco desaparece: en viajes largos la memoria ya no crece sin parar.

## 0.34.0 — Combate justo e imagen limpia  (2026-11-09)
- Imagen limpia: suavizado de bordes (MSAA) activado — 4x en PC y 2x en teléfono — y elegible en Opciones → Gráficos (desactivado, 2x, 4x, 8x). La jarcia, las juntas de cubierta y los cantos del casco ya no se ven como líneas que titilan a lo lejos.
- Sin rayas de sombra sobre cascos y velas (sesgo de sombras más alto).
- Las juntas de la cubierta se despegaron más de la tabla (titilaban con ella de lejos) y se dejan de dibujar a más de 180 m.
- Bajo cubierta, la cámara en primera persona también acorta su distancia lejana: con el plano cercano tan corto, las superficies parpadeaban por falta de precisión.
- Los piratas atacan el barco en el que vas, también si es uno capturado (antes navegar un barco capturado te hacía invisible para ellos).
- Los piratas apuntan sus cañones según la distancia, como el jugador (antes disparaban casi planos y sus balas no llegaban a 100 m).
- Morir cuesta: se cae la mitad de lo que llevas (menos el hacha y el pico) en una caja donde caíste, que puedes volver a buscar, y despiertas con hambre y sed a la mitad.
- Si tu barco se hunde, vuelve a flote sin la carga de la bodega y sin los piratas que te habían abordado.
- Si el barco se hunde estando bajo cubierta, sales por arriba (antes quedabas atrapado dentro del casco).
- Bajo cubierta los piratas ya no te golpean a través del piso.
- Todos tus barcos recargan los cañones en 2,5 s, como la balsa (los construidos recargaban en 3,6 s).
- La balsa aguanta el doble (80): ya no se hunde de un solo barril o espolonazo.
- El barco que se hunde baja igual a cualquier velocidad de cuadros (la fuerza va con la física).

## 0.33.0 — Partidas seguras  (2026-11-09)
- "Nueva partida" funciona: antes recargaba el mundo y se perdían la interfaz, el HUD, el guardado y los controles táctiles. Ahora se vuelven a crear solos al recargar.
- "Nueva partida" ya no borra tu partida: la aparta, y en el menú principal aparece "Recuperar la partida anterior" para volver a ella (se puede cambiar de una a otra).
- Guardado más seguro: cada guardado deja una copia de respaldo; si la partida sale dañada se carga la copia (con aviso) y la dañada se guarda aparte en vez de pisarse. Un guardado cortado a mitad se recupera.
- En el teléfono se guarda al salir de la app (antes solo al cerrar el juego: se podían perder hasta 30 s).
- Teclas de prueba y modo creativo solo existen en el editor o en una compilación de desarrollo (Development Build); en el juego publicado no se pueden usar (antes daban oro infinito).
- La tecla de prueba "barco 800 m más lejos" pasa de K a U: la K es la espada y un espadazo al timón teletransportaba el barco.
- Pausa de verdad: Opciones, Controles, Bitácora y Salir abiertos desde la pausa mantienen el juego detenido, y al cerrarlos se vuelve a la pausa.
- Con el menú principal abierto no da hambre ni sed y los piratas no atacan.
- Tab (fabricar o mercado) no se abre encima de la pausa, del menú principal ni de un diálogo.

## 0.32.2 — Velas con sonido  (2026-11-09)
- Sonido de las velas: al soltarlas se oye la lona desplegándose y llenándose de viento; al recogerlas, el roce de la tela con el crujir de la jarcia. Suena en tu barco y en los demás (de lejos más bajo). Grabaciones CC0 de OpenGameArt.

## 0.32.1 — Cañonazos con fuerza  (2026-11-09)
- Cañonazos mucho más fuertes y graves: el sonido se reforzó con una capa grave y ahora se oye a todo volumen hasta unos 35 m (antes se apagaba a los 7 m y sonaba como un barco lejano). Impactos, balas al agua, explosiones, choques y hundimientos también se oyen más lejos.
- Todos los efectos normalizados al mismo nivel (los pasos sonaban casi mudos).
- Más canales de sonido a la vez (48) para que una andanada completa no corte los demás sonidos.

## 0.32.0 — Portas abiertas y sonido real  (2026-11-09)
- Portas de cañón con huecos de verdad en el casco: el casco se arma con las aberturas cortadas, con jambas de madera del grosor del costado. Desde afuera se ve el cañón dentro de un hueco oscuro; desde la cubierta de cañones se ve el mar por la porta.
- Sonidos reales (grabaciones CC0 de OpenGameArt, créditos en Resources/Audio/CREDITOS.txt): cañonazos, impactos en el casco, balas que caen al agua, choques y espolonazos, barcos que se hunden.
- Espada al aire y choque de espadas (tuya y de la tripulación enemiga), hacha en las palmas, pico en las rocas, palma que cae, comer.
- Pasos en madera y en arena, chapuzón al caer al agua, brazadas al nadar, cadena del ancla, crujido de la jarcia al soltar velas.
- Ambiente: viento (más fuerte navegando rápido, casi nada bajo cubierta), agua contra el casco, olas en la orilla, gaviotas cerca de las islas, crujidos de la madera a bordo y el fuego de la cocina bajo cubierta.
- Interfaz: fabricar suena a martillo, comprar/vender a monedas y la madera a madera real.
- Opciones → Audio: nuevos volúmenes de Efectos y de Ambiente.

## 0.31.1 — Cocina sólida y cámara táctil  (2026-11-09)
- El fogón y la mesa de la cocina son sólidos: el pirata los rodea en vez de atravesarlos.
- En primera persona (bajo cubierta) la espada ya no tapa media pantalla: solo aparece al golpear.
- Pegado a las paredes del casco ya no se ve el mar ni el cielo a través de ellas (la cámara recorta más cerca en primera persona).
- Teléfono: arrastrar el dedo en la mitad derecha de la pantalla gira la cámara.

## 0.31.0 — Barco detallado II  (2026-11-09)
- Tablones de cubierta: juntas oscuras a lo largo y cortes escalonados, siguiendo la forma real del casco (se cierran en la proa). Se adaptan a cualquier tamaño.
- Bote salvavidas: bote de remos boca abajo sobre dos calzos, con quilla, regala y remos, a un costado de la cubierta entre mástiles; solo en barcos de 16 m o más. Es sólido (no se atraviesa).
- Bitácora junto al timón: pedestal de madera con cúpula de bronce de la brújula y las dos esferas compensadoras.
- Catalejo de latón apoyado en la baranda de la toldilla.
- Cocina en la proa de la primera cubierta bajo la de arriba: fogón de ladrillo con boca de fuego, olla, cucharón y chimenea, y una mesa con bancos para la tripulación.
- Camarote de la tripulación: hasta 16 hamacas colgadas de las vigas a los costados.
- Pendiente: castillo de proa elevado (taparía los cañones de proa; necesita colisión propia).

## 0.30.0 — Ventanas nuevas  (2026-11-09)
- Menú principal cinematográfico: la cámara gira despacio alrededor de tu barco; Continuar, Nueva partida (con confirmación), Opciones, Créditos y Salir, con la fecha de la partida guardada.
- Mochila nueva (I): casillas con icono, categorías, búsqueda y orden; ficha de cada objeto con descripción, de dónde sale, en qué recetas se usa y botones (comer, vender, ver recetas). A bordo, la bodega aparece abajo para pasar cosas de un lado a otro (clic, Mayús+clic 10, doble clic todo).
- Fabricación nueva (Tab): categorías (Herramientas, Materiales, Supervivencia, Construcción, Barco, Armamento, Creativo), favoritos con estrella, búsqueda por receta o material, "solo lo que puedo hacer", materiales con lo que tienes y lo que falta, cantidad, Enter para fabricar y Q/E para cambiar de categoría.
- Mercado nuevo (Tab junto a un mercader): pestañas Vender y Comprar, precio por unidad de cada isla, cantidad con Máx y total antes de confirmar. Vender mucho baja el precio por lotes, como antes.
- Tu barco (B): plano visto desde arriba con cada porta (llena o vacía), estado, medidas, mejoras; armamento por lado con + y − (lo que quitas vuelve a la mochila) y bodega con traslados.
- Carta de navegación nueva (M): pergamino con rosa de los vientos, islas con nombre, mercaderes, tu barco y tú; zoom (se recuerda), arrastrar, Centrar, y marcas propias con doble clic que se guardan con la partida.
- Pausa (Esc): detiene el juego; opciones, controles, bitácora, guardar, menú principal y salir.
- Opciones: cambiar cualquier tecla, volumen, calidad gráfica, resolución de render, sombras, VSync, límite de FPS, pantalla completa, tamaño de texto, alto contraste, reducir movimiento, escala de la interfaz, opacidad de paneles y qué partes del HUD se ven.
- Bitácora (G): todos los avisos de la sesión con hora, filtros por tipo y búsqueda.
- Taller de pintura (P) con la interfaz nueva: partes con su color, paleta, color libre, vista previa de cada dibujo y mascarón.
- Controles táctiles para Android (palanca y botones); solo aparecen en el teléfono.
- Arreglos: el costo de mejorar velas ya no se desborda en barcos muy mejorados; un clic en la interfaz no cuenta como espadazo.

## 0.29.0 — Interfaz nueva  (2026-11-08)
- Interfaz nueva hecha con UI Toolkit (base de toda la renovación): madera oscura, pergamino, cuero y bronce, con fuentes Cinzel (títulos) y Alegreya Sans (texto) y texturas hechas por código.
- HUD nuevo por zonas, sin que nada se tape: vida, hambre y sed con barras animadas (lo perdido se ve un momento en claro y parpadean cuando queda poco), brújula con iconos de islas, mercaderes, tu barco y enemigos, tarjeta del enemigo con su tripulación, mochila rápida con casillas e iconos, y tarjeta de tu barco con velas, nudos, rumbo, bodega y una barra de recarga por cada lado de cañones.
- Avisos nuevos: globos a la derecha que entran deslizándose, con color e icono según el tipo (descubrimiento, peligro, comercio, objetos…); los iguales se agrupan. Anuncios grandes en el centro para el abordaje y la derrota. Todo queda registrado para la bitácora.
- Al recoger algo sale "+3 Madera" subiendo junto a la mochila y la casilla se marca en verde.
- Ventanas nuevas: barra de título, se arrastran, recuerdan dónde las dejaste, se abren y cierran con animación y Esc cierra la de arriba. F1 abre la nueva guía de controles (con los nombres reales de tus teclas).
- Atajos de teclado configurables (se guardan): todo el juego pregunta por acciones (usar, saltar, disparar babor…) en vez de teclas fijas. La pantalla para cambiarlos llega con Opciones.
- Sonidos de interfaz hechos por código (clic, abrir, cerrar, monedas, alerta, descubrir…).
- Borrar la partida (F8) ahora pide confirmación en una ventana.
- Mientras hay una ventana abierta el juego no reacciona a las teclas y el cursor queda libre; un clic sobre la interfaz ya no recaptura el cursor ni cuenta como espadazo.
- Tecla de prueba "ir a tu barco": de B a Y (B queda para la ventana del barco).
- El menú de fabricación y la carta de navegación siguen como antes pero con la paleta y las fuentes nuevas; se renuevan en las siguientes versiones.

## 0.28.0 — Taller  (2026-11-08)
- Taller (tecla P, a bordo de tu barco): pinta cada parte por separado con cualquier color (12 de paleta o libre con tono, color vivo y brillo): casco, cubierta, castillo de popa, mástiles y madera, velas, bandera, cañones y hierro, adornos dorados.
- Dibujos en velas y bandera con un segundo color: franjas, rayas, cruz, diagonal, borde, calavera con huesos y sol.
- Elige el mascarón de proa: león, sirena, dragón, calavera o ninguno.
- Los cambios se ven al momento y la cámara gira sola alrededor del barco. Se pagan al guardar (5 de oro por parte cambiada, 20 el mascarón; gratis en creativo). Cancelar o P/Esc deja todo como estaba.
- La pintura se guarda con la partida; las partidas viejas conservan sus colores.

## 0.27.1 — Reversa  (2026-11-08)
- Reversa: con las velas arriadas, S otra vez pone las velas en facha y el barco anda hacia atrás, despacio. W vuelve a avanzar.
- En reversa el timón gira al revés, como en un barco de verdad. El panel del barco muestra "reversa".

## 0.27.0 — Colisiones y proporciones  (2026-11-08)
- Colisión con la forma real del casco: antes el barco chocaba como una caja y en la proa había esquinas invisibles donde se podía pararse en el aire. La caja queda solo para medidas.
- En cubierta solo se camina sobre el casco de verdad (la proa se cierra hasta la punta), y los mástiles, los cañones de cubierta y el cabrestante son sólidos.
- La cámara no atraviesa ningún barco (el tuyo ni los demás), ni la borda, ni el castillo; también se revisa mientras se mueve.
- Flotación según el tamaño: la quilla va al 38 % del alto bajo el agua. Antes siempre era 1,2 m: los botes casi se hundían y los navíos flotaban encima.
- Las filas de portas quedan siempre sobre la línea de agua.
- Andanada en ola: los cañones disparan uno tras otro de popa a proa (la ola entera dura hasta 1,2 s).
- Recarga más rápida en barcos grandes: cada cañón más suma 1,2 %, con tope del doble (antes 64 cañones tardaban casi 4 veces más).
- Proporciones de los barcos chicos: el castillo de popa crece con el casco (1 m en un bote, 2,4 m en los grandes), anclas, bauprés, espolón y faroles más pequeños, y velas de ~1,5 veces la manga. El camarote por dentro solo existe en barcos donde cabe.

## 0.26.0 — Barco detallado  (2026-11-08)
- Barco detallado según la lámina de referencia (docs/referencia-barco.png), todo hecho por código para cualquier tamaño:
- Jarcia: obenques con flechates (las escalas de cuerda) en cada mástil y estays de mástil a mástil y al bauprés.
- Cofas: plataformas con baranda en los mástiles con varias velas.
- Cintas: franjas oscuras a lo largo del casco bajo cada fila de portas y bajo la borda.
- Castillo de popa: baranda en la toldilla, moldura dorada, galería (balcón) detrás de las ventanas, escaleras de la cubierta al timón y faroles en las esquinas.
- Mascarón de proa dorado (león, sirena, dragón o calavera) y, en cubierta, cabrestante y campana modelados en Blender.

## 0.25.0 — Cubierta armada  (2026-11-08)
- La cámara ya no atraviesa paredes: se detiene antes del casco, del castillo de popa y de las islas.
- Fila de cañones en la cubierta de arriba: sobre cureña, detrás de la borda (con portas en la borda si es alta). Cuenta como un piso más: cada vez que alargas también suma uno por costado ahí.
- Modelos sin huecos: el cañón, el barril, el espolón y el mascarón de sirena tenían las tapas abiertas; ahora todos los modelos se exportan cerrados y con las caras hacia afuera.
- Mejorar barriles (Tab → Cañones y bodega): cada mejora suelta un barril más por tanda, hasta 5, abiertos en abanico detrás del barco. Se guarda con la partida.

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
