# Modelos de piezas fijas del barco, hechos en Blender (bpy) y exportados como datos para Unity.
# Ejes en Blender: X derecha, Y adelante (proa), Z arriba. En Unity: (x, y, z) = (X, Z, Y).
import bpy, bmesh, math, json, os, sys
from mathutils import Vector, Matrix

SAL = sys.argv[-1] if len(sys.argv) > 1 and sys.argv[-1].endswith('/') else os.path.join(os.path.dirname(os.path.abspath(__file__)), 'salida') + '/'
os.makedirs(SAL, exist_ok=True)

def limpiar():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def obj_de_bm(bm, nombre):
    me = bpy.data.meshes.new(nombre); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(nombre, me); bpy.context.scene.collection.objects.link(o); return o

def unir(nombre, objs):
    """Junta varios objetos en uno con nombre (una pieza = un material en Unity)."""
    bm = bmesh.new()
    for o in objs:
        m = o.matrix_world.copy()
        tmp = bmesh.new(); tmp.from_mesh(o.data); tmp.transform(m)
        me = bpy.data.meshes.new('t'); tmp.to_mesh(me); tmp.free()
        bm.from_mesh(me); bpy.data.meshes.remove(me)
        bpy.data.objects.remove(o)
    return obj_de_bm(bm, nombre)

def torno(perfil, segs=16, nombre='torno', eje='Y'):
    """Gira un perfil [(r, a)] alrededor del eje (a = posición sobre el eje)."""
    bm = bmesh.new()
    anillos = []
    for (r, a) in perfil:
        anillo = []
        for i in range(segs):
            t = 2*math.pi*i/segs
            c, s = math.cos(t)*r, math.sin(t)*r
            p = (c, a, s) if eje == 'Y' else (c, s, a)
            anillo.append(bm.verts.new(p))
        anillos.append(anillo)
    for k in range(len(anillos)-1):
        A, B = anillos[k], anillos[k+1]
        for i in range(segs):
            j = (i+1) % segs
            bm.faces.new((A[i], A[j], B[j], B[i]))
    # tapas
    if perfil[0][0] > 1e-4: bm.faces.new(list(reversed(anillos[0])))
    if perfil[-1][0] > 1e-4: bm.faces.new(anillos[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return obj_de_bm(bm, nombre)

def caja(nombre, centro, tam, rot=(0,0,0)):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=tam, verts=bm.verts)
    o = obj_de_bm(bm, nombre); o.location = centro; o.rotation_euler = [math.radians(r) for r in rot]
    bpy.context.view_layer.update(); return o

def cilindro(nombre, centro, radio, largo, segs=12, rot=(0,0,0), r2=None):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=segs, radius1=radio, radius2=radio if r2 is None else r2, depth=largo)
    o = obj_de_bm(bm, nombre); o.location = centro; o.rotation_euler = [math.radians(r) for r in rot]
    bpy.context.view_layer.update(); return o

def esfera(nombre, centro, radio, escala=(1,1,1), segs=12, anillos=8):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=anillos, radius=radio)
    bmesh.ops.scale(bm, vec=escala, verts=bm.verts)
    o = obj_de_bm(bm, nombre); o.location = centro; bpy.context.view_layer.update(); return o

def toro(nombre, centro, R, r, segsR=16, segsr=8, rot=(0,0,0)):
    bm = bmesh.new(); vs = []
    for i in range(segsR):
        a = 2*math.pi*i/segsR; fila = []
        for j in range(segsr):
            b = 2*math.pi*j/segsr
            x = (R + r*math.cos(b))*math.cos(a); y = (R + r*math.cos(b))*math.sin(a); z = r*math.sin(b)
            fila.append(bm.verts.new((x, y, z)))
        vs.append(fila)
    for i in range(segsR):
        for j in range(segsr):
            a, b = vs[i][j], vs[(i+1)%segsR][j]; c, d = vs[(i+1)%segsR][(j+1)%segsr], vs[i][(j+1)%segsr]
            bm.faces.new((a, b, c, d))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    o = obj_de_bm(bm, nombre); o.location = centro; o.rotation_euler = [math.radians(x) for x in rot]
    bpy.context.view_layer.update(); return o

