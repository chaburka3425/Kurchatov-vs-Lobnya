import os
os.chdir(os.path.dirname(os.path.abspath(__file__)))
import io
r=lambda f: io.open(f,encoding='utf-8').read()
sh=r('shell.html')
html=sh+'\n<script>\n'+r('images.js')+'\n'+r('itemimg.js')+'\n'+r('sounds.js')+'\n'+r('maximg.js')+'\n'+r('courierimg.js')+'\n'+r('game.js')+'\n</script>\n'
io.open('mpka2.html','w',encoding='utf-8').write(html)
io.open('test.html','w',encoding='utf-8').write('<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head><body>'+html+'</body></html>')
print(len(html))
