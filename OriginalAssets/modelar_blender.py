"""Modelado procedural original con Blender 4.5; conserva .blend y FBX. No implica modelado manual del estudiante."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent; A=ROOT.parent/'Assets/Fase2'
OUT=ROOT
(ROOT/'Evidencias').mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
sc=bpy.context.scene;sc.unit_settings.system='METRIC';sc.unit_settings.scale_length=1
def mat(name,tex,metal,rough,emit=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF')
 image=bpy.data.images.load(str(A/'Textures'/tex));image.pack();n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=image
 m.node_tree.links.new(n.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
 if emit:m.node_tree.links.new(n.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=emit
 return m
panel=mat('Panel','Panel_Atlas.png',.4,.42); circuit=mat('Circuito','Circuito.png',.2,.3,.5);floor=mat('Suelo','Suelo.png',.25,.6)
objects=[]
def cube(name,loc,scale,material,bevel=.035):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=scale
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('Biseles fabricados','BEVEL');m.width=bevel;m.segments=3;bpy.ops.object.modifier_apply(modifier=m.name)
 o.data.materials.append(material);objects.append(o);return o
def lathe(name,profile,material,sides=32):
 verts=[(r*math.cos(i*math.tau/sides),r*math.sin(i*math.tau/sides),z) for z,r in profile for i in range(sides)]
 faces=[]
 for j in range(len(profile)-1):
  for i in range(sides):faces.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
 faces.extend([tuple(reversed(range(sides))),tuple((len(profile)-1)*sides+i for i in range(sides))])
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);o.data.materials.append(material);objects.append(o);return o
stats=[];allmodels=[]
def finish(name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 sc.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(name+'.blend')))
 bpy.ops.export_scene.fbx(filepath=str(A/'Models'/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP',object_types={'MESH'})
 o.data.calc_loop_triangles();stats.append(dict(modelo=name,vertices=len(o.data.vertices),triangulos=len(o.data.loop_triangles),dimensiones_blender=list(o.dimensions),UV=len(o.data.uv_layers),escala=list(o.scale),pivote=list(o.location)))
 allmodels.append(o);objects.clear();o.hide_set(True);o.hide_render=True
# Baldosa construida con rebaje y cuatro insertos antideslizantes.
cube('Base',(0,0,-.09),(2,2,.18),panel)
for x in (-.48,.48):
 for y in (-.48,.48):cube('Inserto',(x,y,.006),(.9,.9,.012),floor,.01)
finish('ModuloSuelo')
cube('Panel',(0,0,1.5),(2,.18,3),panel)
for x in (-.86,.86):cube('Montante',(x,-.12,1.5),(.12,.18,3),panel)
cube('Canal',(0,-.13,1.8),(1.45,.08,.15),circuit)
for x in (-.5,0,.5):cube('Ventilacion',(x,-.12,.55),(.22,.09,.6),panel)
finish('ModuloMuro')
lathe('Pedestal',[(0,.56),(.06,.6),(.14,.54),(.22,.38),(.8,.32),(.89,.49),(.96,.51),(1,.46)],panel)
lathe('Aro',[(.78,.33),(.8,.36),(.85,.36),(.87,.33)],circuit)
cube('Indicador',(0,-.34,.5),(.25,.035,.14),circuit,.01)
finish('Pedestal')
lathe('Nucleo',[(0,.105),(.04,.14),(.08,.105),(.36,.105),(.40,.14),(.44,.105)],circuit)
for z in (.02,.08,.36,.42):lathe('Anillo',[(z-.015,.145),(z,.16),(z+.015,.145)],panel)
for i in range(4):
 a=i*math.pi/2;o=cube('Costilla',(.125*math.cos(a),.125*math.sin(a),.22),(.035,.035,.31),panel,.01)
finish('CeldaEnergia')
cube('Base',(0,0,.07),(1.6,.95,.14),panel)
cube('Pie',(0,.1,.57),(.9,.55,.92),panel)
o=cube('Consola',(0,0,1.12),(1.55,.85,.16),panel);o.rotation_euler.x=math.radians(15)
o=cube('Pantalla',(0,-.02,1.23),(1.05,.54,.025),circuit,.02);o.rotation_euler.x=math.radians(15)
for x in (-.64,.64):cube('Banda',(x,-.32,.64),(.07,.035,.7),circuit,.01)
finish('Estacion')
# Archivo maestro y render auténtico de modelos, sin simular interfaz de Blender.
for idx,o in enumerate(allmodels):
 o.hide_set(False);o.hide_render=False;o.location=((idx-2)*2.6,0,0)
bpy.ops.object.camera_add(location=(10,-17,11));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=15;sc.camera=cam
for loc,power,size in [((0,-5,9),2000,8),((3,4,8),1600,6)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
sc.world.color=(.17,.17,.17);sc.render.engine='CYCLES';sc.cycles.samples=24;sc.render.resolution_x=1600;sc.render.resolution_y=850;sc.render.resolution_percentage=100
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Catalogo_modelos.blend'))
sc.render.filepath=str(ROOT/'Evidencias/Blender_modelos_render.png');bpy.ops.render.render(write_still=True)
(ROOT/'Evidencias/Blender_geometria.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
print('MODELOS_EXPORTADOS',stats)
