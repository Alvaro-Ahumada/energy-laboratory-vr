"""Recursos originales procedurales, creados con asistencia de Codex. Ejecutar con Python/Pillow/numpy."""
from pathlib import Path
import math, json, wave
import numpy as np
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parent
ASSETS=ROOT.parent/'Assets/Fase2'
for d in ('Textures','Audio','Models','Materials','Prefabs','VFX','Scripts','Documentation'):(ASSETS/d).mkdir(parents=True,exist_ok=True)
font_path='C:/Windows/Fonts/arial.ttf'
def font(n):return ImageFont.truetype(font_path,n)
# Atlas de paneles metálicos: detalles y señalización reales, compartido entre módulos.
im=Image.new('RGB',(1024,1024),'#1c303f'); d=ImageDraw.Draw(im)
for x in range(0,1024,256):
 for y in range(0,1024,256):
  d.rounded_rectangle((x+7,y+7,x+249,y+249),radius=15,fill='#304956',outline='#506a75',width=3)
  d.line((x+24,y+46,x+232,y+46),fill='#122630',width=4)
  for dx in (22,234):
   for dy in (22,234):d.ellipse((x+dx-4,y+dy-4,x+dx+4,y+dy+4),fill='#9daeb0')
  for n in range(7):d.line((x+36,y+87+n*12,x+126,y+87+n*12),fill='#152a35',width=4)
  d.line((x+156,y+75,x+156,y+180,x+210,y+180),fill='#48d5d1',width=4)
  d.ellipse((x+202,y+172,x+218,y+188),outline='#48d5d1',width=3)
  d.text((x+32,y+203),'E / 02',font=font(18),fill='#aec5ca')
im.save(ASSETS/'Textures/Panel_Atlas.png')
im=Image.new('RGB',(512,512),'#0d2930');d=ImageDraw.Draw(im)
for y in range(20,512,64):
 d.line((0,y,128,y,160,y+24,320,y+24,352,y,512,y),fill='#45e8d1',width=5)
 d.ellipse((118,y-10,138,y+10),outline='#e8fbdf',width=3)
d.rectangle((12,12,500,500),outline='#d3ae69',width=6)
im.save(ASSETS/'Textures/Circuito.png')
im=Image.new('RGB',(512,512),'#26333a');d=ImageDraw.Draw(im)
for i in range(-512,1024,48):d.line((i,0,i+512,512),fill='#3f4d52',width=7)
d.rectangle((5,5,506,506),outline='#71878c',width=8)
d.line((0,256,512,256),fill='#15272f',width=6); d.line((256,0,256,512),fill='#15272f',width=6)
im.save(ASSETS/'Textures/Suelo.png')
# Partícula con alpha radial, sin imagen externa.
y,x=np.mgrid[:128,:128]; a=np.clip(1-np.sqrt((x-63.5)**2+(y-63.5)**2)/63.5,0,1)**2
rgba=np.ones((128,128,4),dtype=np.uint8)*255;rgba[:,:,3]=(a*255).astype('uint8')
Image.fromarray(rgba).save(ASSETS/'Textures/Particula.png')
SR=44100
def save_wav(name,sig):
 sig=np.asarray(sig);pcm=np.round(np.clip(sig,-1,1)*32767).astype('<i2')
 with wave.open(str(ASSETS/'Audio'/name),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes(pcm.tobytes())
 return dict(archivo=name,duracion_s=len(sig)/SR,pico=float(np.abs(sig).max()),rms=float(np.sqrt(np.mean(sig**2))))
stats=[]
# Todas las frecuencias completan ciclos enteros en 16 s; envolvente periódica, sin salto en el bucle.
t=np.arange(SR*16)/SR
music=sum(np.sin(2*np.pi*f*t)*(0.65+0.35*np.sin(2*np.pi*t/16+idx)**2) for idx,f in enumerate((110,165,220,277.5,330)))*0.035
stats.append(save_wav('Ambiente_16s.wav',music))
for name,freqs,duration in [('Celda.wav',(660,880,1320),.65),('Activacion.wav',(440,554.375,660,880),1.4),('Espera.wav',(220,196),.35)]:
 t=np.arange(int(SR*duration))/SR
 env=np.minimum(t/.015,1)*np.exp(-t/(duration*.28))*np.minimum((duration-t)/.025,1)
 sig=sum(np.sin(2*np.pi*f*t) for f in freqs)/len(freqs)*env*.55
 stats.append(save_wav(name,sig))
(ROOT/'Evidencias').mkdir(exist_ok=True)
(ROOT/'Evidencias/Audio_analisis.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
# Mapa de diseño, explícitamente diferente de una captura de Unity.
svg='''<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="950" viewBox="0 0 1200 950"><rect width="1200" height="950" fill="#f7faf9"/><g font-family="Arial" fill="#16323f"><text x="60" y="60" font-size="32" font-weight="bold">LABORATORIO DE ENERGÍA</text><text x="60" y="95" font-size="18">Mapa de diseño propuesto • vista cenital • unidades en metros</text><rect x="130" y="150" width="660" height="660" rx="12" fill="#e4edee" stroke="#294958" stroke-width="8"/><path d="M460 710 L295 540 L625 540 L460 375 L460 215" fill="none" stroke="#bd843f" stroke-width="6" stroke-dasharray="12 10"/>'''
for cx,cy,label in ((295,540,'C1'),(625,540,'C2'),(460,375,'C3')):
 svg+=f'<circle cx="{cx}" cy="{cy}" r="38" fill="#246d76"/><text x="{cx}" y="{cy+7}" fill="white" text-anchor="middle" font-size="22">{label}</text>'
svg+='''<rect x="405" y="185" width="110" height="60" rx="8" fill="#294958"/><text x="460" y="222" fill="white" text-anchor="middle" font-size="20">ESTACIÓN</text><circle cx="460" cy="710" r="23" fill="#bb8244"/><text x="460" y="760" text-anchor="middle" font-size="22">INICIO (0, 0, -1)</text><text x="460" y="850" text-anchor="middle" font-size="20">Ancho 12 m · fondo 12 m · recinto abierto al cielo</text><text x="840" y="200" font-size="23" font-weight="bold">LEYENDA</text><text x="840" y="245" font-size="19">C1 (-3, 2): celda</text><text x="840" y="282" font-size="19">C2 (3, 2): celda</text><text x="840" y="319" font-size="19">C3 (0, 5): celda</text><text x="840" y="375" font-size="19">Estación (0, 8)</text><text x="840" y="430" font-size="19">Línea: recorrido sugerido</text><text x="840" y="475" font-size="19">Orden libre de recolección</text><text x="840" y="530" font-size="19">Pedestales: 1 m de alto</text><text x="840" y="575" font-size="19">Suelo: zona navegable</text><text x="840" y="630" font-size="19">UI sobre la estación</text><text x="60" y="920" font-size="16">Diagrama generado para planificación. No es una captura ni una prueba de funcionamiento.</text></g></svg>'''
(ROOT/'Mapa.svg').write_text(svg,encoding='utf-8')
print('Texturas, audio y mapa creados.',stats)
