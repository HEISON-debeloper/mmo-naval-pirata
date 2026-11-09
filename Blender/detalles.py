# Piezas de cubierta: cabrestante y campana. Z arriba, Y adelante. Base en z = 0.
import sys, os
src = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'modelos.py')).read().split('\nfor f in (')[0]
exec(src)
import bpy, math

def cabrestante():
    limpiar()
    # Tambor con cintura (como un carrete), sombrero arriba y 8 barras para empujarlo
    tambor = torno([(0.0,0.0),(0.42,0.0),(0.42,0.08),(0.30,0.18),(0.24,0.45),(0.24,0.62),(0.32,0.72),(0.36,0.80),(0.0,0.80)], 16, 'Tambor', eje='Z')
    sombrero = torno([(0.0,0.80),(0.40,0.80),(0.40,0.92),(0.0,0.92)], 16, 's', eje='Z')
    unir('Tambor', [tambor, sombrero])
    barras = []
    for i in range(8):
        a = i * math.pi / 4
        b = caja('b', (math.cos(a)*0.95, math.sin(a)*0.95, 0.86), (1.25, 0.07, 0.07))
        b.rotation_euler = (0, 0, a); bpy.context.view_layer.update()
        barras.append(b)
    unir('Barras', barras)
    unir('Aros', [toro('a', (0,0,z), r, 0.02, 16, 4) for (z, r) in [(0.1, 0.42), (0.78, 0.35)]])
    exportar('cabrestante')

def campana():
    limpiar()
    # Campana colgada de un marco de madera
    unir('Campana', [torno([(0.0,0.95),(0.05,0.95),(0.11,0.92),(0.13,0.80),(0.15,0.70),(0.21,0.62),(0.22,0.58),(0.0,0.58)], 14, 'c', eje='Z'),
                     esfera('badajo', (0,0,0.56), 0.035)])
    unir('Marco', [caja('p1', (-0.32,0,0.6), (0.08,0.08,1.2)), caja('p2', (0.32,0,0.6), (0.08,0.08,1.2)),
                   caja('t', (0,0,1.18), (0.78,0.1,0.1)), caja('b', (0,0,0.03), (0.8,0.3,0.06))])
    exportar('campana')

cabrestante(); campana()