def exportar(nombre_archivo):
    """Guarda cada objeto como malla triangulada en coordenadas de Unity."""
    datos = {'objetos': []}
    for o in bpy.context.scene.objects:
        if o.type != 'MESH': continue
        bm = bmesh.new(); bm.from_mesh(o.data); bm.transform(o.matrix_world)
        # Sin huecos (tapas de los tornos) y todas las caras hacia afuera: si no, en Unity se ven huecos
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bmesh.ops.holes_fill(bm, edges=bm.edges, sides=0)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bmesh.ops.triangulate(bm, faces=bm.faces)
        v, t = [], []
        for f in bm.faces:
            for lv in f.verts:
                X, Y, Z = lv.co
                v += [round(X, 4), round(Z, 4), round(Y, 4)]   # Unity (x, y, z) = Blender (X, Z, Y)
            n = len(v)//3
            t += [n-3, n-2, n-1]
        bm.free()
        datos['objetos'].append({'nombre': o.name, 'v': v, 't': t})
    with open(SAL + nombre_archivo + '.json', 'w') as f: json.dump(datos, f, separators=(',', ':'))
    bpy.ops.wm.save_as_mainfile(filepath=SAL + nombre_archivo + '.blend')
    print('exportado', nombre_archivo, [(o['nombre'], len(o['t'])//3) for o in datos['objetos']])

# ---------------- Cañón con cureña ----------------
def canon():
    limpiar()
    # Tubo de bronce/hierro: culata con botón, refuerzos y boca acampanada. Muñones en y = 0.
    perfil = [(0.0,-0.86),(0.07,-0.86),(0.09,-0.80),(0.06,-0.76),(0.20,-0.74),(0.23,-0.70),(0.23,-0.55),(0.21,-0.50),
              (0.215,-0.46),(0.20,-0.42),(0.195,0.10),(0.205,0.13),(0.19,0.17),(0.165,0.75),(0.18,0.80),(0.19,0.90),
              (0.17,0.95),(0.09,0.95),(0.09,0.40)]
    tubo = torno(perfil, 18, 'Tubo')
    mun = cilindro('m', (0,0,0), 0.06, 0.62, 10, rot=(0,90,0))
    tubo = unir('Tubo', [tubo, mun])
    # Cureña: dos costados escalonados y un eje
    lados = []
    for x in (-0.24, 0.24):
        lados.append(caja('c', (x, -0.12, -0.20), (0.09, 0.95, 0.34)))
        lados.append(caja('c', (x, -0.45, -0.02), (0.09, 0.42, 0.18)))
    lados.append(caja('c', (0, -0.12, -0.31), (0.48, 0.80, 0.08)))
    lados.append(cilindro('e', (0, 0.22, -0.36), 0.05, 0.86, 8, rot=(0,90,0)))
    lados.append(cilindro('e', (0, -0.50, -0.36), 0.05, 0.86, 8, rot=(0,90,0)))
    unir('Cureña', lados)
    ruedas = []
    for x in (-0.38, 0.38):
        for y in (0.22, -0.50):
            ruedas.append(cilindro('r', (x, y, -0.36), 0.17, 0.08, 14, rot=(0,90,0)))
    unir('Ruedas', ruedas)
    exportar('canon')

# ---------------- Rueda del timón ----------------
def timon():
    limpiar()
    partes = [toro('a', (0,0,0), 0.5, 0.04, 24, 6, rot=(90,0,0)),
              cilindro('cubo', (0,0,0), 0.1, 0.16, 10, rot=(90,0,0))]
    for i in range(8):
        a = 2*math.pi*i/8
        c, s = math.cos(a), math.sin(a)
        rayo = cilindro('r', (c*0.3, 0, s*0.3), 0.025, 0.62, 6)
        rayo.rotation_euler = (0, -a + math.pi/2, 0); bpy.context.view_layer.update()
        man = cilindro('h', (c*0.64, 0, s*0.64), 0.035, 0.22, 6, r2=0.02)
        man.rotation_euler = (0, -a + math.pi/2, 0); bpy.context.view_layer.update()
        partes += [rayo, man]
    unir('Rueda', partes)
    unir('Pedestal', [cilindro('p', (0, -0.12, -0.45), 0.08, 0.9, 8), caja('b', (0, -0.12, -0.92), (0.4, 0.4, 0.08))])
    exportar('timon')

# ---------------- Ancla ----------------
def arco(nombre, centro, R, r, a0, a1, segs=12, segsr=6):
    """Trozo de toro en el plano XZ (de a0 a a1 grados)."""
    bm = bmesh.new(); filas = []
    for i in range(segs+1):
        a = math.radians(a0 + (a1-a0)*i/segs); fila = []
        for j in range(segsr):
            b = 2*math.pi*j/segsr
            rr = R + r*math.cos(b)
            fila.append(bm.verts.new((centro[0] + rr*math.cos(a), centro[1] + r*math.sin(b), centro[2] + rr*math.sin(a))))
        filas.append(fila)
    for i in range(segs):
        for j in range(segsr):
            k = (j+1) % segsr
            bm.faces.new((filas[i][j], filas[i+1][j], filas[i+1][k], filas[i][k]))
    bm.faces.new(filas[0]); bm.faces.new(list(reversed(filas[-1])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return obj_de_bm(bm, nombre)

def ancla():
    limpiar()
    partes = [cilindro('caña', (0,0,0.05), 0.055, 1.4, 8), toro('arg', (0,0,0.85), 0.11, 0.03, 12, 6, rot=(90,0,0)),
              arco('brazos', (0,0,-0.2), 0.5, 0.05, 195, 345, 14, 6), esfera('cruz', (0,0,-0.7), 0.09)]
    for lado in (-1, 1):
        a = math.radians(195 if lado < 0 else 345)
        x, z = math.cos(a)*0.5, -0.2 + math.sin(a)*0.5
        u = cilindro('uña', (x + lado*0.04, 0, z + 0.12), 0.13, 0.3, 3, r2=0.0)
        u.rotation_euler = (0, math.radians(-lado*35), 0); u.scale = (1, 0.35, 1); bpy.context.view_layer.update()
        partes.append(u)
    unir('Ancla', partes)
    unir('Cepo', [cilindro('c', (0,0,0.62), 0.045, 0.9, 8, rot=(0,90,0)), esfera('b1', (0.46,0,0.62), 0.06), esfera('b2', (-0.46,0,0.62), 0.06)])
    exportar('ancla')

# ---------------- Farol ----------------
def farol():
    limpiar()
    unir('Farol', [cilindro('base', (0,0,0), 0.14, 0.05, 8), cilindro('tapa', (0,0,0.34), 0.15, 0.12, 8, r2=0.04),
                   toro('asa', (0,0,0.45), 0.06, 0.012, 10, 5, rot=(90,0,0))] +
                  [cilindro('p', (math.cos(a)*0.12, math.sin(a)*0.12, 0.17), 0.012, 0.3, 4) for a in [i*math.pi/2 for i in range(4)]])
    unir('Vidrio', [cilindro('v', (0,0,0.17), 0.105, 0.3, 8)])
    exportar('farol')

# ---------------- Barril ----------------
def barril():
    limpiar()
    torno([(0.0,0),(0.28,0),(0.33,0.2),(0.35,0.45),(0.33,0.7),(0.28,0.9),(0.0,0.9)], 14, 'Barril', eje='Z')
    unir('Aros', [toro('a', (0,0,z), r, 0.018, 16, 4) for (z, r) in [(0.1,0.305),(0.3,0.345),(0.6,0.345),(0.8,0.305)]])
    exportar('barril')

# ---------------- Mascarones de proa (miran hacia +Y, salen inclinados de la proa) ----------------
def mascaron_calavera():
    limpiar()
    craneo = esfera('c', (0, 0.15, 0.25), 0.42, escala=(1, 1.05, 0.95), segs=14, anillos=10)
    mandibula = caja('m', (0, 0.32, -0.12), (0.46, 0.36, 0.2))
    pomulos = [esfera('p', (x, 0.36, 0.02), 0.13) for x in (-0.2, 0.2)]
    huesos = []
    for s in (-1, 1):
        h = cilindro('h', (0, 0.0, -0.05), 0.06, 1.5, 8, rot=(0, 0, 0)); h.rotation_euler = (0, math.radians(s*55), 0)
        bpy.context.view_layer.update(); huesos.append(h)
        for e in (-1, 1):
            ang = math.radians(s*55); dx, dz = math.sin(ang)*0.75*e, math.cos(ang)*0.75*e
            huesos += [esfera('k', (dx + 0.07, -0.02, -0.05 + dz), 0.09), esfera('k', (dx - 0.07, -0.02, -0.05 + dz), 0.09)]
    unir('Mascaron', [craneo, mandibula] + pomulos + huesos)
    ojos = [esfera('o', (x, 0.48, 0.28), 0.11, escala=(1, 0.5, 1.1)) for x in (-0.16, 0.16)]
    nariz = cilindro('n', (0, 0.55, 0.1), 0.06, 0.08, 3, rot=(90, 0, 0))
    dientes = [caja('d', (x, 0.5, -0.08), (0.05, 0.03, 0.09)) for x in (-0.15, -0.05, 0.05, 0.15)]
    unir('Detalle', ojos + [nariz] + dientes)
    exportar('mascaron_calavera')

def mascaron_sirena():
    limpiar()
    # Figura clásica de proa: cuerpo inclinado hacia adelante, cola que baja y se curva hacia atrás
    torso = torno([(0.16,0.0),(0.22,0.18),(0.24,0.35),(0.19,0.55),(0.12,0.68),(0.08,0.75)], 12, 't', eje='Z')
    torso.location = (0, 0.1, 0.1); torso.rotation_euler = (math.radians(-35), 0, 0); bpy.context.view_layer.update()
    cabeza = esfera('cab', (0, 0.62, 0.86), 0.16, segs=12, anillos=8)
    cola1 = torno([(0.0,-0.55),(0.12,-0.45),(0.2,-0.2),(0.2,0.05)], 12, 'c1', eje='Z')
    cola1.location = (0, 0.05, 0.05); cola1.rotation_euler = (math.radians(25), 0, 0); bpy.context.view_layer.update()
    aleta = cilindro('al', (0, -0.25, -0.55), 0.3, 0.06, 3, rot=(90, 0, 0)); aleta.scale = (1.3, 1, 0.6); bpy.context.view_layer.update()
    brazos = []
    for s_ in (-1, 1):
        b = cilindro('b', (s_*0.2, 0.42, 0.45), 0.045, 0.5, 6); b.rotation_euler = (math.radians(-70), math.radians(s_*-30), 0)
        bpy.context.view_layer.update(); brazos.append(b)
    unir('Mascaron', [torso, cabeza, cola1, aleta] + brazos)
    pelo = esfera('p', (0, 0.55, 0.88), 0.18, escala=(1.05, 1.0, 1.15), segs=10, anillos=7)
    melena = caja('m', (0, 0.42, 0.68), (0.28, 0.1, 0.42)); melena.rotation_euler = (math.radians(-35), 0, 0)
    escamas = [toro('e', (0, 0.05 + k*0.0, -0.05 - k*0.12), 0.2 - k*0.03, 0.02, 12, 4) for k in range(3)]
    unir('Detalle', [pelo, melena] + escamas)
    exportar('mascaron_sirena')

def mascaron_dragon():
    limpiar()
    cuello = torno([(0.3,0.0),(0.26,0.4),(0.22,0.8),(0.2,1.0)], 10, 'c', eje='Z')
    cuello.rotation_euler = (math.radians(-40), 0, 0); bpy.context.view_layer.update()
    cabeza = caja('h', (0, 0.85, 0.85), (0.38, 0.75, 0.32)); cabeza.rotation_euler = (math.radians(-10), 0, 0)
    hocico = caja('s', (0, 1.25, 0.78), (0.3, 0.35, 0.22))
    mandibula = caja('j', (0, 1.05, 0.62), (0.3, 0.6, 0.1)); mandibula.rotation_euler = (math.radians(15), 0, 0)
    cuernos = []
    for s in (-1, 1):
        c = cilindro('cu', (s*0.15, 0.55, 1.12), 0.07, 0.55, 6, r2=0.0); c.rotation_euler = (math.radians(-60), math.radians(s*20), 0)
        bpy.context.view_layer.update(); cuernos.append(c)
    crestas = []
    for k in range(4):
        cr = caja('cr', (0, 0.1 + k*0.17, 0.25 + k*0.17), (0.04, 0.14, 0.2)); cr.rotation_euler = (math.radians(-40), 0, 0)
        bpy.context.view_layer.update(); crestas.append(cr)
    unir('Mascaron', [cuello, cabeza, hocico, mandibula] + crestas)
    ojos = [esfera('o', (s*0.19, 0.95, 0.95), 0.06) for s in (-1, 1)]
    dientes = [cilindro('d', (s*0.12, 1.32, 0.66), 0.03, 0.12, 4, r2=0.0, rot=(180, 0, 0)) for s in (-1, -0.33, 0.33, 1)]
    unir('Detalle', ojos + dientes + cuernos)
    exportar('mascaron_dragon')

def mascaron_leon():
    limpiar()
    cabeza = esfera('c', (0, 0.35, 0.28), 0.33, escala=(0.95, 1.0, 1.05), segs=12, anillos=8)
    hocico = esfera('h', (0, 0.62, 0.15), 0.17, escala=(1.2, 0.9, 0.8))
    orejas = [esfera('o', (s*0.24, 0.3, 0.6), 0.08) for s in (-1, 1)]
    unir('Mascaron', [cabeza, hocico] + orejas)
    # Melena: mechones en punta alrededor de la cara
    mechones = []
    for k in range(14):
        a = 2*math.pi*k/14
        m = cilindro('m', (math.cos(a)*0.38, 0.18, 0.28 + math.sin(a)*0.38), 0.13, 0.45, 5, r2=0.0)
        m.rotation_euler = (0, -a + math.pi/2, 0); bpy.context.view_layer.update()
        m.rotation_euler = (math.radians(90), 0, 0); bpy.context.view_layer.update()
        # apunta hacia afuera desde la cara
        d = Vector((math.cos(a), -0.35, math.sin(a))).normalized()
        m.rotation_euler = d.to_track_quat('Z', 'Y').to_euler(); bpy.context.view_layer.update()
        mechones.append(m)
    ojos = [esfera('e', (s*0.12, 0.6, 0.38), 0.05) for s in (-1, 1)]
    nariz = esfera('n', (0, 0.76, 0.22), 0.06, escala=(1.3, 0.8, 0.7))
    anillo = toro('a', (0, 0.66, -0.02), 0.13, 0.025, 14, 5)
    anillo.rotation_euler = (math.radians(90), 0, 0); bpy.context.view_layer.update()
    unir('Melena', mechones + [esfera('fondo', (0, 0.05, 0.28), 0.42, escala=(1, 0.5, 1))])
    unir('Detalle', ojos + [nariz, anillo])
    exportar('mascaron_leon')

for f in (canon, timon, ancla, farol, barril, mascaron_calavera, mascaron_sirena, mascaron_dragon, mascaron_leon):
    f()
