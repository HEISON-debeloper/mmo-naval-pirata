import bpy, sys, math, os
from mathutils import Vector
SAL='/home/claude/blender/salida/'
colores={'Tubo':(0.12,0.12,0.13),'Cureña':(0.45,0.28,0.14),'Ruedas':(0.35,0.2,0.1),'Rueda':(0.45,0.28,0.14),'Pedestal':(0.3,0.18,0.09),
 'Ancla':(0.15,0.15,0.16),'Cepo':(0.45,0.28,0.14),'Farol':(0.12,0.12,0.12),'Vidrio':(1.0,0.8,0.3),'Barril':(0.5,0.32,0.16),'Aros':(0.2,0.2,0.2),
 'Mascaron':(0.85,0.68,0.25),'Detalle':(0.1,0.08,0.06)}
for f in sorted(os.listdir(SAL)):
    if not f.endswith('.blend'): continue
    bpy.ops.wm.open_mainfile(filepath=SAL+f)
    sc=bpy.context.scene
    mn=Vector((1e9,)*3); mx=Vector((-1e9,)*3)
    for o in sc.objects:
        if o.type!='MESH': continue
        m=bpy.data.materials.new(o.name); m.use_nodes=True
        m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*colores.get(o.name,(0.6,0.6,0.6)),1)
        o.data.materials.append(m)
        for v in o.data.vertices:
            w=o.matrix_world@v.co; mn=Vector(map(min,mn,w)); mx=Vector(map(max,mx,w))
    c=(mn+mx)/2; r=(mx-mn).length
    cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam)
    cam.location=c+Vector((r*1.1,r*1.3,r*0.7)); d=c-cam.location
    cam.rotation_euler=d.to_track_quat('-Z','Y').to_euler(); sc.camera=cam
    sol=bpy.data.objects.new('sol',bpy.data.lights.new('sol','SUN')); sc.collection.objects.link(sol); sol.rotation_euler=(0.7,0.2,0.8); sol.data.energy=4
    w=bpy.data.worlds.new('w'); sc.world=w; w.use_nodes=True; w.node_tree.nodes['Background'].inputs[0].default_value=(0.55,0.7,0.8,1)
    sc.render.engine='CYCLES'; sc.cycles.samples=16; sc.cycles.device='CPU'
    sc.render.resolution_x=320; sc.render.resolution_y=320
    sc.render.filepath=SAL+f.replace('.blend','.png'); bpy.ops.render.render(write_still=True)
