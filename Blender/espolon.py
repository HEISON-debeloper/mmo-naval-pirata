# Espolón de proa (bronce), mira hacia +Y (adelante). La base (y = 0) se pega a la roda del barco.
import sys
src = open('/home/claude/blender/modelos.py').read().split('\nfor f in (')[0]
exec(src)
import bpy, math

def espolon():
    limpiar()
    pico = torno([(0.36,0.0),(0.34,0.5),(0.26,1.0),(0.14,1.45),(0.04,1.75),(0.0,1.8)], 4, 'p', eje='Y')
    pico.scale = (1.25, 1, 0.8); pico.rotation_euler = (0, math.radians(45), 0)
    bpy.context.view_layer.update()
    hojas = [caja('h%d' % i, (0, 1.05, z), (1.05 - abs(z)*1.6, 0.75, 0.05)) for i, z in enumerate((-0.18, 0.0, 0.18))]
    cresta = caja('c', (0, 0.9, 0.0), (0.06, 0.95, 0.62))
    unir('Espolon', [pico, cresta] + hojas)
    col = caja('Collar', (0, 0.06, 0), (0.95, 0.16, 0.85))
    exportar('espolon')

espolon()
