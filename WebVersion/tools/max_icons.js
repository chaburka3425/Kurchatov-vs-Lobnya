// Рисует иконки способностей Макса Gold (128x128) и сохраняет PNG
const { chromium } = require('playwright');
const fs = require('fs');
const page_js = `
const TAU = Math.PI*2;
function frame(x, top, bot) {
  const g = x.createLinearGradient(0,0,0,128); g.addColorStop(0, top); g.addColorStop(1, bot);
  x.fillStyle = g; x.fillRect(0,0,128,128);
}
function vign(x, a) {
  const g = x.createRadialGradient(64,64,30,64,64,95); g.addColorStop(0,'rgba(0,0,0,0)'); g.addColorStop(1,'rgba(0,0,0,'+a+')');
  x.fillStyle = g; x.fillRect(0,0,128,128);
}
function border(x) {
  x.strokeStyle = '#3a2a08'; x.lineWidth = 6; x.strokeRect(3,3,122,122);
  const g = x.createLinearGradient(0,0,128,128); g.addColorStop(0,'#fff1b0'); g.addColorStop(.5,'#d4a028'); g.addColorStop(1,'#7a5410');
  x.strokeStyle = g; x.lineWidth = 3; x.strokeRect(2.5,2.5,123,123);
}
function goldGrad(x, x0,y0,x1,y1) { const g = x.createLinearGradient(x0,y0,x1,y1);
  g.addColorStop(0,'#fff6c8'); g.addColorStop(.35,'#ffd24a'); g.addColorStop(.7,'#c88a14'); g.addColorStop(1,'#7a4f08'); return g; }
function glow(x, cx, cy, r, col) { const g = x.createRadialGradient(cx,cy,0,cx,cy,r); g.addColorStop(0,col); g.addColorStop(1,'rgba(0,0,0,0)'); x.fillStyle=g; x.fillRect(cx-r,cy-r,r*2,r*2); }
function coin(x, cx, cy, r, tilt) {
  x.save(); x.translate(cx,cy); x.scale(1, tilt);
  x.fillStyle = '#8a5a0a'; x.beginPath(); x.arc(0, 2, r, 0, TAU); x.fill();
  x.fillStyle = goldGrad(x,-r,-r,r,r); x.beginPath(); x.arc(0,0,r,0,TAU); x.fill();
  x.strokeStyle = 'rgba(122,79,8,.8)'; x.lineWidth = Math.max(1, r*.14); x.beginPath(); x.arc(0,0,r*.7,0,TAU); x.stroke();
  x.fillStyle = 'rgba(255,255,255,.7)'; x.beginPath(); x.ellipse(-r*.35,-r*.35,r*.25,r*.12,-.7,0,TAU); x.fill();
  x.restore();
}
function bullet(x, cx, cy, len, w, ang) {
  x.save(); x.translate(cx,cy); x.rotate(ang);
  x.fillStyle = goldGrad(x,0,-w,0,w);
  x.beginPath(); x.moveTo(-len/2,-w/2); x.lineTo(len*.15,-w/2); x.quadraticCurveTo(len/2, -w/2, len/2, 0); x.quadraticCurveTo(len/2, w/2, len*.15, w/2); x.lineTo(-len/2, w/2); x.closePath(); x.fill();
  x.fillStyle = '#9a6a10'; x.fillRect(-len/2, -w/2, len*.12, w);
  x.fillStyle = 'rgba(255,255,255,.65)'; x.fillRect(-len*.4, -w*.32, len*.6, w*.14);
  x.restore();
}
const icons = {
  rain(x) {   // Золотой дождь
    frame(x, '#14102a', '#3a1d05');
    glow(x, 64, 104, 70, 'rgba(255,190,40,.55)');
    for (let i = 0; i < 26; i++) { const sx = (i*37)%118+5, sy = (i*53)%80; x.strokeStyle = 'rgba(255,215,90,'+(.25+(i%4)*.12)+')'; x.lineWidth = 1.5+(i%3);
      x.beginPath(); x.moveTo(sx, sy); x.lineTo(sx-6, sy+22); x.stroke(); }
    for (const [cx,cy,r,a] of [[30,40,9,2.1],[78,26,7,2.0],[100,60,10,1.95],[52,70,8,2.05],[18,86,7,2.0],[90,94,8,2.1]]) bullet(x, cx, cy, r*3.2, r*1.1, a);
    // лужа золота
    x.fillStyle = goldGrad(x, 0, 100, 0, 128); x.beginPath(); x.ellipse(64, 112, 54, 12, 0, 0, TAU); x.fill();
    for (const [cx, cy, r] of [[40,108,6],[70,114,7],[94,107,5],[60,104,4]]) coin(x, cx, cy, r, .45);
    for (let i=0;i<6;i++){ const a=-Math.PI/2+(i-2.5)*.35; x.strokeStyle='rgba(255,240,170,.9)'; x.lineWidth=2; x.beginPath(); x.moveTo(64+Math.cos(a)*14,104+Math.sin(a)*6); x.lineTo(64+Math.cos(a)*26,104+Math.sin(a)*14); x.stroke(); }
    vign(x,.55); border(x);
  },
  headshot(x) {   // Голдшот
    frame(x, '#2a0d08', '#120404');
    glow(x, 64, 58, 60, 'rgba(255,170,40,.35)');
    // силуэт головы и плеч
    x.fillStyle = '#060303'; x.beginPath(); x.arc(64, 62, 24, 0, TAU); x.fill();
    x.beginPath(); x.moveTo(20,128); x.quadraticCurveTo(26, 92, 64, 90); x.quadraticCurveTo(102, 92, 108, 128); x.fill();
    // попадание: золотая звезда
    x.save(); x.translate(70, 54); x.fillStyle = goldGrad(x,-24,-24,24,24);
    x.beginPath(); for (let i=0;i<16;i++){ const r = i%2?7:24-(i%4)*3, a = i/16*TAU; x.lineTo(Math.cos(a)*r, Math.sin(a)*r); } x.closePath(); x.fill();
    x.fillStyle = '#fffbe0'; x.beginPath(); x.arc(0,0,5,0,TAU); x.fill(); x.restore();
    // брызги-монеты
    for (const [cx,cy,r] of [[100,30,6],[104,62,5],[92,84,4],[30,30,4]]) coin(x,cx,cy,r,.8);
    // прицел
    x.strokeStyle = 'rgba(255,60,40,.95)'; x.lineWidth = 3;
    x.beginPath(); x.arc(64, 60, 36, 0, TAU); x.stroke();
    x.lineWidth = 2.5; x.beginPath(); x.moveTo(64,12); x.lineTo(64,40); x.moveTo(64,80); x.lineTo(64,108); x.moveTo(16,60); x.lineTo(44,60); x.moveTo(84,60); x.lineTo(112,60); x.stroke();
    vign(x,.5); border(x);
  },
  ladder(x) {   // Лестница Габилджона
    frame(x, '#2b5d8f', '#0e1f33');
    glow(x, 76, 18, 70, 'rgba(255,240,180,.85)');
    for (let i = 0; i < 7; i++) { x.strokeStyle = 'rgba(255,240,180,'+(.12+i*.02)+')'; x.lineWidth = 6; x.beginPath(); x.moveTo(76,10); x.lineTo(i*22-6, 128); x.stroke(); }
    // лестница в перспективе
    const L = t => [30 + t*30, 124 - t*104], R = t => [70 + t*18, 124 - t*104];
    x.lineCap = 'round';
    for (let k = 0; k < 9; k++) { const t = (k+.5)/9.5, a = L(t), b = R(t);
      x.strokeStyle = '#6a4508'; x.lineWidth = 6 - t*3; x.beginPath(); x.moveTo(a[0], a[1]+2); x.lineTo(b[0], b[1]+2); x.stroke();
      x.strokeStyle = goldGrad(x, a[0], a[1]-3, a[0], a[1]+3); x.lineWidth = 5 - t*2.5; x.beginPath(); x.moveTo(a[0], a[1]); x.lineTo(b[0], b[1]); x.stroke(); }
    for (const f of [L, R]) { const a = f(0), b = f(1);
      x.strokeStyle = '#6a4508'; x.lineWidth = 9; x.beginPath(); x.moveTo(a[0]+1, a[1]); x.lineTo(b[0]+1, b[1]); x.stroke();
      x.strokeStyle = goldGrad(x, a[0]-4, 0, a[0]+4, 0); x.lineWidth = 6; x.beginPath(); x.moveTo(a[0], a[1]); x.lineTo(b[0], b[1]); x.stroke(); }
    // прицел наверху
    x.strokeStyle = 'rgba(255,255,255,.95)'; x.lineWidth = 2.5; x.beginPath(); x.arc(76, 20, 11, 0, TAU); x.stroke();
    x.beginPath(); x.moveTo(76, 4); x.lineTo(76, 14); x.moveTo(76, 26); x.lineTo(76, 36); x.moveTo(60, 20); x.lineTo(70, 20); x.moveTo(82, 20); x.lineTo(92, 20); x.stroke();
    x.lineCap = 'butt'; vign(x,.45); border(x);
  },
  assassinate(x) {   // Хантерас пик
    frame(x, '#3a0505', '#0a0101');
    // вид через оптику
    const g = x.createRadialGradient(64,64,10,64,64,52); g.addColorStop(0,'#5a1a10'); g.addColorStop(.8,'#2a0806'); g.addColorStop(1,'#000');
    x.fillStyle = g; x.beginPath(); x.arc(64,64,52,0,TAU); x.fill();
    // золотое кольцо прицела
    x.strokeStyle = goldGrad(x, 0, 12, 0, 116); x.lineWidth = 7; x.beginPath(); x.arc(64,64,53,0,TAU); x.stroke();
    x.strokeStyle = '#3a2404'; x.lineWidth = 1.5; x.beginPath(); x.arc(64,64,49.5,0,TAU); x.stroke();
    // голова-цель
    x.fillStyle = 'rgba(0,0,0,.85)'; x.beginPath(); x.arc(64, 60, 15, 0, TAU); x.fill(); x.beginPath(); x.moveTo(36,116); x.quadraticCurveTo(40, 80, 64, 78); x.quadraticCurveTo(88, 80, 92, 116); x.fill();
    // перекрестие
    x.strokeStyle = '#ff3a2a'; x.lineWidth = 2; x.beginPath(); x.moveTo(64,14); x.lineTo(64,52); x.moveTo(64,68); x.lineTo(64,112); x.moveTo(14,60); x.lineTo(52,60); x.moveTo(76,60); x.lineTo(114,60); x.stroke();
    for (let i=1;i<4;i++){ x.beginPath(); x.moveTo(60, 60+i*10); x.lineTo(68, 60+i*10); x.stroke(); }
    x.fillStyle = '#ff3a2a'; x.beginPath(); x.arc(64,60,2.6,0,TAU); x.fill();
    // летящая золотая пуля со следом
    const tr = x.createLinearGradient(0,0,70,0); tr.addColorStop(0,'rgba(255,230,150,.95)'); tr.addColorStop(1,'rgba(255,220,120,0)');
    x.save(); x.translate(64,60); x.rotate(-2.55);
    x.fillStyle = tr; x.fillRect(26, -3, 54, 6); x.restore();
    bullet(x, 52, 51, 22, 8, -2.55 + Math.PI);
    glow(x, 64, 60, 18, 'rgba(255,220,120,.6)');
    vign(x,.35); border(x);
  },
};
window.out = {};
for (const k in icons) { const c = document.createElement('canvas'); c.width = c.height = 128; icons[k](c.getContext('2d')); window.out[k] = c.toDataURL('image/png'); document.body.appendChild(c); }
`;
(async () => {
  const b = await chromium.launch(); const p = await b.newPage();
  await p.setContent('<body style="background:#222;margin:0;display:flex;gap:8px;padding:8px"></body>');
  await p.evaluate(page_js);
  const out = await p.evaluate(() => window.out);
  for (const k in out) fs.writeFileSync(`/home/claude/max/icon_${k}.png`, Buffer.from(out[k].split(',')[1], 'base64'));
  await p.setViewportSize({ width: 560, height: 150 });
  await p.screenshot({ path: '/home/claude/max/icons_preview.png' });
  await b.close();
})();
