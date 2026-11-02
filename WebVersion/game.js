// ===================== МПКА 2 — мини-версия для телефона =====================
'use strict';
const TAU = Math.PI * 2;
const K = 0, M = 1;
const TEAM_NAME = ['Курчатов', 'Москва', 'Лес'];
const TEAM_COL = ['#4a8dff', '#ff4d40', '#d9a441'];
const NEUTRAL = 2;
const TEAM_DARK = ['#1f3f80', '#7a1d16', '#5a3d12'];
const rnd = (a, b) => a + Math.random() * (b - a);
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
const dist = (a, b) => Math.hypot(a.x - b.x, a.y - b.y);
const $ = id => document.getElementById(id);

// ---------- Картинки ----------
const IMG = {};
for (const k in IMG_SRC) { const im = new Image(); im.src = IMG_SRC[k]; IMG[k] = im; }
$('imgK').src = IMG_SRC.cityK; $('imgM').src = IMG_SRC.cityM;
$('pfL').src = IMG_SRC.faceL; $('pfR').src = IMG_SRC.faceR;

// ---------- Холст ----------
const cv = $('game'), ctx = cv.getContext('2d');
let W = 0, H = 0, DPR = 1;
function resize() {
  DPR = Math.min(window.devicePixelRatio || 1, 2);
  W = window.innerWidth; H = window.innerHeight;
  cv.width = Math.round(W * DPR); cv.height = Math.round(H * DPR);
}
window.addEventListener('resize', resize); resize();

// ===================== ДАННЫЕ =====================
const HERO = {
  name: 'Огр Mog', baseStr: 23, strGain: 3.5, baseAgi: 14, agiGain: 1.9, baseInt: 17, intGain: 2.5,
  baseDmg: 40, baseArmor: 3, range: 2.2, bat: 1.7, speed: 5.5
};
const ABIL = [
  { key: 'Q', name: 'Горячая пицца', target: 'enemy', max: 4, range: 6, color: '#ff8c1f',
    mana: [75, 85, 95, 105], cd: [11, 10, 9, 8], v: [[60, 120, 180, 240], [1.5]],
    desc: 'Швыряет раскалённую пиццу: {0} маг. урона и оглушение на {1} сек.' },
  { key: 'W', name: 'Острый соус', target: 'enemy', max: 4, range: 8, color: '#d81f19',
    mana: [90], cd: [15], v: [[20, 30, 40, 50], [5, 6, 7, 8], [20, 22, 24, 26]],
    desc: 'Обливает соусом: {0} урона/сек {1} сек. и замедление {2}%.' },
  { key: 'E', name: 'Энергетик', target: 'ally', max: 4, range: 6, color: '#3fc54a',
    mana: [50], cd: [20], v: [[20, 30, 40, 50], [10, 12, 14, 16], [20]],
    desc: '+{0} к скорости атаки и +{1}% к бегу на {2} сек. Нажми E дважды — выпить самому.' },
  { key: 'R', name: 'Двойная порция', target: 'passive', max: 3, ult: true, color: '#ffc83a',
    mana: [0], cd: [0], v: [[30, 40, 50], [10, 15, 20], [0, 5, 10]],
    desc: 'Пассивная ульта. Способности срабатывают по нескольку раз: x2 — {0}%, x3 — {1}%, x4 — {2}%.' },
];
const at = (arr, lvl) => arr[clamp(lvl - 1, 0, arr.length - 1)];
HERO.id = 'ogr_mog'; HERO.attr = 'str'; HERO.abil = ABIL;
// Саня Финд: копит сосиски вместо душ. Q/W/E — колбасные койлы (общий уровень), R — колбасный реквием.
const SANYA = {
  id: 'sanya_fiend', name: 'Саня Финд', attr: 'agi', baseStr: 19, strGain: 2.3, baseAgi: 20, agiGain: 3.2, baseInt: 18, intGain: 2.2,
  baseDmg: 30, baseArmor: 1, range: 5.5, bat: 1.6, speed: 5.6, ranged: true, look: 'sausage',
  perSausage: 2, maxSausage: 24, deathText: 'Даже моя колбаска не вечна...', deathVoice: 4,  // «Фраза 5» — «Даже моя колбаска не вечна»
  innate: 'Сосисочный запас: +1 сосиска за добитого крипа, +3 за героя (до 24). Каждая сосиска — +2 к урону. При смерти теряет половину.',
  abil: [
    { key: 'Q', name: 'Койл близко', target: 'none', max: 4, share: 'coil', dist: 2.5, rad: 2.4, range: 2.5, color: '#c8322b',
      mana: [75, 80, 85, 90], cd: [10], v: [[90, 150, 210, 270], [3]],
      desc: 'Колбаски падают в круг в 2.5 м перед Саней: {0} маг. урона + {1} за каждую сосиску. Q, W и E качаются вместе.' },
    { key: 'W', name: 'Койл средне', target: 'none', max: 4, share: 'coil', dist: 5, rad: 2.4, range: 5, color: '#c8322b',
      mana: [75, 80, 85, 90], cd: [10], v: [[90, 150, 210, 270], [3]],
      desc: 'Колбаски падают в круг в 5 м перед Саней: {0} маг. урона + {1} за каждую сосиску.' },
    { key: 'E', name: 'Койл далеко', target: 'none', max: 4, share: 'coil', dist: 7.5, rad: 2.4, range: 7.5, color: '#c8322b',
      mana: [75, 80, 85, 90], cd: [10], v: [[90, 150, 210, 270], [3]],
      desc: 'Колбаски падают в круг в 7.5 м перед Саней: {0} маг. урона + {1} за каждую сосиску.' },
    { key: 'R', name: 'Колбасный реквием', short: 'Реквием', target: 'none', max: 3, ult: true, range: 9, color: '#ff7a1a',
      mana: [150, 175, 200], cd: [120, 110, 100], v: [[80, 110, 140], [3]],
      desc: 'Саня 1.7 сек. собирает сосиски, потом стреляет колбасками во все стороны: по одной за сосиску (минимум {1}), каждая — {0} маг. урона.' },
  ],
};
// Макс Gold: снайпер с золотой снайперкой. Q — Золотой дождь (заряды), W — Голдшот (пассивка), E — Лестница Габилджона, R — Хантерас пик.
const MAX = {
  id: 'max_gold', name: 'Макс Gold', attr: 'agi', baseStr: 17, strGain: 1.9, baseAgi: 21, agiGain: 3.2, baseInt: 15, intGain: 2.4,
  baseDmg: 32, baseArmor: 1, range: 5.5, bat: 1.7, speed: 5.3, ranged: true, look: 'goldbullet', bulletSpeed: 42, rangeAbil: 2,
  deathText: 'Макс Gold пал...', deathVoice: 5,   // voice-message 9
  botSkill: [3, 0, 1, 0, 1, 0, 1, 2, 1, 2, 2, 2, 0],
  innate: 'Золотая снайперка: Лестница Габилджона даёт дальность атаки, а Хантерас пик достаёт врага через полкарты.',
  abil: [
    { key: 'Q', name: 'Золотой дождь', short: 'Дождь', target: 'point', icon: 'rain', max: 4, range: 18, rad: 4.5, charges: 3, chargeCd: 35, color: '#ffc23a',
      mana: [50], cd: [0], v: [[20, 45, 70, 95], [15, 20, 25, 30], [10]],
      desc: 'Через 1.2 сек. на круг радиусом 4.5 льётся золото: {0} маг. урона в сек. и замедление {1}% на {2} сек. Под дождём всё видно. 3 заряда.' },
    { key: 'W', name: 'Голдшот', target: 'passive', icon: 'headshot', max: 4, color: '#e0a020',
      mana: [0], cd: [0], v: [[40], [20, 50, 80, 110]],
      desc: 'Пассивка. Выстрел с шансом {0}% попадает в голову: +{1} урона, цель отбрасывает и на 0.5 сек. почти останавливает.' },
    { key: 'E', name: 'Лестница Габилджона', short: 'Лестница', target: 'none', icon: 'ladder', max: 4, color: '#5aa0e0',
      mana: [50], cd: [20, 18, 16, 14], v: [[1, 2, 3, 4], [2], [3]],
      desc: 'Пассивно: +{0} к дальности атаки. Нажми — Макс залезает на золотую лестницу: ещё +{1} к дальности и каждый выстрел — Голдшот, {2} сек. На лестнице бегает на 30% медленнее.' },
    { key: 'R', name: 'Хантерас пик', short: 'Хантерас', target: 'enemy', icon: 'assassinate', max: 3, ult: true, range: 30, color: '#d8321e',
      mana: [175, 225, 275], cd: [20, 15, 10], v: [[320, 485, 650], [2]],
      desc: 'Макс {1} сек. целится через оптику (цель подсвечена), потом стреляет из золотой снайперки прямо в голову: {0} маг. урона и короткое оглушение. Если Макса оглушить — прицел сбит.' },
  ],
};
const HEROES = [HERO, SANYA, MAX];
// ===================== ЭЛЕКТРИЧКА, КУРЬЕР, СЧЁТ =====================
const PASSIVE_GOLD = 1.6, TP_CD = 40, TP_CHANNEL = 3, COURIER_SPEED = 7.2;
const SLOT_COL = ['#3375ff', '#66ffbf', '#bf00bf', '#f3f00b', '#ff6b00', '#fe86c2', '#a1b447', '#65d9f7', '#008321', '#a46900'];
let matchT0 = 0, tpPick = false, tpPickAt = -10, courier = null, scoreHold = false, scoreTap = false, inspect = null;
function initHeroStats(h) { Object.assign(h, { tpN: 1, tpCd: 0, stash: [], kills: 0, deaths: 0, assists: 0, lh: 0, dn: 0, hitBy: new Map() }); }

// --- Билет на электричку: свой слот (T), к любой своей башне, трону или в баню ---
const canTp = u => u.alive && u.tpN > 0 && u.tpCd <= 0 && !u.tp && !u.channel && !stunned(u);
function startTicket(u, x, y, label) {
  u.tpN--; u.tpCd = TP_CD;
  u.tp = { end: T + TP_CHANNEL, x: u.x, y: u.y, dx: x, dy: y };
  u.order = null; u.target = null; if (u === hero) pending = null;
  say(label, '#8fd0ff', u);
}
function pressTicket() {
  if (!hero || !hero.alive || paused || over) return;
  if (hero.tpN <= 0) return toast('Нет билета — купи в магазине (100)', '#ff8a80');
  if (hero.tpCd > 0) return toast('Электричка: ещё ' + Math.ceil(hero.tpCd) + ' сек.', '#ff8a80');
  if (hero.tp) return;
  if (tpPick && performance.now() - tpPickAt < 600) { tpPick = false; const b = BATHS[hero.team]; startTicket(hero, b.x, b.y, 'Еду в баню...'); refreshHud(true); return; }
  tpPick = true; tpPickAt = performance.now(); targeting = -1;
  toast('Куда едем? Тапни у своей башни или на мини-карте (ещё раз T — в баню)', '#8fd0ff');
  refreshHud(true);
}
function ticketAt(x, y) {
  if (!canTp(hero)) return pressTicket();
  const b = BATHS[hero.team];
  let best = { x: b.x, y: b.y }, bd = Math.hypot(x - b.x, y - b.y), where = 'в баню';
  for (const e of ents) if (e.alive && e.team === hero.team && (e.kind === 'tower' || e.kind === 'throne')) {
    const d = Math.hypot(x - e.x, y - e.y); if (d < bd) { bd = d; best = e; where = e.kind === 'throne' ? 'к трону' : 'к башне'; }
  }
  let ox = x - best.x, oy = y - best.y, ol = Math.hypot(ox, oy);
  if (ol < 3) { ox = b.x - best.x; oy = b.y - best.y; ol = Math.hypot(ox, oy) || 1; ox = ox / ol * 3.5; oy = oy / ol * 3.5; ol = 3.5; }
  const k = Math.min(1, 4.5 / ol);
  startTicket(hero, best.x + ox * k, best.y + oy * k, 'Еду ' + where + '...');
  refreshHud(true);
}
function drawTpRing(tp, p) {
  const x = sx(tp.dx), y = sy(tp.dy), k = 1 - clamp((tp.end - T) / TP_CHANNEL, 0, 1);
  ctx.strokeStyle = 'rgba(120,200,255,.9)'; ctx.lineWidth = 2.5; ctx.setLineDash([6, 4]);
  ctx.beginPath(); ctx.ellipse(x, y, 1.6 * p, 1.4 * p, T * 2, 0, TAU); ctx.stroke(); ctx.setLineDash([]);
  ctx.fillStyle = `rgba(120,200,255,${.12 + .25 * k})`; ctx.beginPath(); ctx.ellipse(x, y, 1.6 * p * k, 1.4 * p * k, 0, 0, TAU); ctx.fill();
}

// --- Курьер: несёт вещи из тайника герою. Есть у каждого героя, боты зовут своего сами ---
// «Самокат курьеру» (магазин): быстрее и крепче
const SCOOTER_SPEED = 11.5, SCOOTER_HP = 450;
function makeCourier(h) {
  const b = BATHS[h.team];
  const c = unit({ kind: 'creep', courier: true, team: h.team, x: b.x + 2.5, y: b.y - 2.5, r: .45, hp: 250, maxHp: 250, armor: 0, regen: 2,
    dmg: 0, range: 0, bat: 1, speed: COURIER_SPEED, name: 'Курьер', xpB: 0, goldB: 100 });
  c.state = 'idle'; c.carry = []; c.respawn = 0; c.owner = h; h.courier = c;
  ents.push(c); return c;
}
function upgradeCourier(c) {
  if (!c || c.up) return false;
  c.up = true; c.speed = SCOOTER_SPEED;
  const k = c.alive ? c.hp / c.maxHp : 1; c.maxHp = SCOOTER_HP; c.hp = SCOOTER_HP * k;
  if (c.owner === hero) toast('Курьер на самокате!', '#ffd24a');
  return true;
}
function giveItem(h, d) {
  if (d.id === 'ticket') { if (h.tpN >= 5) return false; h.tpN++; return true; }
  const i = h.inv.findIndex(x => !x); if (i < 0) return false;
  h.inv[i] = { d, cd: 0 }; recalc(h, true); return true;
}
function courierAI(c, dt) {
  const b = BATHS[c.team], h = c.owner, mine = h === hero, bx = b.x + 2.5, by = b.y - 2.5, atBase = Math.hypot(c.x - b.x, c.y - b.y) <= b.r + 3;
  if (c.state === 'toBase') {
    moveToward(c, bx, by, dt);
    if (atBase) { c.carry.push(...h.stash); h.stash.length = 0; c.state = c.carry.length && h.alive ? 'toHero' : 'idle'; if (mine) refreshHud(true); }
  } else if (c.state === 'toHero') {
    if (!h.alive) { c.state = 'back'; return; }
    if (Math.hypot(c.x - h.x, c.y - h.y) < 2.4) {
      for (let i = c.carry.length - 1; i >= 0; i--) if (giveItem(h, c.carry[i])) c.carry.splice(i, 1);
      if (mine) { if (c.carry.length) toast('У героя нет места — курьер несёт вещи обратно', '#ffb08a'); else toast('Вещи доставлены!', '#7dff8a'); }
      burst(h.x, h.y - 2, '#ffd24a', mine ? 14 : 8);
      c.state = 'back'; if (mine) refreshHud(true);
    } else moveToward(c, h.x, h.y, dt);
  } else if (c.state === 'back') {
    moveToward(c, bx, by, dt);
    if (atBase) { while (c.carry.length && h.stash.length < 6) h.stash.push(c.carry.shift()); c.carry = []; c.state = 'idle'; if (mine) refreshHud(true); }
  } else if (!atBase) moveToward(c, bx, by, dt);
}
// Отправить курьера героя h (боты — молча)
function sendCourier(h) {
  const c = h.courier;
  if (!c || !c.alive || !h.alive || (!c.carry.length && !h.stash.length)) return false;
  c.state = c.carry.length ? 'toHero' : 'toBase'; return true;
}
function courierDeliver() {
  if (!courier || !hero || paused || over) return;
  if (!courier.alive) return toast('Курьер убит — вернётся через ' + Math.ceil(courier.respawn - T) + ' сек.', '#ff8a80');
  if (!courier.carry.length && !hero.stash.length) return toast('Тайник пуст — купи что-нибудь в магазине вдали от бани', '#ffb08a');
  if (!hero.alive) return toast('Ты мёртв — вещи заберёшь у бани', '#ffb08a');
  sendCourier(hero);
  toast('Курьер в пути!', '#7dff8a'); refreshHud(true);
}
function courierDied(c, src) {
  const h = c.owner;
  while (c.carry.length && h.stash.length < 6) h.stash.push(c.carry.shift());
  c.carry = []; c.state = 'dead'; c.respawn = T + 40;
  if (h === hero) toast('Курьера убили!', '#ff8a80');
}
function courierRespawn(c) {
  const b = BATHS[c.team];
  Object.assign(c, { alive: true, hp: c.maxHp, x: b.x + 2.5, y: b.y - 2.5, st: St(), state: 'idle', target: null });
}
function stashAutoGive() {
  if (!hero || !hero.alive || !hero.stash || !hero.stash.length || !nearShop()) return;
  for (let i = hero.stash.length - 1; i >= 0; i--) if (giveItem(hero, hero.stash[i])) hero.stash.splice(i, 1);
}
let courierImg = null;
function drawCourier(e, x, y, p) {
  shadow(e.x, e.y, .6);
  ctx.strokeStyle = TEAM_COL[e.team]; ctx.lineWidth = Math.max(1.5, p * .08); ctx.beginPath(); ctx.ellipse(x, y, .6 * p, .25 * p, 0, 0, TAU); ctx.stroke();
  if (e.up) {   // самокат: колёса, зелёная дека, руль; пацан стоит выше
    ccirc(ctx, x - .5 * p, y - .02 * p, .13 * p, '#0d0d0d'); ccirc(ctx, x + .55 * p, y - .02 * p, .13 * p, '#0d0d0d');
    ctx.fillStyle = '#4dd95a'; ctx.fillRect(x - .62 * p, y - .2 * p, 1.2 * p, .1 * p);
    ctx.strokeStyle = '#222'; ctx.lineWidth = Math.max(1.5, p * .07);
    ctx.beginPath(); ctx.moveTo(x + .55 * p, y - .1 * p); ctx.lineTo(x + .5 * p, y - 1.15 * p); ctx.moveTo(x + .32 * p, y - 1.15 * p); ctx.lineTo(x + .68 * p, y - 1.15 * p); ctx.stroke();
    y -= .2 * p;
  }
  ctx.fillStyle = '#26262e'; ctx.fillRect(x - .25 * p, y - .55 * p, .2 * p, .5 * p); ctx.fillRect(x + .05 * p, y - .55 * p, .2 * p, .5 * p);
  ctx.fillStyle = '#f2f2f2'; ctx.beginPath(); ctx.ellipse(x - .16 * p, y - .05 * p, .17 * p, .09 * p, 0, 0, TAU); ctx.ellipse(x + .16 * p, y - .05 * p, .17 * p, .09 * p, 0, 0, TAU); ctx.fill();
  // золотой рюкзак
  ctx.fillStyle = '#c88a14'; ctx.fillRect(x - .38 * p, y - 1.35 * p, .76 * p, .6 * p);
  ctx.fillStyle = '#ffd24a'; ctx.fillRect(x - .34 * p, y - 1.31 * p, .68 * p, .18 * p);
  // пуховик
  ctx.fillStyle = '#111114'; ctx.beginPath(); ctx.ellipse(x, y - .9 * p, .42 * p, .42 * p, 0, 0, TAU); ctx.fill();
  ctx.strokeStyle = '#2c2c34'; ctx.lineWidth = Math.max(1, p * .04);
  for (const yy of [.75, 1.0]) { ctx.beginPath(); ctx.moveTo(x - .4 * p, y - yy * p); ctx.quadraticCurveTo(x, y - (yy - .06) * p, x + .4 * p, y - yy * p); ctx.stroke(); }
  ctx.strokeStyle = '#ffd24a'; ctx.beginPath(); ctx.moveTo(x, y - 1.25 * p); ctx.lineTo(x, y - .55 * p); ctx.stroke();
  // голова — лицо с фото
  const hx = x, hy = y - 1.62 * p, r = .4 * p;
  if (!courierImg && typeof COURIER_FACE !== 'undefined') { courierImg = new Image(); courierImg.src = COURIER_FACE; }
  ccirc(ctx, hx, hy, r, '#e8c39a');
  if (e.owner === hero && courierImg && courierImg.complete && courierImg.naturalWidth) { ctx.save(); ctx.beginPath(); ctx.arc(hx, hy, r, 0, TAU); ctx.clip(); ctx.drawImage(courierImg, hx - r, hy - r, r * 2, r * 2); ctx.restore(); }
  ctx.strokeStyle = 'rgba(0,0,0,.4)'; ctx.lineWidth = 1; ctx.beginPath(); ctx.arc(hx, hy, r, 0, TAU); ctx.stroke();
  if (e.carry && e.carry.length) { ctx.fillStyle = '#ffd24a'; ctx.font = `800 ${Math.max(9, p * .35)}px Rubik, system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.fillText('×' + e.carry.length, x + .6 * p, y - 1.9 * p); }
}

// --- Таблица счёта и карточка героя ---
function showScore(on) { scoreHold = on; refreshHud(true); }
const ultOf = h => { const i = AB(h).findIndex(a => a.ult); return i >= 0 && AB(h)[i].target !== 'passive' ? h.abil[i] : null; };
const netWorth = h => h.gold + h.inv.reduce((a, it) => a + (it ? totalCost(it.d) : 0), 0);
const nickOf = h => h === hero ? 'Ты' : (h.nick || 'бот');
function renderScore() {
  let html = '', slot = 0;
  for (const t of [K, M]) {
    html += `<div class="th"><span></span><span class="kc">${teamKills[t]}</span><span class="tn" style="color:${TEAM_COL[t]}">${t === K ? 'КУРЧАТОВ' : 'МОСКВА'}</span><span>УР.</span><span>ЗОЛОТО</span><span>У</span><span>С</span><span>П</span><span>УЛЬТ</span></div>`;
    for (const h of heroes.filter(x => x.team === t)) {
      const ally = t === playerTeam, u = ultOf(h);
      html += `<div class="row${h === hero ? ' me' : ''}" data-hi="${heroes.indexOf(h)}"><div class="cs" style="background:${SLOT_COL[slot++]}"></div>
        <div class="pf">${faces(h.def)}${h.alive ? '' : `<span class="tm">${Math.ceil(h.respawn - T)}</span>`}</div>
        <div class="nm"><b>${nickOf(h)}</b><span>${h.def.name}</span></div><div class="c">${h.lvl}</div>
        <div class="c">${ally ? `<div class="gold2">${h.gold}</div>` : ''}</div><div class="c">${h.kills}</div><div class="c">${h.deaths}</div><div class="c">${h.assists}</div>
        <div class="c">${ally && u && u.lvl ? `<div class="ultd" style="background:${u.cd > 0 ? '#777' : '#58e35f'}"></div>` : ''}</div></div>`;
    }
  }
  $('scoreOv').innerHTML = html + '<div class="hint">Держи Ё (или кнопка «СЧЁТ»). Тап по строке — инвентарь и способности.</div>';
}
function renderInspect() {
  const h = inspect; if (!h) return;
  const ally = h.team === playerTeam, fog = !ally && h.alive && !canSee(playerTeam, h), tc = TEAM_COL[h.team];
  const abil = AB(h).map((a, i) => { const s = h.abil[i], ic = abilIcon(a);
    return `<div class="ab${s.lvl ? '' : ' off'}" style="${ic ? `background-image:url(${ic})` : `background:${a.color}`}" title="${a.name}"><span class="k">${a.key}</span>${ic ? '' : (a.short || a.name)}
      <div class="lv">${'<i></i>'.repeat(a.max).replace(/<i><\/i>/g, (m, k) => m)}</div>${s.lvl && s.cd > 0 ? `<div class="cd">${Math.ceil(s.cd)}</div>` : ''}</div>`; }).join('')
    .replace(/<div class="lv">(.*?)<\/div>/g, m => m);
  const pips = (lvl, max) => Array.from({ length: max }, (_, k) => `<i class="${k < lvl ? 'on' : ''}"></i>`).join('');
  const abil2 = AB(h).map((a, i) => { const s = h.abil[i], ic = abilIcon(a);
    return `<div class="ab${s.lvl ? '' : ' off'}" style="${ic ? `background-image:url(${ic})` : `background:${a.color}`}"><span class="k">${a.key}</span>${ic ? '' : (a.short || a.name)}<div class="lv">${pips(s.lvl, a.max)}</div>${s.lvl && s.cd > 0 ? `<div class="cd">${Math.ceil(s.cd)}</div>` : ''}</div>`; }).join('');
  void abil;
  const items = h.inv.map((it, i) => { const pic = it && ITEM_IMG[it.d.id];
    return `<div class="it${i > 5 ? ' bp' : ''}" style="${pic ? `background-image:url(${pic})` : it ? `background:${it.d.c}` : ''}">${it && !pic ? it.d.name : ''}</div>`; }).join('');
  $('heroCard').style.setProperty('--c', tc);
  $('heroCard').innerHTML = `<button class="x" data-x="1">✕</button>
    <div class="hd"><div class="pf">${faces(h.def)}</div><div><h4>${h.def.name} <span style="font-size:13px;color:#aaa">(${nickOf(h)})</span></h4>
      <div class="sub"><span style="color:${tc}">${h.team === K ? 'Курчатов' : 'Москва'}</span> · Уровень <b>${h.lvl}</b> · У/С/П <b>${h.kills}/${h.deaths}/${h.assists}</b>${fog ? ' · <span style="color:#8fa0d8">в тумане</span>' : ''}</div>
      <div class="sub">${ally ? `Золото <b style="color:#ffc83a">${h.gold}</b> · ` : ''}Ценность <b style="color:#ffc83a">${netWorth(h)}</b>${h.alive ? '' : ` · <b style="color:#ff8a80">воскреснет через ${Math.ceil(h.respawn - T)}</b>`}</div></div></div>
    <div class="bars2"><div class="b"><i style="width:${h.hp / h.maxHp * 100}%;background:#3aa83a"></i><span>${Math.ceil(h.hp)} / ${Math.round(h.maxHp)}</span></div>
      <div class="b"><i style="width:${h.mp / h.maxMp * 100}%;background:#3a6cff"></i><span>${Math.ceil(h.mp)} / ${Math.round(h.maxMp)}</span></div></div>
    <div class="grid2"><div class="st">Урон <b>${Math.round(h.dmg)}</b><br>Броня <b>${h.armor.toFixed(1)}</b><br>Скор. атаки <b>${Math.round(h.as)}</b><br>Дальность <b>${h.range.toFixed(1)}</b><br>Скорость <b>${Math.round(speedOf(h) * 60)}</b><br>
      <span style="color:#ff7a66">Сил ${Math.round(h.str)}</span> <span style="color:#6fe07a">Лов ${Math.round(h.agi)}</span> <span style="color:#6fa8ff">Инт ${Math.round(h.int)}</span></div>
      <div><div class="lbl">СПОСОБНОСТИ</div><div class="abs">${abil2}</div><div class="lbl" style="margin-top:6px">ПРЕДМЕТЫ</div><div class="its">${items}</div></div></div>`;
}
function openInspect(h) { inspect = inspect === h ? null : h; scoreTap = false; refreshHud(true); }
const AB = h => (h && h.def ? h.def : HERO).abil;
function assignAllyLanes(myLane) {
  const pool = LANE_POOL.slice(); const k = pool.indexOf(myLane); if (k >= 0) pool.splice(k, 1); else pool.pop();
  shuffle(pool).forEach((l, i) => { const allies = bots.filter(b => b.team === playerTeam); if (allies[i]) allies[i].lane = l; });
}
const pickRandom = arr => arr[Math.floor(Math.random() * arr.length)];
function setHeroDef(h, def) {
  h.def = def; h.name = def.name + (h.bot ? ` (${h.nick || 'бот'})` : '');
  h.abil = def.abil.map(a => ({ lvl: 0, cd: 0, ch: a.charges || 0, chT: 0 })); h.sausage = 0; h.channel = null; h.ladder = 0;
  recalc(h, false);
}
function addSausage(h, n) {
  if (!h.def || !h.def.perSausage) return;
  const old = h.sausage || 0; h.sausage = clamp(old + n, 0, h.def.maxSausage);
  if (h.sausage !== old) { recalc(h, true); if (n > 0 && h === hero) floatText(h.x, h.y - 4.2, '+' + n + ' сосиск' + (n === 1 ? 'а' : 'и'), '#ff9a50', 13); }
}
function joinLv(arr, cur) {
  if (arr.every(v => v === arr[0])) return String(arr[0]);
  return arr.map((v, i) => i === cur - 1 ? `<span class="hl">${v}</span>` : v).join('/');
}

// ---------- Предметы ----------
const ITEMS = [
  { id: 'shawarma', name: 'Шаурма', cat: 0, cost: 110, c: '#d99a4d', lore: 'С двойным мясом. Говорят, лечит всё.', act: 'heal', av: 50, ad: 8, cons: true },
  { id: 'chips', name: 'Чипсы', cat: 0, cost: 60, c: '#ffd833', lore: 'Со вкусом краба.', act: 'mana', av: 15, ad: 10, cons: true },
  { id: 'ticket', name: 'Билет на электричку', cat: 0, cost: 100, c: '#66b3e6', lore: 'До бани без пересадок.', act: 'tp', ad: 3, cons: true },
  { id: 'scooter', name: 'Самокат курьеру', cat: 0, cost: 150, c: '#4dd95a', lore: 'Электросамокат для курьера: в 1.6 раза быстрее и 450 здоровья. Один раз, в инвентарь не идёт.' },
  { id: 'seeds', name: 'Семечки', cat: 1, cost: 50, c: '#6b4f33', lore: 'Кулёк с рынка.', s: { str: 1, agi: 1, int: 1 } },
  { id: 'dumbbell', name: 'Гантелька', cat: 1, cost: 140, c: '#c0332e', lore: 'Три кило. Для начала хватит.', s: { str: 3 } },
  { id: 'rope', name: 'Скакалка', cat: 1, cost: 140, c: '#40b34d', lore: 'Из школьного спортзала.', s: { agi: 3 } },
  { id: 'notebook', name: 'Тетрадка', cat: 1, cost: 140, c: '#4080e6', lore: 'В клетку, с ответами на полях.', s: { int: 3 } },
  { id: 'tracksuit', name: 'Олимпийка', cat: 1, cost: 800, c: '#2a2a33', lore: 'Тёплая, с лампасами.', s: { hp: 200 } },
  { id: 'energos', name: 'Энергос', cat: 1, cost: 800, c: '#33ccd9', lore: 'Литровая банка.', s: { mp: 200 } },
  { id: 'brass', name: 'Кастет', cat: 2, cost: 450, c: '#9a9aa6', lore: 'Найден у гаражей.', s: { dmg: 10 } },
  { id: 'gloves', name: 'Перчатки без пальцев', cat: 2, cost: 450, c: '#5a4033', lore: 'Удобно бить и листать телефон.', s: { as: 20 } },
  { id: 'mail', name: 'Кольчуга из крышек', cat: 2, cost: 550, c: '#b3b38c', lore: 'Собирали всем подъездом.', s: { armor: 4 } },
  { id: 'bat', name: 'Бита', cat: 2, cost: 1000, c: '#a6733f', lore: 'В бейсбол никто не играет.', s: { dmg: 18 } },
  { id: 'slippers', name: 'Тапки', cat: 3, cost: 500, c: '#e66699', lore: 'С зайчиками. Зато быстро.', s: { ms: 0.5 } },
  { id: 'sausage', name: 'Сосиска в тесте', cat: 3, cost: 500, c: '#e68c59', lore: 'Из школьной столовой.', s: { hpr: 4 } },
  { id: 'kefir', name: 'Кефир', cat: 3, cost: 175, c: '#f2f2f2', lore: 'Полезно для головы.', s: { mpr: 1.2 } },
  { id: 'ushanka', name: 'Ушанка', cat: 3, cost: 500, c: '#735a4d', lore: 'От мороза и от магии.', s: { mr: 0.15 } },
  { id: 'band_str', name: 'Браслет из резинок', cat: 4, cost: 210, parts: ['seeds', 'dumbbell'], c: '#e64d40', lore: 'Сплела сестра.', s: { str: 5, agi: 2, int: 2, dmg: 3, hpr: 0.75 } },
  { id: 'band_agi', name: 'Чётки', cat: 4, cost: 210, parts: ['seeds', 'rope'], c: '#4dd959', lore: 'Щёлкаешь — руки быстрее.', s: { agi: 5, str: 2, int: 2, as: 5, armor: 1 } },
  { id: 'band_int', name: 'Шпаргалка', cat: 4, cost: 210, parts: ['seeds', 'notebook'], c: '#4d8cff', lore: 'Мелким почерком.', s: { int: 5, str: 2, agi: 2, mpr: 0.6 } },
  { id: 'sneakers', name: 'Разные кроссовки', cat: 4, cost: 0, parts: ['slippers', 'gloves', 'dumbbell'], c: '#d92626', lore: 'Левый красный, правый белый.', s: { ms: 0.55, as: 25, str: 10 } },
  { id: 'phase', name: 'Самоходы', cat: 4, cost: 0, parts: ['slippers', 'mail', 'brass'], c: '#e69933', lore: 'Сами несут в драку.', s: { ms: 0.6, armor: 5, dmg: 18 }, act: 'speed', av: 25, ad: 3, acd: 8 },
  { id: 'crystal', name: 'Дубина Mega', cat: 4, cost: 500, parts: ['brass', 'bat'], c: '#bf4de6', lore: 'Слышно в Москве.', s: { dmg: 32, crit: 0.2, critM: 1.75 } },
  { id: 'vanguard', name: 'Пуховик', cat: 4, cost: 0, parts: ['tracksuit', 'sausage'], c: '#404d73', lore: 'В нём не больно.', s: { hp: 250, hpr: 5, block: 0.5, blockA: 15 } },
  { id: 'hood', name: 'Ушанка деда', cat: 4, cost: 450, parts: ['ushanka', 'sausage'], c: '#8c664d', lore: 'Магия отскакивает.', s: { mr: 0.25, hpr: 6, armor: 2 } },
  { id: 'shampur', name: 'Шампур', cat: 4, cost: 400, parts: ['bat', 'sausage', 'kefir'], c: '#ccccd9', lore: 'Насаживает сразу несколько.', s: { dmg: 30, hpr: 4.5, mpr: 1.5, cleave: 0.4 } },
  { id: 'mangal', name: 'Мангал', cat: 4, cost: 1000, parts: ['bat', 'mail'], c: '#ff731a', lore: 'Враги рядом поджариваются.', s: { dmg: 30, armor: 3, burn: 25 } },
  { id: 'bkb', name: 'Чёрный пояс', cat: 4, cost: 600, parts: ['tracksuit', 'bat'], c: '#141414', lore: 'По карате. Магия не берёт.', s: { str: 10, dmg: 24, hp: 100 }, act: 'immune', ad: 6, acd: 70 },
  { id: 'travel', name: 'Сапоги-скороходы', cat: 4, cost: 1500, parts: ['sneakers'], c: '#ffcc4d', lore: 'Сами возят до бани.', s: { ms: 0.75, as: 30, str: 12 }, act: 'tp', ad: 3, acd: 45 },
  { id: 'crown', name: 'Корона Курчатова', cat: 4, cost: 1200, parts: ['crystal', 'vanguard'], c: '#ffd926', lore: 'Только для тех, кто за Курчатов реально убьёт.', s: { dmg: 45, hp: 350, hpr: 7, crit: 0.25, critM: 2.2, block: 0.6, blockA: 25 } },
];
const CATS = ['РАСХОДНИКИ', 'АТРИБУТЫ', 'СНАРЯЖЕНИЕ', 'РАЗНОЕ', 'УЛУЧШЕНИЯ'];
const ITEM = {}; ITEMS.forEach(d => { d.parts = d.parts || []; d.s = d.s || {}; ITEM[d.id] = d; });
const totalCost = d => d.cost + d.parts.reduce((a, p) => a + totalCost(ITEM[p]), 0);
const usedIn = id => ITEMS.filter(d => d.parts.includes(id));
function describe(d) {
  const s = d.s, L = [];
  const add = (v, t, k = 1) => { if (v) L.push(`+${+(v * k).toFixed(2)} ${t}`); };
  add(s.str, 'к силе'); add(s.agi, 'к ловкости'); add(s.int, 'к интеллекту'); add(s.dmg, 'к урону'); add(s.as, 'к скорости атаки');
  add(s.armor, 'к броне'); add(s.hp, 'к здоровью'); add(s.mp, 'к мане'); add(s.hpr, 'к реген. здоровья'); add(s.mpr, 'к реген. маны');
  add(s.ms, 'к скорости бега', 100); add(s.mr, '% к защите от магии', 100);
  if (s.crit) L.push(`<span style="color:#ff8080">Крит: ${s.crit * 100}% шанс ударить в ${s.critM} раза сильнее</span>`);
  if (s.cleave) L.push(`<span style="color:#ffb080">Рассечение: ${s.cleave * 100}% урона по врагам вокруг цели</span>`);
  if (s.block) L.push(`<span style="color:#a0c0ff">Блок: ${s.block * 100}% шанс заблокировать ${s.blockA} урона</span>`);
  if (s.burn) L.push(`<span style="color:#ffa040">Жар: ${s.burn} урона/сек врагам в радиусе 6</span>`);
  const A = { heal: `Съесть: +${d.av * d.ad} здоровья за ${d.ad} сек.`, mana: `Съесть: +${d.av * d.ad} маны за ${d.ad} сек.`,
    tp: `Применить: через ${d.ad} сек. телепорт в свою баню`, speed: `Применить: +${d.av}% к бегу на ${d.ad} сек.`,
    immune: `Применить: иммунитет к магии на ${d.ad} сек.` }[d.act];
  if (A) L.push(`<span class="hl">${A}</span>${d.acd ? ` <span class="cost">(перезарядка ${d.acd} сек.)</span>` : ''}${d.cons ? ' <span style="color:#aaa">(одноразовый)</span>' : ''}`);
  return L.join('<br>');
}

// ===================== МИР =====================
let ents = [], projs = [], parts = [], texts = [], timers = [], decor = [], bubbles = [];
let heroes = [], bots = [], bot = null, teamKills = [0, 0];   // bot — первый вражеский бот (для совместимости)
const NICKS = [['Петя', 'Вова', 'Димон', 'Лёха', 'Серый'], ['Артём', 'Кирилл', 'Макс', 'Тимур', 'Егор']];
const LANE_POOL = ['top', 'mid', 'bot', 'jungle', 'bot'];
const shuffle = a => { a = a.slice(); for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(Math.random() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; } return a; };
// Глиф: 5 сек неуязвимости башням, трону и крипам команды. Перезарядка 2 минуты.
const GLYPH_DUR = 5, GLYPH_CD = 120;
let glyph = [{ end: 0, ready: 0 }, { end: 0, ready: 0 }];
const glyphOn = u => u.team !== NEUTRAL && glyph[u.team] && T < glyph[u.team].end && (u.kind === 'tower' || u.kind === 'throne' || (u.kind === 'creep' && !u.neutral));
function useGlyph(team) {
  const gl = glyph[team];
  if (T < gl.ready) { if (team === playerTeam) toast('Глиф перезаряжается: ' + Math.ceil(gl.ready - T) + ' сек', '#ff8a80'); return false; }
  gl.end = T + GLYPH_DUR; gl.ready = T + GLYPH_CD;
  for (const e of ents) if (e.alive && e.team === team && (e.kind === 'tower' || e.kind === 'throne' || e.kind === 'creep')) burst(e.x, e.y - 1.5, '#ffd84a', e.kind === 'creep' ? 5 : 16);
  toast(team === playerTeam ? 'ГЛИФ! Твои строения и крипы неуязвимы 5 сек' : 'Враг включил глиф!', '#ffd84a');
  return true;
}
let T = 0, paused = true, over = false, playerTeam = K, hero = null, nextWave = 5, waveNo = 0;
let cam = { x: 0, y: 0, ppu: 26, follow: true };
let goldTick = 0;

const BATHS = [{ team: K, x: -36, y: 36, r: 4.5 }, { team: M, x: 36, y: -36, r: 4.5 }];
const HUTS = [{ x: -30, y: 42 }, { x: 42, y: -30 }];
// Три линии. Для каждой: точка выхода крипов и путь для Курчатова [0] и Москвы [1]
const P = (x, y) => ({ x, y });
const LANE_DEF = {
  top: { ru: 'верх', spawn: [P(-50, 32), P(32, -50)],
    path: [[P(-50, 20), P(-50, -20), P(-48, -48), P(-20, -50), P(20, -50), P(40, -45)],
           [P(20, -50), P(-20, -50), P(-48, -48), P(-50, -20), P(-50, 20), P(-45, 40)]] },
  mid: { ru: 'мид', spawn: [P(-38, 26), P(38, -26)],
    path: [[P(-24, 22), P(-7, 7), P(7, -7), P(22, -22), P(40, -40)],
           [P(24, -22), P(7, -7), P(-7, 7), P(-22, 22), P(-40, 40)]] },
  bot: { ru: 'низ', spawn: [P(-32, 50), P(50, -32)],
    path: [[P(-20, 50), P(20, 50), P(48, 48), P(50, 20), P(50, -20), P(45, -40)],
           [P(50, -20), P(50, 20), P(48, 48), P(20, 50), P(-20, 50), P(-40, 45)]] },
};
const LANE_IDS = ['top', 'mid', 'bot'];
const SIDE_RECTS = [[-54.2, -54.2, -45.8, 46], [-54.2, -54.2, 46, -45.8], [45.8, -46, 54.2, 54.2], [-46, 45.8, 54.2, 54.2]];
// Башни: [линия, ярус, x, y] для Курчатова; у Москвы — зеркально (-x, -y), линии верх<->низ
const TOWER_K = [['top', 1, -50, -5], ['top', 2, -50, 22], ['mid', 1, -15, 15], ['mid', 2, -30, 30], ['bot', 1, 5, 50], ['bot', 2, -22, 50]];
// Лесные лагеря гопников: side — чья половина леса
const CAMPS = [{ x: -36, y: -12, side: K }, { x: -30, y: -38, side: M }, { x: 12, y: 36, side: K }, { x: 38, y: 30, side: M },
               { x: -12, y: -36, side: M }, { x: 36, y: 12, side: M }, { x: -38, y: -30, side: K }, { x: 30, y: 38, side: K }];
CAMPS.forEach((c, i) => { c.big = i < 4; c.id = i; });
const onSideLane = (x, y, m) => (Math.abs(x + 50) < m && y < 52) || (Math.abs(y + 50) < m && x < 52) || (Math.abs(x - 50) < m && y > -52) || (Math.abs(y - 50) < m && x > -52);

function St() { return { stun: 0, immune: 0, slows: [], dots: [], hots: [], asb: [], msb: [] }; }
function unit(o) {
  return Object.assign({ alive: true, cd: 0, target: null, st: St(), invuln: false, mr: 0.25, armor: 0, regen: 0, scan: 0,
    xpB: 0, goldB: 0, as: 100, bat: 1, r: 0.5, mp: 0, maxMp: 0, mpr: 0 }, o);
}

function buildWorld() {
  ents = []; projs = []; parts = []; texts = []; timers = []; bubbles = [];
  T = 0; nextWave = 5; waveNo = 0; over = false; teamKills = [0, 0]; fx = []; sausLines = []; glyph = [{ end: 0, ready: 0 }, { end: 0, ready: 0 }];
  // Троны
  const tk = unit({ kind: 'throne', team: K, x: -45, y: 45, r: 3.2, hp: 1500, maxHp: 1500, armor: 5, name: 'Трон Курчатова' });
  const tm = unit({ kind: 'throne', team: M, x: 45, y: -45, r: 3.2, hp: 1500, maxHp: 1500, armor: 5, name: 'Трон Москвы' });
  const tower = (team, lane, tier, x, y) => unit({ kind: 'tower', team, lane, tier, x, y, r: tier === 2 ? 2.5 : 2.2,
    hp: tier === 2 ? 1300 : 700, maxHp: tier === 2 ? 1300 : 700, armor: tier === 2 ? 9 : 5, dmg: tier === 2 ? 65 : 40, range: tier === 2 ? 10 : 9, bat: tier === 2 ? 1.3 : 1.5, regen: tier === 2 ? 2 : 0,
    name: (team === K ? 'Чикен Пицца' : 'Бургер Кинг') + ` (${LANE_DEF[lane].ru} ${tier})`, xpB: tier === 2 ? 160 : 100, goldB: tier === 2 ? 250 : 150 });
  ents.push(tk, tm);
  tk.prot = []; tm.prot = [];
  for (const team of [K, M]) {
    const mine = {};
    for (const [lane, tier, x, y] of TOWER_K) {
      const ln = team === K ? lane : lane === 'top' ? 'bot' : lane === 'bot' ? 'top' : 'mid';
      const t = tower(team, ln, tier, team === K ? x : -x, team === K ? y : -y);
      mine[ln + tier] = t; ents.push(t);
    }
    for (const ln of LANE_IDS) { mine[ln + 2].prot = [mine[ln + 1]]; (team === K ? tk : tm).prot.push(mine[ln + 2]); }
  }
  // трон уязвим, когда сломана хотя бы одна вторая башня
  tk.protAny = tm.protAny = true;
  CAMPS.forEach(c => { c.next = 30; });

  // Декор
  decor = [];
  let seed = 2026; const sr = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
  const keep = [[-45, 45], [45, -45], [-36, 36], [36, -36], [-30, 42], [42, -30], [-38, 26], [38, -26], ...TOWER_K.flatMap(t => [[t[2], t[3]], [-t[2], -t[3]]]), ...CAMPS.map(c => [c.x, c.y])];
  const free = (x, y) => Math.abs(x + y) / 1.414 > 9.5 && !onSideLane(x, y, 6.5) && keep.every(k => Math.hypot(x - k[0], y - k[1]) > 8.5);
  for (let t = -57; t <= 57; t += 3.4) [[t, -57], [t, 57], [-57, t], [57, t]].forEach(([x, y]) => {
    x += (sr() - .5) * 2; y += (sr() - .5) * 2; if (free(x, y)) decor.push({ x, y, kind: sr() < .5 ? 'pine' : 'tree', s: .8 + sr() * .6 });
  });
  for (let i = 0; i < 230; i++) { const x = (sr() - .5) * 106, y = (sr() - .5) * 106; if (free(x, y)) decor.push({ x, y, kind: sr() < .5 ? 'pine' : 'tree', s: .8 + sr() * .5 }); }
  for (let i = 0; i < 40; i++) { const x = (sr() - .5) * 110, y = (sr() - .5) * 110; if (free(x, y)) decor.push({ x, y, kind: 'rock', s: .7 + sr() * 1.2 }); }

  // Герой
  playerTeam = Math.random() < .5 ? K : M;
  const b = BATHS[playerTeam];
  hero = unit({ kind: 'hero', team: playerTeam, x: b.x, y: b.y, r: 0.9, name: HERO.name, xpB: 150, goldB: 200,
    lvl: 1, xp: 0, points: 1, gold: 600, abil: ABIL.map(() => ({ lvl: 0, cd: 0 })), inv: new Array(9).fill(null),
    order: null, respawn: 0, items: {} });
  setHeroDef(hero, HERO);
  ents.push(hero);
  // Боты: 4 союзника и 5 врагов
  bots = [];
  const nicks = [shuffle(NICKS[0]), shuffle(NICKS[1])];
  const mk = (team, i) => {
    const bb = BATHS[team], a = i / 5 * TAU;
    const b = unit({ kind: 'hero', bot: true, nick: nicks[team][i], team, x: bb.x + Math.cos(a) * 2, y: bb.y + Math.sin(a) * 2, r: 0.9, name: 'бот', xpB: 150, goldB: 200,
      lvl: 1, xp: 0, points: 1, gold: 600, abil: ABIL.map(() => ({ lvl: 0, cd: 0 })), inv: new Array(9).fill(null),
      order: null, respawn: 0, items: {}, mode: 'lane', built: new Set(), think: rnd(0, .3), lane: 'mid' });
    setHeroDef(b, pickRandom(HEROES)); bots.push(b); ents.push(b); return b;
  };
  for (let i = 0; i < 4; i++) mk(playerTeam, i);
  for (let i = 0; i < 5; i++) mk(1 - playerTeam, i);
  bot = bots.find(b => b.team !== playerTeam);
  // линии врагов — сразу, союзников — после выбора игрока
  shuffle(LANE_POOL).forEach((l, i) => { bots.filter(b => b.team !== playerTeam)[i].lane = l; });
  assignAllyLanes('mid');
  heroes = [hero, ...bots];
  for (const h of heroes) initHeroStats(h);
  for (const h of heroes) makeCourier(h);
  courier = hero.courier;
  cam.x = hero.x; cam.y = hero.y; cam.follow = true;
}

// ---------- Характеристики героя ----------
function itemStats(h) {
  const s = { str: 0, agi: 0, int: 0, dmg: 0, as: 0, armor: 0, hp: 0, mp: 0, hpr: 0, mpr: 0, ms: 0, mr: 0, crit: 0, critM: 1, cleave: 0, block: 0, blockA: 0, burn: 0 };
  for (let i = 0; i < 6; i++) {
    const it = h.inv[i]; if (!it) continue; const d = it.d.s;
    for (const k of ['str', 'agi', 'int', 'dmg', 'as', 'armor', 'hp', 'mp', 'hpr', 'mpr', 'ms']) s[k] += d[k] || 0;
    if (d.mr) s.mr = 1 - (1 - s.mr) * (1 - d.mr);
    for (const k of ['crit', 'critM', 'cleave', 'block', 'blockA', 'burn']) if (d[k]) s[k] = Math.max(s[k], d[k]);
  }
  return s;
}
function recalc(h, keep = true) {
  const HD = h.def || HERO;
  const I = itemStats(h), l = h.lvl;
  h.str = HD.baseStr + HD.strGain * (l - 1) + I.str;
  h.agi = HD.baseAgi + HD.agiGain * (l - 1) + I.agi;
  h.int = HD.baseInt + HD.intGain * (l - 1) + I.int;
  const hr = keep && h.maxHp ? h.hp / h.maxHp : 1, mr = keep && h.maxMp ? h.mp / h.maxMp : 1;
  h.maxHp = 120 + 22 * h.str + I.hp; h.hp = h.maxHp * hr;
  h.maxMp = 75 + 12 * h.int + I.mp; h.mp = h.maxMp * mr;
  h.regen = 1 + 0.1 * h.str + I.hpr; h.mpr = 0.5 + 0.05 * h.int + I.mpr;
  h.armor = HD.baseArmor + h.agi / 6 + I.armor; h.mr = 1 - 0.75 * (1 - I.mr);
  h.dmg = HD.baseDmg + (HD.attr === 'agi' ? h.agi : HD.attr === 'int' ? h.int : h.str) + I.dmg; h.as = 100 + h.agi + I.as; h.range = HD.range; h.bat = HD.bat;
  if (HD.rangeAbil != null && h.abil) { const ra = HD.abil[HD.rangeAbil], rs = h.abil[HD.rangeAbil];
    if (rs && rs.lvl) h.range += at(ra.v[0], rs.lvl); if (h.ladder > T) h.range += ra.v[1][0]; }
  h.dmg += (h.sausage || 0) * (HD.perSausage || 0); h.ranged = !!HD.ranged; h.look = HD.look;
  h.baseSpeed = HD.speed + I.ms; h.crit = I.crit; h.critM = I.critM; h.cleave = I.cleave; h.block = I.block; h.blockA = I.blockA; h.burn = I.burn;
}

// ---------- Крипы ----------
function spawnWave() {
  waveNo++;
  const power = 1 + 0.03 * (waveNo - 1);
  for (const team of [K, M]) for (const lane of LANE_IDS) {
    const L = LANE_DEF[lane], sp = L.spawn[team], path = L.path[team], f = path[0];
    const dx = f.x - sp.x, dy = f.y - sp.y, dl = Math.hypot(dx, dy) || 1, nx = dx / dl, ny = dy / dl;
    for (let i = 0; i < 4; i++) {
      const ranged = i === 3, off = (i - 1.5) * 1.2;
      ents.push(unit({ kind: 'creep', team, lane, ranged, x: sp.x - ny * off - (ranged ? nx * 1.5 : 0), y: sp.y + nx * off - (ranged ? ny * 1.5 : 0),
        r: 0.45, hp: (ranged ? 220 : 300) * power, maxHp: (ranged ? 220 : 300) * power, armor: ranged ? 0 : 2, regen: 0.5,
        dmg: (ranged ? 24 : 18) * power, range: ranged ? 6 : 1.2, bat: 1, speed: 4.2, path, wp: 0,
        name: ranged ? `Пацан с рогаткой из ${team === K ? 'Курчатова' : 'Москвы'}` : `Пацан из ${team === K ? 'Курчатова' : 'Москвы'}`,
        xpB: ranged ? 60 : 50, goldB: ranged ? 48 : 38 }));
    }
  }
}
function spawnCamp(c) {
  const n = c.big ? 3 : 2;
  for (let i = 0; i < n; i++) {
    const boss = c.big && i === 0, a = i / n * TAU;
    ents.push(unit({ kind: 'creep', neutral: true, team: NEUTRAL, camp: c.id, home: { x: c.x + Math.cos(a) * 1.4, y: c.y + Math.sin(a) * 1.4 },
      x: c.x + Math.cos(a) * 1.4, y: c.y + Math.sin(a) * 1.4, r: boss ? 0.7 : 0.5, boss,
      hp: boss ? 700 : 380, maxHp: boss ? 700 : 380, armor: boss ? 3 : 1, regen: 1, dmg: boss ? 34 : 20, range: 1.3, bat: boss ? 1.4 : 1.1, speed: 4,
      name: boss ? 'Главный гопник' : 'Гопник с района', xpB: boss ? 90 : 45, goldB: boss ? 75 : 40 }));
  }
}

// ===================== ТУМАН ВОЙНЫ =====================
// Каждая команда видит только то, что рядом с её героями, крипами, башнями и троном.
const VISION = { hero: 12, creep: 8, tower: 11, throne: 14 };
let vis = [new Set(), new Set()], visT = -1;
function visionSources(team) {
  const out = [];
  for (const e of ents) if (e.alive && e.team === team && VISION[e.kind]) out.push({ x: e.x, y: e.y, r: VISION[e.kind] });
  for (const f of fx) if (f.kind === 'rain' && f.src.team === team && T < f.end) out.push({ x: f.x, y: f.y, r: f.r + 1 });
  return out;
}
function updateVision() {
  if (visT === T) return; visT = T;
  for (const team of [K, M]) {
    const src = visionSources(team), seen = new Set();
    for (const e of ents) {
      if (!e.alive || e.team === team) continue;
      for (const s of src) { const dx = e.x - s.x, dy = e.y - s.y; if (dx * dx + dy * dy <= s.r * s.r) { seen.add(e); break; } }
    }
    vis[team] = seen;
  }
  for (const h of heroes) if (h.alive && h.channel && h.channel.kind === 'aim' && h.channel.t.alive) vis[h.team].add(h.channel.t);
}
// Видит ли команда этот юнит. Строения видны всегда (как в Доте — их видно сквозь туман).
const canSee = (team, e) => !e || e.team === team || e.kind === 'tower' || e.kind === 'throne' || team === NEUTRAL || vis[team].has(e);

// ===================== БОЙ =====================
const armorMul = a => 1 - (0.06 * a) / (1 + 0.06 * Math.abs(a));
const enemies = (a, b) => a.team !== b.team;
const edge = (a, b) => dist(a, b) - b.r;

function damage(src, tgt, amt, type, crit) {
  if (!tgt.alive || tgt.invuln) return;
  if (glyphOn(tgt)) { if (src === hero && Math.random() < .3) floatText(tgt.x, tgt.y - 2.5, 'глиф', '#ffd84a', 12); return; }
  if (type === 'mag' && T < tgt.st.immune) return;
  let d = amt;
  if (type === 'phys') { if (tgt.block && Math.random() < tgt.block) d = Math.max(0, d - tgt.blockA); d *= armorMul(tgt.armor); }
  else if (type === 'mag') d *= 1 - tgt.mr;
  tgt.hp -= d;
  if (src && src.kind === 'hero' && tgt.kind === 'hero' && enemies(src, tgt)) (tgt.hitBy ||= new Map()).set(src, T);
  if (d > 0 && src && enemies(src, tgt) && (src.kind === 'hero' || src.kind === 'tower') && tgt.st.hots.some(h => h.hp > 0)) {
    tgt.st.hots = tgt.st.hots.filter(h => !(h.hp > 0));
    if (tgt.kind === 'hero') floatText(tgt.x, tgt.y - 3.4, 'Шаурму выбили!', '#ff9a80', 13);
  }
  if (tgt.neutral && src && src.alive && src.team !== NEUTRAL) for (const e of ents) if (e.alive && e.camp === tgt.camp && !e.target) e.target = src;
  if ((src === hero || tgt === hero) && d >= 1) {
    const col = tgt === hero ? '#ff6b6b' : crit ? '#ff3a2a' : type === 'mag' ? '#c88cff' : type === 'pure' ? '#ffd98a' : '#ffffff';
    floatText(tgt.x + rnd(-.4, .4), tgt.y - (tgt.kind === 'hero' ? 3 : 2), (tgt === hero ? '-' : '') + Math.round(d) + (crit ? '!' : ''), col, crit ? 22 : d < 15 ? 12 : 15);
  }
  if (tgt.hp <= 0) kill(tgt, src);
}

function kill(u, src) {
  u.alive = false; u.hp = 0; u.died = T;
  burst(u.x, u.y - 1, TEAM_COL[u.team], u.kind === 'creep' ? 12 : 26);
  const denied = !!(src && src !== u && src.team === u.team && u.kind === 'creep');
  if (src && src.kind === 'hero' && u.kind === 'creep') {
    if (denied) { src.dn = (src.dn || 0) + 1; floatText(u.x, u.y - 2.4, '!', '#ff5a4a', 30); }
    else src.lh = (src.lh || 0) + 1;
  }
  if (u.courier) courierDied(u, src);
  for (const h of heroes) {
    if (h === u || !enemies(u, h)) continue;
    if (src === h || (h.alive && dist(u, h) <= 12)) addXp(u.xpB * (denied ? .5 : 1), h);
    if (src === h && u.goldB) { h.gold += u.goldB; if (h === hero) floatText(u.x, u.y - 2.4, '+' + u.goldB, '#ffc83a', 15); }
  }
  if (src && src.kind === 'hero' && src.alive !== undefined && (u.kind === 'creep' || u.kind === 'hero')) addSausage(src, u.kind === 'hero' ? 3 : 1);
  if (src && src.kind === 'hero' && u.kind === 'hero') setTimeout(() => speak(src, 1, false), 2200);
  if (u.kind === 'hero') {
    teamKills[1 - u.team]++;
    u.deaths = (u.deaths || 0) + 1;
    if (src && src.kind === 'hero' && enemies(src, u)) src.kills = (src.kills || 0) + 1;
    if (u.hitBy) { for (const [h, t] of u.hitBy) if (h !== src && T - t < 10 && enemies(h, u)) h.assists = (h.assists || 0) + 1; u.hitBy.clear(); }
    if (u.tpN <= 0 && u.gold >= ITEM.ticket.cost) { u.gold -= ITEM.ticket.cost; u.tpN = 1; }   // билет докупается сам
    u.tp = null;
    if (u.sausage) addSausage(u, -Math.ceil(u.sausage / 2));
    speakDeath(u);
    u.channel = null;
    u.respawn = T + 5 + u.lvl * 0.6; u.order = null; u.target = null; u.mode = 'lane';
    if (u === hero) toast(src && src.kind === 'hero' ? 'Тебя убил ' + src.name + '!' : 'Ты погиб!', '#ff8a80');
    else if (src === hero) toast('Ты убил ' + u.name + '! +' + u.goldB, '#ffc83a');
    else if (u.team === playerTeam) toast('Погиб союзник: ' + u.name, '#ff8a80');
    else toast((src && src.kind === 'hero' ? src.name + ' убил ' : 'Погиб враг: ') + u.name, '#ffc83a');
  }
  if (u.kind === 'throne') endGame(u.team === playerTeam ? 1 - playerTeam : playerTeam);
}

function addXp(x, h = hero) {
  if (h.lvl >= 18) return;
  h.xp += x;
  while (h.lvl < 18 && h.xp >= xpNeed(h)) {
    h.xp -= xpNeed(h); h.lvl++; h.points++; recalc(h, true);
    floatText(h.x, h.y - 4, 'УРОВЕНЬ ' + h.lvl + '!', '#ffd84a', 20);
    burst(h.x, h.y - 1.5, '#ffd84a', 20);
    if (h.bot) botLearn(h);
  }
}
const xpNeed = (h = hero) => 80 + 40 * (h.lvl - 1);

// Статусы
const stunned = u => T < u.st.stun;
function stun(u, d) { if (T < u.st.immune) return; u.st.stun = Math.max(u.st.stun, T + d); }
function slow(u, p, d) { if (T < u.st.immune) return; u.st.slows.push({ v: p, e: T + d }); }
function dot(u, dps, d, src) { if (T < u.st.immune) return; u.st.dots.push({ v: dps, e: T + d, src, t: 0 }); }
function speedOf(u) {
  const base = u.kind === 'hero' ? u.baseSpeed : u.speed;
  const sl = u.st.slows.reduce((m, s) => Math.max(m, s.v), 0), bo = u.st.msb.reduce((a, s) => a + s.v, 0);
  return base * Math.max(0.1, 1 - sl / 100 + bo / 100);
}
const atkInterval = u => u.bat / ((u.as + u.st.asb.reduce((a, s) => a + s.v, 0)) / 100);

function tickStatus(u, dt) {
  const s = u.st;
  for (const k of ['slows', 'asb', 'msb', 'hots']) s[k] = s[k].filter(x => x.e > T);
  for (const d of s.dots) { d.t += dt; while (d.t >= .5) { d.t -= .5; damage(d.src, u, d.v * .5, 'mag'); } }
  s.dots = s.dots.filter(x => x.e > T);
  for (const h of s.hots) { u.hp = Math.min(u.maxHp, u.hp + h.hp * dt); if (u.maxMp) u.mp = Math.min(u.maxMp, u.mp + h.mp * dt); }
}

// Атака
function attack(u, t) {
  if (u.channel) return;
  u.cd = atkInterval(u); u.face = Math.atan2(t.y - u.y, t.x - u.x);
  let d = u.dmg; const crit = u.crit && Math.random() < u.crit; if (crit) d *= u.critM;
  if (u.kind === 'tower') { shoot(u, t, d, 'phys', u.team === K ? 'pizzaT' : 'burger', 16); return; }
  if (u.ranged) { const hs = u.def === MAX ? maxHeadshot(u) : 0;
    shoot(u, t, d + hs, 'phys', u.look || 'stone', u.kind === 'hero' ? (u.def && u.def.bulletSpeed) || 22 : 18, hs ? x => headshotHit(u, x) : null); return; }
  damage(u, t, d, 'phys', crit);
  burst(t.x, t.y - 1, crit ? '#ff4030' : '#fff2d0', crit ? 10 : 4);
  if (u.cleave) for (const e of ents) if (e !== t && e.alive && enemies(u, e) && e.kind !== 'tower' && e.kind !== 'throne' && dist(e, t) < 3.5) damage(u, e, d * u.cleave, 'pure');
}
function shoot(src, tgt, dmg, type, look, speed, onHit) {
  projs.push({ x: src.x, y: src.y - (src.kind === 'tower' ? 3.5 : 1.5), tgt, dmg, type, look, speed, onHit, src, spin: 0 });
}

function findEnemy(u, radius, prefer) {
  let best = null, bd = radius;
  for (const e of ents) {
    if (!e.alive || !enemies(u, e) || e.invuln || e.neutral || !canSee(u.team, e)) continue;
    if (prefer === 'units' && (e.kind === 'tower' || e.kind === 'throne')) continue;
    const d = edge(u, e); if (d < bd) { bd = d; best = e; }
  }
  return best;
}

// ===================== СПОСОБНОСТИ =====================
let targeting = -1, pending = null;
function multicast(h = hero) {
  if (h.def !== HERO) return 1;
  const r = h.abil[3]; if (!r.lvl) return 1;
  const v = ABIL[3].v, p = Math.random() * 100;
  const x4 = at(v[2], r.lvl), x3 = at(v[1], r.lvl), x2 = at(v[0], r.lvl);
  return p < x4 ? 4 : p < x4 + x3 ? 3 : p < x4 + x3 + x2 ? 2 : 1;
}
function validTarget(i, t, h = hero) {
  if (AB(h)[i].target === 'point') return !!t;
  if (!t || !t.alive || t.kind === 'tower' || t.kind === 'throne') return false;
  return AB(h)[i].target === 'enemy' ? enemies(h, t) : !enemies(h, t);
}
function pressAbility(i) {
  showAbilTip(i);
  if (!hero.alive || paused || over) return;
  const a = AB(hero)[i], s = hero.abil[i];
  if (!s.lvl) return say('Способность не изучена');
  if (hero.channel) return;
  if (a.target === 'passive') return say('Это пассивка');
  if (stunned(hero)) return say('Оглушён!');
  if (s.cd > 0) return say('Перезарядка ' + Math.ceil(s.cd));
  if (hero.mp < at(a.mana, s.lvl)) return say('Не хватает маны');
  if (a.target === 'none') { targeting = -1; return doCast(i, hero); }
  if (a.target === 'ally' && targeting === i) { targeting = -1; return beginCast(i, hero); }
  targeting = i;
}
function beginCast(i, t) {
  if (edge(hero, t) <= AB(hero)[i].range) doCast(i, t);
  else { pending = { i, t }; hero.order = null; hero.target = null; }
}
function doCast(i, t, h = hero) {
  const a = AB(h)[i], s = h.abil[i], lv = s.lvl;
  if (!lv || s.cd > 0 || h.mp < at(a.mana, lv) || h.channel) return;
  if (a.charges && !(s.ch > 0)) return;
  h.mp -= at(a.mana, lv); s.cd = at(a.cd, lv);
  if (h.def === SANYA) return sanyaCast(i, h, a, lv);
  if (h.def === MAX) return maxCast(i, h, a, lv, t);
  const n = multicast(h);
  if (n > 1) floatText(h.x, h.y - 4.2, 'x' + n + '!', '#ffc83a', 18 + n * 5);
  if (i === 0) {
    for (let k = 0; k < n; k++) later(k * .4, () => { if (t.alive) shoot(h, t, at(a.v[0], lv), 'mag', 'pizza', 26, x => stun(x, at(a.v[1], lv))); });
  } else if (i === 1) {
    const list = [t, ...nearby(t, 7, n - 1, e => validTarget(1, e, h) && e !== t)];
    for (const e of list) shoot(h, e, 0, 'mag', 'sauce', 18, x => { dot(x, at(a.v[0], lv), at(a.v[1], lv), h); slow(x, at(a.v[2], lv), at(a.v[1], lv)); });
  } else if (i === 2) {
    const list = [t, ...nearby(h, 7, n - 1, e => validTarget(2, e, h) && e !== t)];
    for (const e of list) { e.st.asb.push({ v: at(a.v[0], lv), e: T + at(a.v[2], lv) }); e.st.msb.push({ v: at(a.v[1], lv), e: T + at(a.v[2], lv) });
      floatText(e.x, e.y - 3, 'Энергетик!', '#66ff66', 13); burst(e.x, e.y - 1, '#3fc54a', 10); }
  }
}
// ---------- Голос героев ----------
const mkVoice = arr => typeof arr !== 'undefined' && arr ? arr.map(s => { const a = new Audio(s); a.preload = 'auto'; return a; }) : [];
const VOICE = { sanya_fiend: typeof SANYA_VOICE !== 'undefined' ? mkVoice(SANYA_VOICE) : [], max_gold: typeof MAX_VOICE !== 'undefined' ? mkVoice(MAX_VOICE) : [] };
let voiceBusyUntil = 0, lastVoice = -1;
function playVoice(h, i, d) {
  const lines = VOICE[h.def.id]; lines.forEach(a => { a.pause(); a.currentTime = 0; });
  const a = lines[i]; a.volume = clamp(1 - (d || 0) / 24, .25, 1); a.play().catch(() => {});
  voiceBusyUntil = performance.now() + (a.duration ? a.duration * 1000 + 300 : 2600);
}
// фраза при смерти героя
function speakDeath(h) {
  if (!h.def || !h.def.deathText) return;
  floatText(h.x, h.y - 4.4, h.def.deathText, '#ffb48a', 15);
  const lines = VOICE[h.def.id], i = h.def.deathVoice, d = Math.hypot(h.x - cam.x, h.y - cam.y);
  if (SETTINGS.sound === '0' || i == null || !lines || !lines[i] || d > 22) return;
  playVoice(h, i, d);
}
function speak(h, chance = 1, force = false) {
  if (SETTINGS.sound === '0' || !h || !h.def) return;
  const lines = VOICE[h.def.id]; if (!lines || !lines.length) return;
  const now = performance.now();
  if (!force && (now < voiceBusyUntil || Math.random() > chance)) return;
  const d = h.x === undefined || !hero ? 0 : Math.hypot(h.x - cam.x, h.y - cam.y);
  if (d > 22) return;
  const pool = lines.map((_, k) => k).filter(k => k !== h.def.deathVoice && (k !== lastVoice || lines.length < 3));
  const i = pool[Math.floor(Math.random() * pool.length)]; lastVoice = i;
  playVoice(h, i, d);
}
// ---------- Саня Финд ----------
let fx = [], sausLines = [];
const isUnitTarget = e => e.kind === 'creep' || e.kind === 'hero';
function sanyaCast(i, h, a, lv) {
  const face = h.face ?? (h.team === K ? -Math.PI / 4 : Math.PI * 3 / 4);
  if (i < 3) {
    const cx = h.x + Math.cos(face) * a.dist, cy = h.y + Math.sin(face) * a.dist, dmg = at(a.v[0], lv) + (h.sausage || 0) * a.v[1][0];
    fx.push({ kind: 'coil', x: cx, y: cy, r: a.rad, t0: T, t1: T + .55, drops: Array.from({ length: 9 }, () => { const an = rnd(0, TAU), rr = Math.sqrt(Math.random()) * a.rad * .85; return { x: Math.cos(an) * rr, y: Math.sin(an) * rr, d: rnd(.8, 1) }; }) });
    later(.55, () => {
      for (const e of ents) if (e.alive && enemies(h, e) && isUnitTarget(e) && Math.hypot(e.x - cx, e.y - cy) <= a.rad + e.r * .5) damage(h, e, dmg, 'mag');
      burst(cx, cy, '#c8322b', 14);
    });
    speak(h, .35);
  } else {
    h.channel = { end: T + 1.67, lv, a }; h.order = null; h.target = null;
    floatText(h.x, h.y - 4.4, 'КОЛБАСНЫЙ РЕКВИЕМ...', '#ff7a1a', 16);
    speak(h, 1, true);
  }
}
function sanyaRelease(h) {
  const c = h.channel; h.channel = null;
  const n = clamp(Math.max(h.sausage || 0, c.a.v[1][0]), 1, 30), dmg = at(c.a.v[0], c.lv), base = h.face || 0;
  for (let k = 0; k < n; k++) { const an = base + TAU * k / n;
    sausLines.push({ x: h.x, y: h.y, vx: Math.cos(an) * 11, vy: Math.sin(an) * 11, left: c.a.range, hit: new Set(), dmg, src: h, spin: rnd(0, TAU) }); }
  burst(h.x, h.y - 1.5, '#ff7a1a', 30);
}
function tickSanya(dt) {
  for (const h of heroes) if (h.channel) {
    if (!h.alive || stunned(h)) { h.channel = null; continue; }
    if (h.channel.kind === 'aim') { const t = h.channel.t; if (!t.alive) { h.channel = null; continue; } h.face = Math.atan2(t.y - h.y, t.x - h.x); if (T >= h.channel.end) maxFire(h); continue; }
    if (T >= h.channel.end) sanyaRelease(h);
  }
  for (const l of sausLines) {
    const st = 11 * dt; l.x += l.vx * dt; l.y += l.vy * dt; l.left -= st; l.spin += dt * 15;
    for (const e of ents) if (e.alive && enemies(l.src, e) && isUnitTarget(e) && !l.hit.has(e) && Math.hypot(e.x - l.x, e.y - l.y) <= 1 + e.r) { l.hit.add(e); damage(l.src, e, l.dmg, 'mag'); }
  }
  sausLines = sausLines.filter(l => l.left > 0);
  tickMax(dt);
  fx = fx.filter(f => T < (f.end || f.t1) + .5);
}
// ---------- Макс Gold ----------
const abilIcon = a => a && a.icon && typeof MAX_ICONS !== 'undefined' ? MAX_ICONS[a.icon] : null;
function maxCast(i, h, a, lv, t) {
  const s = h.abil[i];
  if (i === 0) {   // Золотой дождь
    s.ch--; if (s.chT <= 0) s.chT = a.chargeCd; s.cd = s.ch > 0 ? .4 : s.chT;
    fx.push({ kind: 'rain', x: t.x, y: t.y, r: a.rad, t0: T, t1: T + 1.2, end: T + 1.2 + a.v[2][0], src: h, dps: at(a.v[0], lv), sl: at(a.v[1], lv), tick: 0, seed: Math.random() * 1000 });
    h.face = Math.atan2(t.y - h.y, t.x - h.x);
    speak(h, 1);
  } else if (i === 2) {   // Лестница Габилджона
    h.ladder = T + a.v[2][0]; h.st.msb.push({ v: -30, e: h.ladder }); recalc(h, true);
    floatText(h.x, h.y - 5.4, 'НА ЛЕСТНИЦУ!', '#ffd24a', 15); burst(h.x, h.y - 1, '#ffd24a', 16); speak(h, 1);
  } else if (i === 3) {   // Хантерас пик
    h.channel = { kind: 'aim', end: T + a.v[1][0], lv, a, t }; h.order = null; h.target = null;
    h.face = Math.atan2(t.y - h.y, t.x - h.x);
    floatText(h.x, h.y - 4.8, 'ХАНТЕРАС ПИК...', '#ff5a3a', 16);
    if (t === hero) toast('Макс Gold целится в тебя!', '#ff8a80');
    speak(h, 1, true);
  }
}
function maxFire(h) {
  const c = h.channel; h.channel = null;
  const t = c.t, dmg = at(c.a.v[0], c.lv);
  shoot(h, t, dmg, 'mag', 'goldbig', 70, x => { stun(x, .3); floatText(x.x, x.y - 4.6, 'ХЕДШОТ!', '#ffd24a', 24); burst(x.x, x.y - 2.4, '#fff0a0', 26); });
  const dir = Math.cos(h.face || 0) >= 0 ? 1 : -1;
  burst(h.x + dir * 3.3, h.y - 1.2, '#fff0a0', 18);
}
function maxHeadshot(u) {
  const s = u.abil[1]; if (!s || !s.lvl) return 0;
  return u.ladder > T || Math.random() < .4 ? at(MAX.abil[1].v[1], s.lvl) : 0;
}
function headshotHit(u, x) {
  if (x.kind === 'tower' || x.kind === 'throne') return;
  slow(x, 100, .5);
  if (!glyphOn(x) && T >= x.st.immune) { const d = Math.hypot(x.x - u.x, x.y - u.y) || 1; x.x += (x.x - u.x) / d * .45; x.y += (x.y - u.y) / d * .45; }
  if (u === hero || x === hero) floatText(x.x, x.y - 3.6, 'Голдшот!', '#ffd24a', 14);
  if (x.kind === 'hero') speak(u, .35);
  burst(x.x, x.y - 2.2, '#ffd24a', 9);
}
function tickMax(dt) {
  for (const f of fx) if (f.kind === 'rain' && T >= f.t1 && T < f.end) {
    f.tick += dt;
    while (f.tick >= .5) { f.tick -= .5;
      for (const e of ents) if (e.alive && enemies(f.src, e) && isUnitTarget(e) && Math.hypot(e.x - f.x, e.y - f.y) <= f.r + e.r * .5) { damage(f.src, e, f.dps * .5, 'mag'); slow(e, f.sl, .6); } }
  }
}
// бот: добить любого видимого героя ультой
function maxSnipe(u) {
  const A = AB(u), r = u.abil[3];
  if (!r.lvl || r.cd > 0 || u.mp < at(A[3].mana, r.lvl)) return false;
  for (const e of heroes) if (e.alive && enemies(u, e) && canSee(u.team, e) && dist(u, e) <= A[3].range && T >= e.st.immune &&
    e.hp < at(A[3].v[0], r.lvl) * (1 - (e.mr || 0)) * .95) { doCast(3, e, u); return true; }
  return false;
}
function maxBotCast(u, t, isHero) {
  const A = AB(u), d = dist(u, t);
  if (maxSnipe(u)) return true;
  const r = u.abil[3];
  if (isHero && r.lvl && !r.cd && u.mp >= at(A[3].mana, r.lvl) && t.hp < t.maxHp * .45 && d > 6) { doCast(3, t, u); return true; }
  const q = u.abil[0];
  if (q.lvl && q.ch > 0 && !q.cd && u.mp >= at(A[0].mana, q.lvl) && d <= A[0].range && (isHero || q.ch >= 2)) { doCast(0, { x: t.x, y: t.y, r: 0, point: true }, u); return true; }
  const e = u.abil[2];
  if (isHero && e.lvl && !e.cd && u.mp >= at(A[2].mana, e.lvl) && d > u.range - 1 && d < u.range + 3) { doCast(2, u, u); return true; }
  return false;
}
// Модель: светлые волосы (лицо с фото), чёрная кожанка с золотом, золотые наплечники, цепь, золотые кроссовки и длинная золотая снайперка
function goldG(c, x0, y0, x1, y1) { const g = c.createLinearGradient(x0, y0, x1, y1);
  g.addColorStop(0, '#fff3c0'); g.addColorStop(.3, '#ffd24a'); g.addColorStop(.65, '#c88a14'); g.addColorStop(1, '#6e4606'); return g; }
let maxHeadImg = null;
function drawMax(c, x, y, p, e) {
  const dir = Math.cos((e && e.face) ?? 0) >= 0 ? 1 : -1, onLadder = e && e.ladder > T, aim = e && e.channel && e.channel.kind === 'aim';
  const oy = onLadder ? -1.1 * p : 0;
  // золотая стремянка
  if (onLadder) {
    c.lineCap = 'round';
    for (const s of [-1, 1]) { c.strokeStyle = '#6e4606'; c.lineWidth = .2 * p; c.beginPath(); c.moveTo(x + s * .95 * p, y); c.lineTo(x + s * .55 * p, y + oy - .05 * p); c.stroke();
      c.strokeStyle = goldG(c, x - p, 0, x + p, 0); c.lineWidth = .13 * p; c.beginPath(); c.moveTo(x + s * .95 * p, y); c.lineTo(x + s * .55 * p, y + oy - .05 * p); c.stroke(); }
    for (let k = 1; k <= 3; k++) { const yy = y + oy * k / 3.3, ww = .95 - .4 * k / 3.3; c.strokeStyle = '#ffd24a'; c.lineWidth = .1 * p; c.beginPath(); c.moveTo(x - ww * p, yy); c.lineTo(x + ww * p, yy); c.stroke(); }
    c.lineCap = 'butt';
    const g = c.createRadialGradient(x, y + oy - 2 * p, 0, x, y + oy - 2 * p, 3 * p); g.addColorStop(0, 'rgba(255,215,80,.35)'); g.addColorStop(1, 'rgba(255,215,80,0)');
    c.fillStyle = g; c.fillRect(x - 3 * p, y + oy - 5 * p, 6 * p, 6 * p);
  }
  c.save(); c.translate(x, y + oy); c.scale(dir, 1);
  // ноги: тёмные джинсы и золотые кроссовки
  c.fillStyle = '#1b2230'; c.fillRect(-.55 * p, -1.3 * p, .45 * p, 1.2 * p); c.fillRect(.1 * p, -1.3 * p, .45 * p, 1.2 * p);
  c.fillStyle = goldG(c, 0, -.3 * p, 0, 0); c.beginPath(); c.ellipse(-.3 * p, -.1 * p, .4 * p, .19 * p, 0, 0, TAU); c.ellipse(.42 * p, -.1 * p, .4 * p, .19 * p, 0, 0, TAU); c.fill();
  c.fillStyle = '#fff'; c.fillRect(-.6 * p, -.06 * p, .55 * p, .05 * p); c.fillRect(.12 * p, -.06 * p, .6 * p, .05 * p);
  // задняя рука (держит ствол)
  c.fillStyle = '#0c0c10'; c.save(); c.translate(-.75 * p, -2.75 * p); c.rotate(-1.25); c.fillRect(-.2 * p, 0, .4 * p, 1.45 * p); c.restore();
  // кожанка
  const jg = c.createLinearGradient(-.9 * p, 0, .9 * p, 0); jg.addColorStop(0, '#060608'); jg.addColorStop(.45, '#1d1d24'); jg.addColorStop(1, '#08080a');
  c.fillStyle = jg; c.beginPath(); c.roundRect ? c.roundRect(-.9 * p, -3.1 * p, 1.8 * p, 2 * p, .4 * p) : c.rect(-.9 * p, -3.1 * p, 1.8 * p, 2 * p); c.fill();
  c.fillStyle = 'rgba(255,255,255,.08)'; c.beginPath(); c.ellipse(-.35 * p, -2.4 * p, .18 * p, .6 * p, .2, 0, TAU); c.fill();
  c.strokeStyle = '#ffd24a'; c.lineWidth = Math.max(1, .06 * p);
  c.beginPath(); c.moveTo(.05 * p, -3.05 * p); c.lineTo(.12 * p, -1.15 * p); c.stroke();               // молния
  c.beginPath(); c.moveTo(-.9 * p, -1.2 * p); c.lineTo(.9 * p, -1.2 * p); c.stroke();                 // золотой кант
  // ремень с пряжкой
  c.fillStyle = '#2a1a08'; c.fillRect(-.88 * p, -1.42 * p, 1.76 * p, .2 * p);
  c.fillStyle = goldG(c, 0, -1.5 * p, 0, -1.2 * p); c.fillRect(-.12 * p, -1.47 * p, .32 * p, .3 * p);
  // воротник
  c.fillStyle = '#121216'; c.beginPath(); c.moveTo(-.55 * p, -3.1 * p); c.lineTo(-.05 * p, -2.55 * p); c.lineTo(.45 * p, -3.1 * p); c.closePath(); c.fill();
  // цепь и медальон
  c.strokeStyle = goldG(c, -.5 * p, 0, .5 * p, 0); c.lineWidth = Math.max(1.5, .08 * p); c.beginPath(); c.arc(0, -3.05 * p, .55 * p, .4, Math.PI - .4); c.stroke();
  c.fillStyle = goldG(c, 0, -2.75 * p, 0, -2.3 * p); c.beginPath(); c.arc(0, -2.5 * p, .2 * p, 0, TAU); c.fill();
  if (p > 16) { c.fillStyle = '#6e4606'; c.font = `900 ${.24 * p}px Rubik, system-ui, sans-serif`; c.textAlign = 'center'; c.textBaseline = 'middle'; c.fillText('G', 0, -2.49 * p); c.textBaseline = 'alphabetic'; }
  // золотые наплечники
  for (const s of [-1, 1]) { c.fillStyle = goldG(c, s * .9 * p, -3.3 * p, s * .5 * p, -2.6 * p); c.beginPath(); c.ellipse(s * .8 * p, -2.95 * p, .42 * p, .26 * p, s * .3, 0, TAU); c.fill();
    c.strokeStyle = '#7a5208'; c.lineWidth = Math.max(1, .04 * p); c.stroke();
    c.fillStyle = '#fff3c0'; for (const k of [-1, 0, 1]) { c.beginPath(); c.arc(s * (.8 + k * .2) * p, -2.95 * p + Math.abs(k) * .05 * p, .045 * p, 0, TAU); c.fill(); } }
  // снайперка
  const ry = -2.05 * p, kick = aim ? 0 : 0;
  c.save(); c.translate(0, ry); c.rotate(aim ? -.02 : -.06);
  // приклад
  c.fillStyle = goldG(c, 0, -.3 * p, 0, .35 * p);
  c.beginPath(); c.moveTo(-1.35 * p, -.2 * p); c.lineTo(-.25 * p, -.18 * p); c.lineTo(-.15 * p, .2 * p); c.lineTo(-1.25 * p, .38 * p); c.quadraticCurveTo(-1.45 * p, .1 * p, -1.35 * p, -.2 * p); c.fill();
  c.strokeStyle = 'rgba(110,70,6,.85)'; c.lineWidth = Math.max(1, .035 * p); c.beginPath(); c.moveTo(-1.3 * p, -.05 * p); c.quadraticCurveTo(-.8 * p, .15 * p, -.3 * p, -.02 * p); c.stroke();
  // корпус с гравировкой
  c.fillStyle = goldG(c, 0, -.22 * p, 0, .2 * p); c.fillRect(-.3 * p, -.22 * p, 1.75 * p, .42 * p);
  c.strokeStyle = 'rgba(110,70,6,.85)'; c.lineWidth = Math.max(1, .035 * p);
  for (let k = 0; k < 3; k++) { const bx = (-.05 + k * .5) * p; c.beginPath(); c.arc(bx, 0, .13 * p, Math.PI * .2, Math.PI * 1.6); c.stroke();
    c.beginPath(); c.moveTo(bx + .13 * p, 0); c.quadraticCurveTo(bx + .3 * p, -.15 * p, bx + .42 * p, .02 * p); c.stroke(); }
  c.fillStyle = '#d8241a'; c.beginPath(); c.arc(1.2 * p, -.02 * p, .07 * p, 0, TAU); c.fill(); c.fillStyle = '#ffb0a0'; c.beginPath(); c.arc(1.18 * p, -.04 * p, .025 * p, 0, TAU); c.fill();
  // магазин и спуск
  c.fillStyle = '#5a3a06'; c.fillRect(.35 * p, .2 * p, .3 * p, .38 * p);
  c.strokeStyle = '#5a3a06'; c.lineWidth = .05 * p; c.beginPath(); c.arc(-.02 * p, .26 * p, .12 * p, 0, Math.PI); c.stroke();
  // ствол и дульный тормоз
  c.fillStyle = goldG(c, 0, -.1 * p, 0, .1 * p); c.fillRect(1.45 * p, -.09 * p, 1.85 * p, .17 * p);
  c.fillStyle = '#3a2604'; c.fillRect(1.45 * p, .04 * p, 1.85 * p, .04 * p);
  c.fillStyle = goldG(c, 0, -.16 * p, 0, .16 * p); c.fillRect(3.25 * p, -.15 * p, .38 * p, .3 * p);
  c.fillStyle = '#2a1a04'; for (const k of [0, 1]) c.fillRect((3.32 + k * .14) * p, -.11 * p, .06 * p, .22 * p);
  // сошки
  c.strokeStyle = '#7a5208'; c.lineWidth = .05 * p; c.beginPath(); c.moveTo(1.9 * p, .08 * p); c.lineTo(1.4 * p, .3 * p); c.stroke();
  // оптика
  c.fillStyle = '#121216'; c.fillRect(-.05 * p, -.52 * p, 1.25 * p, .24 * p);
  c.fillStyle = '#121216'; c.beginPath(); c.ellipse(1.22 * p, -.4 * p, .1 * p, .17 * p, 0, 0, TAU); c.fill();
  c.fillStyle = goldG(c, 0, -.55 * p, 0, -.25 * p); for (const k of [.05, .55, 1.05]) c.fillRect(k * p, -.55 * p, .09 * p, .3 * p);
  c.fillStyle = goldG(c, 0, -.3 * p, 0, -.2 * p); c.fillRect(.25 * p, -.29 * p, .08 * p, .1 * p); c.fillRect(.85 * p, -.29 * p, .08 * p, .1 * p);
  const gl = aim ? .6 + .4 * Math.sin(T * 14) : .55;
  c.fillStyle = `rgba(140,220,255,${gl})`; c.beginPath(); c.ellipse(1.25 * p, -.4 * p, .05 * p, .12 * p, 0, 0, TAU); c.fill();
  c.restore();
  // передняя рука (на спуске)
  c.fillStyle = '#0c0c10'; c.save(); c.translate(.7 * p, -2.85 * p); c.rotate(-.35); c.fillRect(-.2 * p, 0, .4 * p, .95 * p); c.restore();
  ccirc(c, .1 * p, -1.85 * p, .2 * p, '#f2c8ad');
  ccirc(c, 1.95 * p, -2.12 * p, .19 * p, '#f2c8ad');
  c.fillStyle = '#ffd24a'; c.fillRect(.0 * p, -1.98 * p, .2 * p, .06 * p);   // перстень
  c.restore();
  // голова — лицо с фото (не отзеркаливаем)
  const hx = x + dir * .05 * p, hy = y + oy - 3.7 * p, r = .62 * p;
  if (!maxHeadImg && typeof MAX_HEAD !== 'undefined') { maxHeadImg = new Image(); maxHeadImg.src = MAX_HEAD; }
  c.fillStyle = '#f2c8ad'; c.fillRect(hx - .18 * p, hy + .4 * p, .36 * p, .35 * p);
  ccirc(c, hx, hy, r + .07 * p, '#c88a14');
  ccirc(c, hx, hy, r, '#e8c39a');
  if (maxHeadImg && maxHeadImg.complete && maxHeadImg.naturalWidth) { c.save(); c.beginPath(); c.arc(hx, hy, r, 0, TAU); c.clip(); c.drawImage(maxHeadImg, hx - r, hy - r, r * 2, r * 2); c.restore(); }
  c.strokeStyle = goldG(c, hx - r, hy - r, hx + r, hy + r); c.lineWidth = Math.max(1.5, .07 * p); c.beginPath(); c.arc(hx, hy, r, 0, TAU); c.stroke();
}
function drawGoldBullet(q, x, y, p) {
  const t = q.tgt, tx = sx(t.x), ty = sy(t.y) - (t.kind === 'tower' || t.kind === 'throne' ? 2 : 1.2) * p, a = Math.atan2(ty - y, tx - x), big = q.look === 'goldbig';
  ctx.save(); ctx.translate(x, y); ctx.rotate(a);
  const len = (big ? 2.6 : 1.2) * p, tr = ctx.createLinearGradient(-len * 2.2, 0, 0, 0);
  tr.addColorStop(0, 'rgba(255,220,120,0)'); tr.addColorStop(1, big ? 'rgba(255,240,170,.9)' : 'rgba(255,220,120,.6)');
  ctx.fillStyle = tr; ctx.fillRect(-len * 2.2, -(big ? .09 : .05) * p, len * 2.2, (big ? .18 : .1) * p);
  const w = (big ? .2 : .12) * p, l = (big ? .6 : .38) * p;
  ctx.fillStyle = goldG(ctx, 0, -w, 0, w);
  ctx.beginPath(); ctx.moveTo(-l / 2, -w / 2); ctx.lineTo(l * .1, -w / 2); ctx.quadraticCurveTo(l / 2, -w / 2, l / 2, 0); ctx.quadraticCurveTo(l / 2, w / 2, l * .1, w / 2); ctx.lineTo(-l / 2, w / 2); ctx.closePath(); ctx.fill();
  if (big) { ctx.shadowColor = '#ffe080'; ctx.shadowBlur = p * .6; ctx.fill(); }
  ctx.restore();
}
function drawRainGround(f, p) {
  const x = sx(f.x), y = sy(f.y), fade = T > f.end ? Math.max(0, 1 - (T - f.end) / .5) : 1;
  ctx.globalAlpha = fade;
  if (T < f.t1) {
    const k = (T - f.t0) / (f.t1 - f.t0);
    ctx.strokeStyle = 'rgba(255,210,74,.85)'; ctx.lineWidth = 2; ctx.setLineDash([6, 5]);
    ctx.beginPath(); ctx.ellipse(x, y, f.r * p, f.r * p * .9, 0, 0, TAU); ctx.stroke(); ctx.setLineDash([]);
    ctx.fillStyle = `rgba(255,200,60,${.08 + .15 * k})`; ctx.beginPath(); ctx.ellipse(x, y, f.r * p * k, f.r * p * .9 * k, 0, 0, TAU); ctx.fill();
  } else {
    const g = ctx.createRadialGradient(x, y, 0, x, y, f.r * p); g.addColorStop(0, 'rgba(255,215,80,.38)'); g.addColorStop(1, 'rgba(255,190,40,.12)');
    ctx.fillStyle = g; ctx.beginPath(); ctx.ellipse(x, y, f.r * p, f.r * p * .9, 0, 0, TAU); ctx.fill();
    ctx.strokeStyle = 'rgba(255,215,80,.7)'; ctx.lineWidth = 1.5; ctx.stroke();
    // падающие золотые пули и блики
    for (let k = 0; k < 22; k++) {
      const ph = ((T * 1.7 + k * .137 + f.seed) % 1), an = (k * 2.399 + f.seed) % TAU, rr = Math.sqrt(((k * .61803 + f.seed * .01) % 1)) * f.r * .95;
      const gx = x + Math.cos(an) * rr * p, gy = y + Math.sin(an) * rr * p * .9, h = (1 - ph) * 5 * p;
      ctx.strokeStyle = `rgba(255,225,120,${.25 + .5 * ph})`; ctx.lineWidth = Math.max(1, .09 * p);
      ctx.beginPath(); ctx.moveTo(gx + .3 * p, gy - h - .9 * p); ctx.lineTo(gx, gy - h); ctx.stroke();
      if (ph > .85) { ctx.fillStyle = 'rgba(255,240,170,.9)'; ctx.beginPath(); ctx.ellipse(gx, gy, .2 * p, .08 * p, 0, 0, TAU); ctx.fill(); }
    }
  }
  ctx.globalAlpha = 1;
}
function drawMaxOverlay(p) {
  for (const h of heroes) {
    if (!h.alive || !h.channel || h.channel.kind !== 'aim') continue;
    const t = h.channel.t; if (!t.alive) continue;
    const dir = Math.cos(h.face || 0) >= 0 ? 1 : -1, oy = h.ladder > T ? 1.1 : 0;
    const mx = sx(h.x) + dir * 3.4 * p, my = sy(h.y) - (2.15 + oy) * p, tx = sx(t.x), ty = sy(t.y) - 3.6 * p;
    const k = clamp(1 - (h.channel.end - T) / 2, 0, 1);
    if (h.team === playerTeam || canSee(playerTeam, h)) {
      ctx.strokeStyle = `rgba(255,40,30,${.35 + .4 * k})`; ctx.lineWidth = 1.5; ctx.beginPath(); ctx.moveTo(mx, my); ctx.lineTo(tx, ty); ctx.stroke(); }
    // прицел над головой цели
    const r = (1.6 - .9 * k) * p;
    ctx.strokeStyle = '#ff3a2a'; ctx.lineWidth = 2.5; ctx.beginPath(); ctx.arc(tx, ty, r, 0, TAU); ctx.stroke();
    ctx.lineWidth = 2; ctx.beginPath();
    ctx.moveTo(tx - r * 1.5, ty); ctx.lineTo(tx - r * .4, ty); ctx.moveTo(tx + r * .4, ty); ctx.lineTo(tx + r * 1.5, ty);
    ctx.moveTo(tx, ty - r * 1.5); ctx.lineTo(tx, ty - r * .4); ctx.moveTo(tx, ty + r * .4); ctx.lineTo(tx, ty + r * 1.5); ctx.stroke();
    ctx.fillStyle = '#ff3a2a'; ctx.beginPath(); ctx.arc(tx, ty, 2.5, 0, TAU); ctx.fill();
  }
}
function nearby(c, r, n, f) {
  if (n <= 0) return [];
  return ents.filter(e => e.alive && f(e) && dist(e, c) <= r).sort((a, b) => dist(a, c) - dist(b, c)).slice(0, n);
}
function canLearn(i, h = hero) {
  const s = h.abil[i], a = AB(h)[i];
  if (h.points <= 0 || s.lvl >= a.max) return false;
  return h.lvl >= (a.ult ? (s.lvl + 1) * 6 : s.lvl * 2 + 1);
}
function learnAbil(i, h) {
  const a = AB(h)[i], lv = h.abil[i].lvl + 1;
  AB(h).forEach((b, k) => { if (k === i || (a.share && b.share === a.share)) h.abil[k].lvl = lv; });
  h.points--; recalc(h, true);
}
function learn(i) { if (canLearn(i)) { learnAbil(i, hero); burst(hero.x, hero.y - 2, '#ffc83a', 10); refreshHud(true); } }
function later(t, fn) { timers.push({ t: T + t, fn }); }

// ===================== ПРЕДМЕТЫ =====================
function planCost(d, h = hero) {
  const used = new Set();
  const plan = (x, top) => {
    if (!top) { const i = h.inv.findIndex((it, k) => it && it.d.id === x.id && !used.has(k)); if (i >= 0) { used.add(i); return 0; } }
    return x.cost + x.parts.reduce((a, p) => a + plan(ITEM[p], false), 0);
  };
  const c = plan(d, true);
  return { cost: c, used };
}
function nearShop() {
  if (!hero.alive) return true;
  const b = BATHS[hero.team]; return Math.hypot(hero.x - b.x, hero.y - b.y) <= b.r + 9;
}
// «Самокат курьеру» — улучшение, а не предмет. err — текст ошибки или null
function buyScooter(h) {
  const c = h.courier, d = ITEM.scooter;
  if (!c) return 'Курьера ещё нет';
  if (c.up) return 'Курьер уже на самокате';
  if (h.gold < d.cost) return 'Не хватает ' + (d.cost - h.gold) + ' золота';
  h.gold -= d.cost; upgradeCourier(c); return null;
}
// Покупка в тайник: детали, что уже есть у героя, идут в сборку (платишь только за недостающее)
function buyToStash(h, d) {
  const { cost, used } = planCost(d, h);
  if (h.gold < cost) return 'Не хватает ' + (cost - h.gold) + ' золота';
  if (h.stash.length >= 6) return 'Тайник полон — забери вещи курьером';
  h.gold -= cost; used.forEach(k => h.inv[k] = null); if (used.size) recalc(h, true);
  h.stash.push(d); return null;
}
function buy(d) {
  if (d.id === 'scooter') {
    const err = buyScooter(hero);
    if (err) toast(err, '#ff8a80'); else burst(hero.x, hero.y - 2, '#4dd95a', 14);
    renderShop(); refreshHud(true); return;
  }
  if (!nearShop()) {
    const err = buyToStash(hero, d);
    if (err) return toast(err, '#ff8a80');
    toast('В тайник: ' + d.name + ' — курьер привезёт', '#ffd24a');
    renderShop(); refreshHud(true); return;
  }
  if (d.id === 'ticket') {
    if (hero.tpN >= 5) return toast('Больше 5 билетов не влезает', '#ff8a80');
    if (hero.gold < d.cost) return toast('Не хватает ' + (d.cost - hero.gold) + ' золота', '#ff8a80');
    hero.gold -= d.cost; hero.tpN++; toast('Билет — в слот электрички (T)', '#8fd0ff'); renderShop(); refreshHud(true); return;
  }
  const { cost, used } = planCost(d);
  if (hero.gold < cost) return toast('Не хватает ' + (cost - hero.gold) + ' золота', '#ff8a80');
  const free = hero.inv.filter(x => !x).length + used.size;
  if (free < 1) return toast('Инвентарь полон', '#ff8a80');
  hero.gold -= cost;
  used.forEach(k => hero.inv[k] = null);
  hero.inv[hero.inv.findIndex(x => !x)] = { d, cd: 0 };
  recalc(hero, true);
  toast('Куплено: ' + d.name, '#7dff8a');
  burst(hero.x, hero.y - 2, '#ffc83a', 14);
  renderShop(); refreshHud(true);
}
function sell(slot) {
  const it = hero.inv[slot]; if (!it) return;
  if (!nearShop()) return toast('Продать можно у своей бани', '#ff8a80');
  const p = Math.floor(totalCost(it.d) / 2);
  hero.gold += p; hero.inv[slot] = null; recalc(hero, true);
  toast('Продано +' + p, '#ffc83a'); refreshHud(true); renderShop();
}
let selSlot = -1;
function useItem(slot, h = hero) {
  const it = h.inv[slot]; if (!it || slot > 5 || !h.alive) return;
  const d = it.d;
  if (!d.act) return;
  if (it.cd > 0) return say('Перезарядка ' + Math.ceil(it.cd), undefined, h);
  if (stunned(h)) return say('Оглушён!', undefined, h);
  if (d.act === 'heal') { h.st.hots.push({ hp: d.av, mp: 0, e: T + d.ad }); say('Ням!', '#7dff8a', h); }
  if (d.act === 'mana') { h.st.hots.push({ hp: 0, mp: d.av, e: T + d.ad }); say('Хрусть!', '#8fb0ff', h); }
  if (d.act === 'speed') { h.st.msb.push({ v: d.av, e: T + d.ad }); say('Вжух!', '#ffc83a', h); }
  if (d.act === 'immune') { h.st.immune = T + d.ad; h.st.slows = []; h.st.dots = []; h.st.stun = 0; say('ЧЁРНЫЙ ПОЯС!', '#ffc83a', h); }
  if (d.act === 'tp') { if (h.tp) return; h.tp = { end: T + d.ad, x: h.x, y: h.y }; h.order = null; h.target = null; say('Еду в баню...', '#8fd0ff', h); }
  it.cd = d.acd || 0;
  burst(h.x, h.y - 2, d.c, 10);
  if (d.cons) { h.inv[slot] = null; recalc(h, true); }
  if (h === hero) refreshHud(true);
}

// ===================== ОБНОВЛЕНИЕ =====================
function moveToward(u, tx, ty, dt) {
  if (u.channel) return false;
  const dx = tx - u.x, dy = ty - u.y, d = Math.hypot(dx, dy); if (d < 0.05) return true;
  u.face = Math.atan2(dy, dx);
  const step = Math.min(d, speedOf(u) * dt); u.x += dx / d * step; u.y += dy / d * step;
  return step >= d;
}
function collide() {
  const movers = ents.filter(e => e.alive && (e.kind === 'creep' || e.kind === 'hero'));
  const solid = ents.filter(e => e.alive && (e.kind === 'tower' || e.kind === 'throne'));
  for (const u of movers) {
    for (const s of solid) { const d = dist(u, s), m = s.r + u.r; if (d < m && d > 0.001) { const nx = (u.x - s.x) / d, ny = (u.y - s.y) / d, side = (u.side ||= Math.random() < .5 ? 1 : -1);
      u.x = s.x + nx * m - ny * .06 * side; u.y = s.y + ny * m + nx * .06 * side; } }
    u.x = clamp(u.x, -58, 58); u.y = clamp(u.y, -58, 58);
  }
  for (let i = 0; i < movers.length; i++) for (let j = i + 1; j < movers.length; j++) {
    const a = movers[i], b = movers[j], d = dist(a, b), m = (a.r + b.r) * 0.8;
    if (d < m && d > 0.001) { const p = (m - d) / 2, nx = (b.x - a.x) / d, ny = (b.y - a.y) / d; a.x -= nx * p; a.y -= ny * p; b.x += nx * p; b.y += ny * p; }
  }
}

function update(dt) {
  T += dt;
  // таймеры
  for (const t of timers.filter(t => t.t <= T)) t.fn();
  timers = timers.filter(t => t.t > T);

  updateVision();
  // волны
  if (T >= nextWave) { spawnWave(); nextWave += 30; }

  // защита строений
  for (const e of ents) if (e.prot) e.invuln = e.protAny ? e.prot.every(p => p.alive) : e.prot.some(p => p.alive);

  // лесные лагеря: появляются с 0:30, потом раз в минуту, если лагерь пуст
  for (const c of CAMPS) if (T >= c.next) {
    c.next = T + 60;
    const busy = ents.some(e => e.alive && e.camp === c.id) || heroes.some(h => h.alive && Math.hypot(h.x - c.x, h.y - c.y) < 8);
    if (!busy) spawnCamp(c);
  }

  // пассивное золото
  goldTick += dt * PASSIVE_GOLD; while (goldTick >= 1) { goldTick -= 1; for (const h of heroes) h.gold += 1; }

  for (const u of ents) {
    if (!u.alive) continue;
    tickStatus(u, dt);
    if (!u.alive) continue;
    u.hp = Math.min(u.maxHp, u.hp + u.regen * dt);
    if (u.maxMp) u.mp = Math.min(u.maxMp, u.mp + u.mpr * dt);
    u.cd -= dt;
    if (u.kind === 'tower') towerAI(u, dt);
    else if (u.kind === 'creep') { if (u.courier) courierAI(u, dt); else creepAI(u, dt); }
    else if (u.kind === 'hero') heroAI(u, dt);
  }

  // бани
  for (const b of BATHS) {
    for (const u of ents) if (u.alive && u.team === b.team && (u.kind === 'hero' || u.kind === 'creep') && Math.hypot(u.x - b.x, u.y - b.y) <= b.r) {
      u.hp = Math.min(u.maxHp, u.hp + u.maxHp * .06 * dt); if (u.maxMp) u.mp = Math.min(u.maxMp, u.mp + u.maxMp * .06 * dt);
    }
    if (Math.random() < dt * 9) { const a = rnd(0, TAU), r = Math.sqrt(Math.random()) * (b.r - .6); bubbles.push({ x: b.x + Math.cos(a) * r, y: b.y + Math.sin(a) * r * .55, h: 0, s: rnd(.08, .22), life: rnd(.8, 1.5) }); }
  }
  bubbles.forEach(b => { b.h += dt * 1.2; b.life -= dt; }); bubbles = bubbles.filter(b => b.life > 0);

  for (const h of heroes) heroTick(h, dt);
  tickSanya(dt);
  botGlyph();

  // снаряды
  for (const p of projs) {
    const t = p.tgt; if (!t.alive) { p.dead = true; continue; }
    const tx = t.x, ty = t.y - (t.kind === 'tower' || t.kind === 'throne' ? 2 : 1);
    const dx = tx - p.x, dy = ty - p.y, d = Math.hypot(dx, dy), st = p.speed * dt; p.spin += dt * 12;
    if (d <= st) { p.dead = true; if (p.dmg > 0) damage(p.src, t, p.dmg, p.type); if (p.onHit && t.alive) p.onHit(t);
      burst(tx, ty, { pizza: '#ff9a30', sauce: '#e02a1a', stone: '#9a8a7a', burger: '#d9903a', pizzaT: '#ffb040', sausage: '#b8502e', goldbullet: '#ffd24a', goldbig: '#fff0a0' }[p.look] || '#ffd24a', 7); }
    else { p.x += dx / d * st; p.y += dy / d * st; }
  }
  projs = projs.filter(p => !p.dead);

  collide();
  ents = ents.filter(e => e.alive || e.kind !== 'creep' || e.courier || T - e.died < 0.4);
  for (const h of heroes) if (h.courier && !h.courier.alive && T >= h.courier.respawn) courierRespawn(h.courier);
  stashAutoGive();

  parts.forEach(p => { p.vz -= 18 * dt; p.x += p.vx * dt; p.y += p.vy * dt; p.z = Math.max(0, p.z + p.vz * dt); p.life -= dt; }); parts = parts.filter(p => p.life > 0);
  texts.forEach(t => t.life -= dt); texts = texts.filter(t => t.life > 0);

  if (cam.follow && hero.alive) { cam.x += (hero.x - cam.x) * Math.min(1, dt * 6); cam.y += (hero.y - cam.y) * Math.min(1, dt * 6); }
}

function botGlyph() {
  if (!bot || T < glyph[bot.team].ready) return;
  const hurt = ents.find(e => e.alive && e.team === bot.team && (e.kind === 'tower' || e.kind === 'throne') && !e.invuln && e.hp < e.maxHp * .3 &&
    ents.filter(x => x.alive && enemies(x, e) && !x.neutral && dist(x, e) < 10).length >= 3);
  if (hurt) useGlyph(bot.team);
}
function heroTick(h, dt) {
  if (h.tpCd > 0) h.tpCd -= dt;
  // жар мангала
  if (h.alive && h.burn) { h.burnT = (h.burnT || 0) + dt; if (h.burnT >= .5) { h.burnT -= .5;
    for (const e of ents) if (e.alive && enemies(h, e) && e.kind !== 'tower' && e.kind !== 'throne' && dist(e, h) < 6) damage(h, e, h.burn * .5, 'mag'); } }
  // перезарядки
  h.abil.forEach(a => a.cd = Math.max(0, a.cd - dt));
  h.abil.forEach((s, k) => { const a = AB(h)[k]; if (a.charges && s.lvl && s.ch < a.charges) { s.chT -= dt; if (s.chT <= 0) { s.ch++; s.chT = s.ch < a.charges ? a.chargeCd : 0; } } });
  if (h.ladder && T >= h.ladder) { h.ladder = 0; recalc(h, true); }
  h.inv.forEach(it => { if (it) it.cd = Math.max(0, it.cd - dt); });
  // воскрешение
  if (!h.alive && T >= h.respawn) {
    const b = BATHS[h.team];
    Object.assign(h, { alive: true, x: b.x, y: b.y, hp: h.maxHp, mp: h.maxMp, st: St(), order: null, target: null, tp: null, mode: 'lane' });
    if (h === hero) cam.follow = true;
    if (h.def === MAX) speak(h, 1);
  }
}

function towerAI(u, dt) {
  if (u.target && (!u.target.alive || edge(u, u.target) > u.range || u.target.invuln)) u.target = null;
  if ((u.scan -= dt) <= 0) {
    u.scan = .2;
    // Сначала крипы, героя — только если больше некого
    let creep = null, heroT = null, cd = 1e9, hd = 1e9;
    for (const e of ents) {
      if (!e.alive || !enemies(u, e) || e.invuln || e.neutral) continue;
      const d = edge(u, e); if (d > u.range) continue;
      if (e.kind === 'creep' && d < cd) { cd = d; creep = e; }
      else if (e.kind === 'hero' && d < hd) { hd = d; heroT = e; }
    }
    if (!u.target || (u.target.kind === 'hero' && creep)) u.target = creep || heroT;
  }
  if (u.target && u.cd <= 0) attack(u, u.target);
}

function neutralAI(u, dt) {
  if (stunned(u)) return;
  const away = Math.hypot(u.x - u.home.x, u.y - u.home.y);
  if (u.target && (!u.target.alive || away > 11 || edge(u, u.target) > 9)) { u.target = null; u.leash = true; }
  if (u.target && !u.leash) { if (edge(u, u.target) > u.range) moveToward(u, u.target.x, u.target.y, dt); else if (u.cd <= 0) attack(u, u.target); return; }
  if (away > 0.3) { moveToward(u, u.home.x, u.home.y, dt); u.hp = Math.min(u.maxHp, u.hp + u.maxHp * .1 * dt); } else u.leash = false;
}
function creepAI(u, dt) {
  if (u.neutral) return neutralAI(u, dt);
  if (stunned(u)) return;
  if (!u.target || !u.target.alive || edge(u, u.target) > 11 || u.target.invuln) u.target = null;
  if ((u.scan -= dt) <= 0) { u.scan = .25; if (!u.target) u.target = findEnemy(u, u.ranged ? 8 : 7); }
  if (u.target) {
    if (edge(u, u.target) > u.range) moveToward(u, u.target.x, u.target.y, dt);
    else if (u.cd <= 0) attack(u, u.target);
  } else {
    const w = u.path[Math.min(u.wp, u.path.length - 1)];
    if (Math.hypot(w.x - u.x, w.y - u.y) < 2.5 && u.wp < u.path.length - 1) u.wp++;
    moveToward(u, w.x, w.y, dt);
  }
}

function heroAI(u, dt) {
  if (u.channel) return;
  // телепорт (электричка или сапоги)
  if (u.tp) {
    if (stunned(u) || Math.hypot(u.x - u.tp.x, u.y - u.tp.y) > .5) { u.tp = null; say('Телепорт сорван', '#ff8a80', u); }
    else if (T >= u.tp.end) {
      const b = BATHS[u.team], dx = u.tp.dx ?? b.x, dy = u.tp.dy ?? b.y, r = u.tp.dx != null ? .8 : 1.5;
      u.x = dx + rnd(-r, r); u.y = dy + rnd(-r * .7, r * .7); u.tp = null; burst(u.x, u.y - 1, '#8fd0ff', 24);
      if (u === hero) { cam.follow = true; cam.x = u.x; cam.y = u.y; }
    }
    else if (Math.random() < dt * 4 && u.tp.dx != null) burst(u.tp.dx, u.tp.dy - .5, '#8fd0ff', 3);
    return;
  }
  if (u.bot) return botAI(u, dt);
  if (stunned(u)) return;
  // подход для способности
  if (pending) {
    if (!validTarget(pending.i, pending.t)) pending = null;
    else if (edge(u, pending.t) <= AB(u)[pending.i].range) { const p = pending; pending = null; doCast(p.i, p.t); }
    else { moveToward(u, pending.t.x, pending.t.y, dt); return; }
  }
  const o = u.order;
  if (o && o.type === 'attack') {
    const t = o.t;
    if (!t.alive || t.invuln) { if (t.alive) say('Неуязвим — сначала сломай башню перед ним'); u.order = null; }
    else if (edge(u, t) > u.range) { moveToward(u, t.x, t.y, dt); return; }
    else { if (u.cd <= 0) attack(u, t); return; }
  }
  if (o && o.type === 'move') { if (moveToward(u, o.x, o.y, dt)) u.order = null; return; }
  // стоит — бьёт ближайшего врага рядом
  if (!u.target || !u.target.alive || edge(u, u.target) > 6 || u.target.invuln) u.target = null;
  if ((u.scan -= dt) <= 0) { u.scan = .25; if (!u.target) u.target = findEnemy(u, 5); }
  if (u.target) { if (edge(u, u.target) > u.range) moveToward(u, u.target.x, u.target.y, dt); else if (u.cd <= 0) attack(u, u.target); }
}

// ===================== БОТ-ГЕРОЙ =====================
const BOT_BUILD = ['band_str', 'sneakers', 'vanguard', 'bkb', 'crystal', 'crown', 'mangal'];
const BOT_SKILL = [3, 0, 1, 0, 2, 0, 1, 0, 1, 1, 2, 2, 2]; // R когда можно, дальше Q > W > E
function botLearn(h) {
  let guard = 20;
  while (h.points > 0 && guard--) {
    let done = false;
    for (const i of (h.def.botSkill || (h.def === SANYA ? [3, 0, 0, 0, 0] : BOT_SKILL))) if (canLearn(i, h)) { learnAbil(i, h); done = true; break; }
    if (!done) break;
  }
}
function botBuyItem(h, d) {
  const { cost, used } = planCost(d, h);
  if (h.gold < cost) return false;
  if (h.inv.filter(x => !x).length + used.size < 1) return false;
  h.gold -= cost; used.forEach(k => h.inv[k] = null);
  h.inv[h.inv.findIndex(x => !x)] = { d, cd: 0 };
  recalc(h, true); burst(h.x, h.y - 2, '#ffc83a', 10);
  return true;
}
function botBuyStep(h, d) {   // целиком, а если не хватает — самую нужную часть
  if (botBuyItem(h, d)) return true;
  const have = h.inv.filter(Boolean).map(x => x.d.id);
  for (const p of d.parts) { const k = have.indexOf(p); if (k >= 0) { have.splice(k, 1); continue; } if (botBuyStep(h, ITEM[p])) return true; }
  return false;
}
const botMarkBuilt = h => { for (const id of BOT_BUILD) if (!h.built.has(id) && h.inv.some(x => x && x.d.id === id)) h.built.add(id); };
const botScooter = h => { if (h.courier && !h.courier.up && T - matchT0 > 180 && h.gold >= 400) buyScooter(h); };
function botShop(h) {
  if ((h.shopT || 0) > T) return; h.shopT = T + 1;
  // у бани вещи из тайника сами переходят в инвентарь
  for (let i = h.stash.length - 1; i >= 0; i--) if (giveItem(h, h.stash[i])) h.stash.splice(i, 1);
  botMarkBuilt(h);
  if (!h.inv.some(x => x && x.d.id === 'shawarma') && h.gold >= 110 + 200) botBuyItem(h, ITEM.shawarma);
  botScooter(h);
  const next = BOT_BUILD.find(id => !h.built.has(id));
  if (next && botBuyStep(h, ITEM[next]) && h.inv.some(x => x && x.d.id === next)) h.built.add(next);
}
// Вдали от бани: покупка в тайник и вызов своего курьера, когда он свободен
function botStashStep(h, d) {
  if (!buyToStash(h, d)) return true;
  const have = h.inv.filter(Boolean).map(x => x.d.id);
  for (const p of d.parts) { const k = have.indexOf(p); if (k >= 0) { have.splice(k, 1); continue; } if (botStashStep(h, ITEM[p])) return true; }
  return false;
}
function botRemoteShop(h) {
  if ((h.shopT || 0) > T) return; h.shopT = T + 1.5;
  const c = h.courier;
  if (!c || !c.alive || c.state !== 'idle') return;
  botMarkBuilt(h); botScooter(h);
  const next = BOT_BUILD.find(id => !h.built.has(id));
  if (next && !h.stash.length) botStashStep(h, ITEM[next]);
  if (h.stash.length && h.inv.some(x => !x)) sendCourier(h);
}
function botCast(h, i, t, dt) {   // true — бот занят этой способностью
  const a = AB(h)[i], s = h.abil[i];
  if (!s.lvl || s.cd > 0 || h.mp < at(a.mana, s.lvl) || !validTarget(i, t, h)) return false;
  if (edge(h, t) > a.range) { moveToward(h, t.x, t.y, dt); return true; }
  doCast(i, t, h); return true;
}
const enemyTowerAt = (h, x, y) => ents.find(t => t.alive && t.kind === 'tower' && enemies(h, t) && Math.hypot(t.x - x, t.y - y) < t.range + 1.2);
const alliesNear = (h, x, y, r) => ents.filter(e => e.alive && e.kind === 'creep' && !e.courier && e.team === h.team && Math.hypot(e.x - x, e.y - y) < r).length;
// Саня-бот: разворачивается к цели и бьёт подходящим по дальности койлом; ульта — когда враг рядом и сосисок много
function sanyaBotCast(u, t, isHero) {
  const d = dist(u, t), A = AB(u);
  if (isHero && u.abil[3].lvl && !u.abil[3].cd && u.mp >= at(A[3].mana, u.abil[3].lvl) && d < 4.5 && ((u.sausage || 0) >= 8 || t.hp < 500)) {
    doCast(3, u, u); return true; }
  for (let i = 0; i < 3; i++) { const a = A[i], s = u.abil[i];
    if (!s.lvl || s.cd > 0 || u.mp < at(a.mana, s.lvl)) continue;
    if (Math.abs(d - a.dist) <= a.rad * .7) { u.face = Math.atan2(t.y - u.y, t.x - u.x); doCast(i, u, u); return true; } }
  return false;
}
function botSafe(h, x, y) { const t = enemyTowerAt(h, x, y); return !t || alliesNear(h, t.x, t.y, t.range) >= 3; }
function botAI(u, dt) {
  if (stunned(u) || u.channel) return;
  if (u.def === MAX && (u.snipeT = (u.snipeT || 0) - dt) <= 0) { u.snipeT = .7; if (maxSnipe(u)) return; }
  const b = BATHS[u.team], atBath = Math.hypot(u.x - b.x, u.y - b.y) <= b.r, hpR = u.hp / u.maxHp;
  if (atBath) botShop(u);
  // отступление
  if (u.mode !== 'retreat' && hpR < 0.28) {
    u.mode = 'retreat'; u.target = null;
    const k = u.inv.findIndex((x, i) => i < 6 && x && x.d.act === 'heal'); if (k >= 0) useItem(k, u);
  }
  if (u.mode === 'retreat') {
    if (atBath) { if (hpR > 0.95 && u.mp > u.maxMp * .7) u.mode = 'lane'; return; }
    const bk = u.inv.findIndex((x, i) => i < 6 && x && x.d.act === 'immune' && !x.cd);
    if (bk >= 0 && u.st.dots.length + u.st.slows.length) useItem(bk, u);
    if (canTp(u) && Math.hypot(u.x - b.x, u.y - b.y) > 30 && hpR > .12) { startTicket(u, b.x + rnd(-1.5, 1.5), b.y + rnd(-1, 1), 'Еду в баню...'); return; }
    moveToward(u, b.x, b.y, dt); return;
  }
  // драка с героем
  let foe = null, fd = 10;
  for (const f of heroes) if (f !== u && f.alive && enemies(u, f) && canSee(u.team, f) && dist(u, f) < fd) { fd = dist(u, f); foe = f; }
  // кто рядом: свои и чужие герои
  const allyHeroes = foe ? heroes.filter(h => h.alive && h.team === u.team && dist(h, foe) < 12).length : 0;
  const foeHeroes = foe ? heroes.filter(h => h.alive && h.team !== u.team && canSee(u.team, h) && dist(h, foe) < 12).length : 0;
  // вдали от бани — покупка курьером; без курьера идём в баню сами
  if (!atBath) botRemoteShop(u);
  if (u.mode === 'lane' && !foe && !(u.courier && u.courier.alive) && u.gold >= 1100 && !atBath) { u.mode = 'retreat'; u.target = null; return; }
  if (foe) {
    const fR = foe.hp / foe.maxHp;
    const brave = { easy: -.15, normal: 0, hard: .2 }[SETTINGS.diff] || 0;
    let want = (fR < hpR + brave || foe.hp < 250 || allyHeroes > foeHeroes) && hpR > 0.4 && allyHeroes + 0.5 >= foeHeroes && (!enemyTowerAt(u, foe.x, foe.y) || foe.hp < 150);
    // первые 40 секунд боя сами в драку не лезем — только если бьют союзного героя рядом
    if (want && T - matchT0 < 40) {
      const ft = foe.order && foe.order.type === 'attack' ? foe.order.t : foe.target;
      if (!(ft && ft.kind === 'hero' && ft.team === u.team && dist(ft, u) < 8)) want = false;
    }
    if (want) {
      const bk = u.inv.findIndex((x, i) => i < 6 && x && x.d.act === 'immune' && !x.cd);
      if (bk >= 0 && hpR < 0.6) useItem(bk, u);
      if ((u.react = (u.react || 0) - dt) <= 0) { u.react = { easy: 1.6, normal: .9, hard: .5 }[SETTINGS.diff] || .9;
        const skill = { easy: .35, normal: .65, hard: .95 }[SETTINGS.diff] || .65;
        if (u.def === SANYA) { if (Math.random() < skill && sanyaBotCast(u, foe, true)) { u.react = 1.1; return; } }
        else if (u.def === MAX) { if (Math.random() < skill && maxBotCast(u, foe, true)) { u.react = 1.1; return; } }
        else if (Math.random() < skill && (botCast(u, 0, foe, dt) || botCast(u, 1, foe, dt))) { u.react = 1.2; return; } }
      if (u.def === HERO && dist(u, foe) < 5) botCast(u, 2, u, dt);
      if (edge(u, foe) > u.range) moveToward(u, foe.x, foe.y, dt); else if (u.cd <= 0) attack(u, foe);
      return;
    }
  }
  // какая линия: если на своей линии все вражеские башни сломаны — идём туда, где они ещё стоят
  if (u.lane !== 'jungle' && (u.laneT = (u.laneT || 0) - dt) <= 0) {
    u.laneT = 5;
    const alive = ln => ents.some(t => t.alive && t.kind === 'tower' && t.lane === ln && enemies(u, t));
    if (!alive(u.lane)) { const rest = LANE_IDS.filter(alive); if (rest.length) u.lane = rest[Math.floor(Math.random() * rest.length)]; }
  }
  let spot = null;
  // лес: фармим лагеря на своей половине
  if (u.lane === 'jungle' && hpR > 0.45) {
    let best = null, bd = 1e9;
    for (const e of ents) if (e.alive && e.neutral && CAMPS[e.camp].side === u.team) { const d = dist(u, e); if (d < bd) { bd = d; best = e; } }
    if (best) { u.target = best; if (edge(u, best) > u.range) moveToward(u, best.x, best.y, dt); else if (u.cd <= 0) attack(u, best);
      if (best.boss && (u.react = (u.react || 0) - dt) <= 0) { u.react = 1; if (u.def === SANYA) sanyaBotCast(u, best, false); else if (u.def === MAX) maxBotCast(u, best, false); else botCast(u, 0, best, dt); } return; }
  }
  const lane = u.lane === 'jungle' ? 'mid' : u.lane, L = LANE_DEF[lane], path = L.path[u.team];
  const throne = ents.find(e => e.kind === 'throne' && e.team === u.team);
  const toBase = (o, k) => { const dx = throne.x - o.x, dy = throne.y - o.y, d = Math.hypot(dx, dy) || 1; return { x: o.x + dx / d * k, y: o.y + dy / d * k }; };
  // держимся за самым передним своим крипом на этой линии
  const prog = e => e.wp * 1000 - Math.hypot(e.x - e.path[e.wp].x, e.y - e.path[e.wp].y);
  const mine = ents.filter(e => e.alive && e.kind === 'creep' && e.team === u.team && e.lane === lane);
  if (mine.length) { const f = mine.reduce((a, e) => prog(e) > prog(a) ? e : a), w = path[f.wp];
    const dx = w.x - f.x, dy = w.y - f.y, d = Math.hypot(dx, dy) || 1; spot = { x: f.x - dx / d * 1.6, y: f.y - dy / d * 1.6 }; }
  else {
    const tw = ents.filter(e => e.alive && e.kind === 'tower' && e.team === u.team && e.lane === lane).sort((a, c) => a.tier - c.tier)[0];
    spot = tw ? toBase(tw, 3.5) : toBase(L.spawn[u.team], 0);
  }
  { const t = enemyTowerAt(u, spot.x, spot.y); if (t && !botSafe(u, spot.x, spot.y)) spot = toBase(t, t.range + 2.5); }
  // цель рядом: крипы, потом строения (только под прикрытием крипов)
  if (u.target && (!u.target.alive || u.target.invuln || edge(u, u.target) > 7 || !botSafe(u, u.target.x, u.target.y))) u.target = null;
  if (u.target && u.target.team === u.team && u.target.hp >= u.target.maxHp * .5) u.target = null;
  if ((u.scan -= dt) <= 0) {
    u.scan = .3;
    // денай: свой крип почти умер — добиваем
    if (!u.target) {
      const ch = { easy: .12, normal: .3, hard: .6 }[SETTINGS.diff] || .3;
      const dn = ents.find(e => e.alive && e.kind === 'creep' && !e.courier && !e.neutral && e.team === u.team && e.hp < e.maxHp * .5 && e.hp < u.dmg * .9 && edge(u, e) <= u.range + 1);
      if (dn && Math.random() < ch) u.target = dn;
    }
    if (!u.target) {
      let best = null, bd = 7;
      for (const e of ents) {
        if (!e.alive || !enemies(u, e) || e.invuln || e.kind === 'hero' || e.neutral) continue;
        if (!botSafe(u, e.x, e.y)) continue;
        if ((e.kind === 'tower' || e.kind === 'throne') && (hpR < 0.5 || alliesNear(u, e.x, e.y, e.kind === 'tower' ? e.range : 8) < 3)) continue;
        const d = edge(u, e) + (e.kind === 'creep' ? 0 : 2); if (d < bd) { bd = d; best = e; }
      }
      u.target = best;
    }
  }
  if (u.target && u.def === MAX && u.mp > u.maxMp * .6 && (u.farm = (u.farm || 0) - dt) <= 0) { u.farm = 2;
    if (u.abil[0].ch >= 2 && ents.filter(e => e.alive && e.kind === 'creep' && enemies(u, e) && !e.neutral && dist(e, u.target) < 3.5).length >= 4) maxBotCast(u, u.target, false); }
  if (u.target && u.def === SANYA && u.mp > u.maxMp * .55 && (u.farm = (u.farm || 0) - dt) <= 0) { u.farm = 1.5;
    if (ents.filter(e => e.alive && e.kind === 'creep' && enemies(u, e) && !e.neutral && dist(e, u.target) < 2.4).length >= 3) sanyaBotCast(u, u.target, false); }
  if (u.target) { if (edge(u, u.target) > u.range) moveToward(u, u.target.x, u.target.y, dt); else if (u.cd <= 0) attack(u, u.target); return; }
  if (Math.hypot(spot.x - u.x, spot.y - u.y) > 1) moveToward(u, spot.x, spot.y, dt);
}

// ===================== ЭФФЕКТЫ =====================
function burst(x, y, col, n) {
  for (let i = 0; i < n; i++) { const a = rnd(0, TAU), sp = rnd(1.5, 5);
    parts.push({ x, y, z: 1, vx: Math.cos(a) * sp, vy: Math.sin(a) * sp * .6, vz: rnd(3, 8), life: rnd(.35, .7), max: .7, s: rnd(.1, .22), c: col }); }
}
function floatText(x, y, s, c, size) { texts.push({ x, y, s, c, size, life: 1.2 }); }
function say(s, c = '#ff9a9a', u = hero) { floatText(u.x, u.y - 3.6, s, c, 14); }
let toastT;
function toast(s, c) { const t = $('toast'); t.textContent = s; t.style.color = c || '#fff'; t.hidden = false; t.style.opacity = 1;
  clearTimeout(toastT); toastT = setTimeout(() => { t.style.opacity = 0; setTimeout(() => t.hidden = true, 400); }, 1800); }

// ===================== ОТРИСОВКА =====================
const sx = x => (x - cam.x) * cam.ppu + W / 2;
const sy = y => (y - cam.y) * cam.ppu + H / 2;
const toWorld = (px, py) => ({ x: (px - W / 2) / cam.ppu + cam.x, y: (py - H / 2) / cam.ppu + cam.y });

let groundPat = null;
function makeGround() {
  const c = document.createElement('canvas'); c.width = c.height = 128; const g = c.getContext('2d');
  g.fillStyle = '#2f5a2a'; g.fillRect(0, 0, 128, 128);
  for (let i = 0; i < 260; i++) { g.fillStyle = Math.random() < .5 ? 'rgba(70,120,55,.35)' : 'rgba(20,50,20,.35)';
    g.fillRect(Math.random() * 128, Math.random() * 128, 2 + Math.random() * 3, 2 + Math.random() * 3); }
  groundPat = ctx.createPattern(c, 'repeat');
}

function draw() {
  ctx.setTransform(DPR, 0, 0, DPR, 0, 0);
  const p = cam.ppu;
  // земля
  ctx.fillStyle = '#16230f'; ctx.fillRect(0, 0, W, H);
  ctx.save(); ctx.translate(sx(0), sy(0)); ctx.scale(p / 4, p / 4);
  ctx.fillStyle = groundPat; ctx.fillRect(-60 * 4, -60 * 4, 120 * 4, 120 * 4); ctx.restore();
  // линия
  ctx.save(); ctx.translate(sx(0), sy(0)); ctx.rotate(-Math.PI / 4);
  ctx.fillStyle = '#8a7146'; ctx.fillRect(-66 * p, -4.2 * p, 132 * p, 8.4 * p);
  ctx.fillStyle = 'rgba(0,0,0,.08)'; for (let i = -64; i < 64; i += 3) ctx.fillRect(i * p, -4.2 * p, 1.2 * p, 8.4 * p);
  ctx.restore();
  // боковые линии
  ctx.fillStyle = '#8a7146';
  for (const [x1, y1, x2, y2] of SIDE_RECTS) ctx.fillRect(sx(x1), sy(y1), (x2 - x1) * p, (y2 - y1) * p);
  // лесные лагеря
  for (const c of CAMPS) { ctx.fillStyle = 'rgba(60,40,20,.35)'; ctx.beginPath(); ctx.ellipse(sx(c.x), sy(c.y), 3.2 * p, 2.2 * p, 0, 0, TAU); ctx.fill(); }
  // базы
  for (const [x, y, t] of [[-45, 45, K], [45, -45, M]]) { const g = ctx.createRadialGradient(sx(x), sy(y), 0, sx(x), sy(y), 18 * p);
    g.addColorStop(0, TEAM_COL[t] + '55'); g.addColorStop(1, TEAM_COL[t] + '00'); ctx.fillStyle = g; ctx.fillRect(sx(x) - 18 * p, sy(y) - 18 * p, 36 * p, 36 * p); }
  // бани (бассейн на земле)
  for (const b of BATHS) drawBath(b);
  // радиус способности
  if (targeting >= 0 && hero.alive) { ctx.strokeStyle = 'rgba(255,200,58,.7)'; ctx.setLineDash([6, 6]); ctx.lineWidth = 2;
    ctx.beginPath(); ctx.ellipse(sx(hero.x), sy(hero.y), AB(hero)[targeting].range * p, AB(hero)[targeting].range * p * .9, 0, 0, TAU); ctx.stroke(); ctx.setLineDash([]); }

  // Саня: куда упадут койлы (подсказка для игрока)
  if (SETTINGS.aim !== '0' && hero.def === SANYA && hero.alive && hero.abil[0].lvl) {
    const f = hero.face ?? 0;
    AB(hero).slice(0, 3).forEach((a, i) => { const cx = hero.x + Math.cos(f) * a.dist, cy = hero.y + Math.sin(f) * a.dist;
      ctx.strokeStyle = hero.abil[i].cd > 0 ? 'rgba(255,255,255,.08)' : 'rgba(255,140,60,.28)'; ctx.lineWidth = 1.5; ctx.setLineDash([4, 5]);
      ctx.beginPath(); ctx.ellipse(sx(cx), sy(cy), a.rad * p, a.rad * p * .9, 0, 0, TAU); ctx.stroke(); ctx.setLineDash([]); });
  }
  for (const f of fx) if (f.kind === 'rain') drawRainGround(f, p);
  for (const h of heroes) if (h.alive && h.tp && h.tp.dx != null) drawTpRing(h.tp, p);
  for (const f of fx) if (f.kind === 'coil') {
    const k = clamp((T - f.t0) / (f.t1 - f.t0), 0, 1), fade = T > f.t1 ? 1 - (T - f.t1) / .5 : 1;
    ctx.globalAlpha = Math.max(0, fade); ctx.fillStyle = T > f.t1 ? 'rgba(120,40,20,.45)' : `rgba(200,50,43,${.15 + .2 * k})`;
    ctx.beginPath(); ctx.ellipse(sx(f.x), sy(f.y), f.r * p, f.r * p * .9, 0, 0, TAU); ctx.fill();
    ctx.strokeStyle = '#ff7a3a'; ctx.lineWidth = 2; ctx.stroke(); ctx.globalAlpha = 1;
  }

  // всё, что стоит — по глубине
  const list = [];
  const view = 30 * 26 / p;
  for (const d of decor) if (Math.abs(d.x - cam.x) < view && Math.abs(d.y - cam.y) < view) list.push({ y: d.y, f: () => drawDecor(d) });
  for (const h of HUTS) list.push({ y: h.y, f: () => drawHut(h, HUTS.indexOf(h)) });
  for (const e of ents) if ((e.alive || e.kind === 'creep') && canSee(playerTeam, e)) list.push({ y: e.y, f: () => drawEnt(e) });
  list.sort((a, b) => a.y - b.y).forEach(o => o.f());

  // снаряды
  for (const pr of projs) drawProj(pr);
  drawMaxOverlay(p);
  for (const f of fx) if (f.kind === 'coil') for (const d of f.drops) {
    const k = clamp((T - f.t0) / ((f.t1 - f.t0) * d.d), 0, 1), h = (1 - k * k) * 7;
    if (T > f.t1 + .25) continue;
    drawSausage(ctx, sx(f.x + d.x), sy(f.y + d.y) - h * p, .5 * p, .13 * p, k < 1 ? T * 9 + d.x : 0.3);
  }
  for (const l of sausLines) drawSausage(ctx, sx(l.x), sy(l.y) - 1.2 * p, .6 * p, .15 * p, l.spin);
  // частицы
  for (const q of parts) { ctx.globalAlpha = Math.min(1, q.life / .3); ctx.fillStyle = q.c;
    const s = q.s * p * (q.life / q.max + .3); ctx.fillRect(sx(q.x) - s / 2, sy(q.y) - q.z * p * .5 - s / 2, s, s); }
  ctx.globalAlpha = 1;
  drawFog();
  // полоски здоровья
  for (const e of ents) if (e.alive && canSee(playerTeam, e)) drawBar(e);
  // тексты
  for (const t of texts) { const k = 1 - t.life / 1.2; ctx.globalAlpha = Math.min(1, t.life * 2);
    ctx.font = `800 ${Math.round(t.size * (1 + .3 * (1 - k)))}px Rubik, system-ui, sans-serif`; ctx.textAlign = 'center';
    ctx.fillStyle = '#000'; ctx.fillText(t.s, sx(t.x) + 1.5, sy(t.y) - k * 40 + 1.5); ctx.fillStyle = t.c; ctx.fillText(t.s, sx(t.x), sy(t.y) - k * 40); }
  ctx.globalAlpha = 1;
  if (hero.tp) { const k = 1 - (hero.tp.end - T) / 3; ctx.fillStyle = 'rgba(0,0,0,.6)'; ctx.fillRect(W / 2 - 110, H * .62, 220, 16);
    ctx.fillStyle = '#6fb6ff'; ctx.fillRect(W / 2 - 110, H * .62, 220 * k, 16); ctx.fillStyle = '#fff'; ctx.font = '700 11px Rubik, system-ui, sans-serif'; ctx.textAlign = 'center'; ctx.fillText('Телепорт в баню...', W / 2, H * .62 + 12); }
  drawMinimap();
}

// Туман: тёмный слой с «дырками» видимости
let fogCv = null, fogCtx = null;
function drawFog() {
  if (!fogCv || fogCv.width !== cv.width || fogCv.height !== cv.height) { fogCv = document.createElement('canvas'); fogCv.width = cv.width; fogCv.height = cv.height; fogCtx = fogCv.getContext('2d'); }
  const f = fogCtx, p = cam.ppu;
  f.setTransform(DPR, 0, 0, DPR, 0, 0);
  f.globalCompositeOperation = 'source-over'; f.clearRect(0, 0, W, H);
  f.fillStyle = 'rgba(6,10,18,0.62)'; f.fillRect(0, 0, W, H);
  f.globalCompositeOperation = 'destination-out';
  for (const s of visionSources(playerTeam)) {
    const x = sx(s.x), y = sy(s.y), r = s.r * p;
    if (x < -r || y < -r || x > W + r || y > H + r) continue;
    const gr = f.createRadialGradient(x, y, r * 0.7, x, y, r);
    gr.addColorStop(0, 'rgba(0,0,0,1)'); gr.addColorStop(1, 'rgba(0,0,0,0)');
    f.fillStyle = gr; f.beginPath(); f.arc(x, y, r, 0, TAU); f.fill();
  }
  ctx.save(); ctx.setTransform(1, 0, 0, 1, 0, 0); ctx.drawImage(fogCv, 0, 0); ctx.restore();
}
function shadow(x, y, rx) { ctx.fillStyle = 'rgba(0,0,0,.28)'; ctx.beginPath(); ctx.ellipse(sx(x), sy(y), rx * cam.ppu, rx * cam.ppu * .4, 0, 0, TAU); ctx.fill(); }
function circ(x, y, r, c) { ctx.fillStyle = c; ctx.beginPath(); ctx.arc(x, y, r, 0, TAU); ctx.fill(); }

function drawDecor(d) {
  const p = cam.ppu, x = sx(d.x), y = sy(d.y), s = d.s;
  if (d.kind === 'rock') { shadow(d.x, d.y, s * .8); ctx.fillStyle = '#6f6b66'; ctx.beginPath(); ctx.ellipse(x, y - s * .25 * p, s * .8 * p, s * .5 * p, 0, 0, TAU); ctx.fill();
    ctx.fillStyle = '#8a8680'; ctx.beginPath(); ctx.ellipse(x - s * .2 * p, y - s * .4 * p, s * .4 * p, s * .22 * p, 0, 0, TAU); ctx.fill(); return; }
  shadow(d.x, d.y, s * 1.1);
  ctx.fillStyle = '#5a3a1e'; ctx.fillRect(x - .15 * s * p, y - 1.4 * s * p, .3 * s * p, 1.4 * s * p);
  if (d.kind === 'pine') {
    for (let i = 0; i < 3; i++) { const w = (1.5 - i * .35) * s * p, yy = y - (1.1 + i * .9) * s * p;
      ctx.fillStyle = i % 2 ? '#1f5a2a' : '#1a4d24'; ctx.beginPath(); ctx.moveTo(x - w, yy); ctx.lineTo(x + w, yy); ctx.lineTo(x, yy - 1.5 * s * p); ctx.fill(); }
  } else {
    circ(x, y - 2.3 * s * p, 1.25 * s * p, '#2f6e2c'); circ(x + .5 * s * p, y - 2.8 * s * p, .8 * s * p, '#3d8236'); circ(x - .4 * s * p, y - 2.6 * s * p, .55 * s * p, '#4a943f');
  }
}

function drawBath(b) {
  const p = cam.ppu, x = sx(b.x), y = sy(b.y);
  ctx.fillStyle = '#6b4424'; ctx.beginPath(); ctx.ellipse(x, y, (b.r + .5) * p, (b.r + .5) * p * .6, 0, 0, TAU); ctx.fill();
  const g = ctx.createRadialGradient(x - p, y - p * .5, 0, x, y, b.r * p); g.addColorStop(0, '#ffc23a'); g.addColorStop(1, '#d98a0a');
  ctx.fillStyle = g; ctx.beginPath(); ctx.ellipse(x, y, b.r * p, b.r * p * .6, 0, 0, TAU); ctx.fill();
  ctx.fillStyle = '#fff8e6';
  for (let i = 0; i < 14; i++) { const a = i / 14 * TAU; ctx.beginPath(); ctx.ellipse(x + Math.cos(a) * (b.r - .5) * p, y + Math.sin(a) * (b.r - .5) * p * .6, .7 * p, .3 * p, 0, 0, TAU); ctx.fill(); }
  for (const q of bubbles) if (Math.hypot(q.x - b.x, (q.y - b.y) / .55) < b.r + 1) { ctx.globalAlpha = Math.min(1, q.life); circ(sx(q.x), sy(q.y) - q.h * p, q.s * p, '#fffbe8'); }
  ctx.globalAlpha = 1;
  ctx.font = `800 ${Math.max(9, p * .5)}px Rubik, system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.fillStyle = '#fff2b0';
  ctx.fillText(b.team === hero.team ? '+ЗДОРОВЬЕ +МАНА' : 'ЧУЖАЯ БАНЯ', x, y + b.r * p * .6 + p * 1.2);
}
function drawHut(h, i) {
  const p = cam.ppu, x = sx(h.x), y = sy(h.y);
  shadow(h.x, h.y, 2.4);
  ctx.fillStyle = '#9a6332'; ctx.fillRect(x - 1.8 * p, y - 2.5 * p, 3.6 * p, 2.5 * p);
  ctx.fillStyle = '#7a4b22'; for (let k = 0; k < 5; k++) ctx.fillRect(x - 1.8 * p, y - (2.5 - k * .5) * p, 3.6 * p, .12 * p);
  ctx.fillStyle = '#4a2a14'; ctx.beginPath(); ctx.moveTo(x - 2.1 * p, y - 2.4 * p); ctx.lineTo(x + 2.1 * p, y - 2.4 * p); ctx.lineTo(x, y - 4.1 * p); ctx.fill();
  ctx.fillStyle = '#8a3a2a'; ctx.fillRect(x + .7 * p, y - 4 * p, .45 * p, 1 * p);
  ctx.fillStyle = '#f2d98c'; ctx.fillRect(x - 1.5 * p, y - 2.3 * p, 3 * p, .55 * p);
  ctx.fillStyle = '#5a3510'; ctx.font = `800 ${Math.max(7, p * .34)}px Rubik, system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.fillText('ПИВНАЯ БАНЯ', x, y - 1.9 * p);
  // кружка
  ctx.fillStyle = '#f0a51a'; ctx.fillRect(x + 2.2 * p, y - 1.8 * p, 1.1 * p, 1.6 * p);
  ctx.fillStyle = '#fff8e6'; ctx.beginPath(); ctx.ellipse(x + 2.75 * p, y - 1.85 * p, .7 * p, .35 * p, 0, 0, TAU); ctx.fill();
  ctx.strokeStyle = '#dfe9ff'; ctx.lineWidth = .18 * p; ctx.beginPath(); ctx.arc(x + 3.35 * p, y - 1 * p, .4 * p, -1.4, 1.4); ctx.stroke();
}

function drawEnt(e) {
  const p = cam.ppu, x = sx(e.x), y = sy(e.y);
  if (e.alive && glyphOn(e)) { const r = (e.kind === 'creep' ? .9 : e.kind === 'tower' ? 3 : 4) * p, k = .55 + .25 * Math.sin(T * 10);
    ctx.strokeStyle = `rgba(255,216,74,${k})`; ctx.lineWidth = Math.max(2, p * .15); ctx.beginPath(); ctx.ellipse(x, y - r * .6, r, r * 1.05, 0, 0, TAU); ctx.stroke(); }
  if (!e.alive) { ctx.globalAlpha = Math.max(0, 1 - (T - e.died) / .4); }
  if (e.kind === 'throne') drawThrone(e, x, y, p);
  else if (e.kind === 'tower') drawTower(e, x, y, p);
  else if (e.kind === 'hero') drawHero(e, x, y, p);
  else if (e.courier) drawCourier(e, x, y, p);
  else drawCreep(e, x, y, p);
  ctx.globalAlpha = 1;
}
function drawThrone(e, x, y, p) {
  shadow(e.x, e.y, 3.5);
  const c = TEAM_COL[e.team];
  ctx.fillStyle = TEAM_DARK[e.team]; ctx.fillRect(x - 3 * p, y - 4 * p, 6 * p, 4 * p);
  ctx.fillStyle = c; ctx.fillRect(x - 3 * p, y - 4.6 * p, 6 * p, .8 * p);
  const im = e.team === K ? IMG.cityK : IMG.cityM, ih = 5 * p, iw = ih * (im.naturalWidth / (im.naturalHeight || 1) || .75);
  ctx.fillStyle = c; ctx.fillRect(x - iw / 2 - .2 * p, y - 10.4 * p, iw + .4 * p, ih + .4 * p);
  if (im.complete) ctx.drawImage(im, x - iw / 2, y - 10.2 * p, iw, ih);
  ctx.font = `${Math.max(10, p * .9)}px "Russo One", sans-serif`; ctx.textAlign = 'center'; ctx.fillStyle = '#000'; ctx.fillText(TEAM_NAME[e.team].toUpperCase(), x + 2, y - 10.8 * p + 2);
  ctx.fillStyle = c; ctx.fillText(TEAM_NAME[e.team].toUpperCase(), x, y - 10.8 * p);
}
function drawTower(e, x, y, p) {
  shadow(e.x, e.y, 2.6);
  if (e.team === K) {
    ctx.fillStyle = '#ff9e26'; ctx.fillRect(x - 2 * p, y - 3 * p, 4 * p, 3 * p);
    ctx.fillStyle = '#b31f14'; ctx.fillRect(x - 2.2 * p, y - 3.3 * p, 4.4 * p, .35 * p);
    ctx.fillStyle = '#26394d'; ctx.fillRect(x - 1.7 * p, y - 1.9 * p, 1.1 * p, 1 * p); ctx.fillRect(x + .6 * p, y - 1.9 * p, 1.1 * p, 1 * p);
    ctx.fillStyle = '#d91a1a'; ctx.fillRect(x - 1.8 * p, y - 2.8 * p, 3.6 * p, .65 * p);
    ctx.fillStyle = '#fff'; ctx.font = `800 ${Math.max(6, p * .36)}px Rubik, system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.fillText('ЧИКЕН ПИЦЦА', x, y - 2.36 * p);
    ctx.fillStyle = '#d99447'; ctx.beginPath(); ctx.ellipse(x, y - 3.6 * p, 1.8 * p, .7 * p, 0, 0, TAU); ctx.fill();
    ctx.fillStyle = '#ffd84d'; ctx.beginPath(); ctx.ellipse(x, y - 3.65 * p, 1.55 * p, .58 * p, 0, 0, TAU); ctx.fill();
    for (const [a, b] of [[-.8, -.1], [.5, .2], [-.2, .25], [.9, -.2], [0, -.25]]) { ctx.fillStyle = '#b3261a'; ctx.beginPath(); ctx.ellipse(x + a * p, y - (3.65 - b) * p, .25 * p, .12 * p, 0, 0, TAU); ctx.fill(); }
    ctx.fillStyle = '#b8732a'; ctx.beginPath(); ctx.ellipse(x + 1 * p, y - 4.6 * p, .4 * p, .6 * p, 0, 0, TAU); ctx.fill();
    ctx.fillStyle = '#f5f0e6'; ctx.fillRect(x + .93 * p, y - 5.6 * p, .14 * p, .5 * p); circ(x + 1 * p, y - 5.65 * p, .17 * p, '#f5f0e6');
  } else {
    ctx.fillStyle = '#8c4526'; ctx.fillRect(x - 2 * p, y - 3 * p, 4 * p, 3 * p);
    ctx.fillStyle = '#f2d9a6'; ctx.fillRect(x - 2 * p, y - .5 * p, 4 * p, .5 * p);
    ctx.fillStyle = '#ff8c1a'; ctx.fillRect(x - 2.2 * p, y - 3.3 * p, 4.4 * p, .35 * p);
    ctx.fillStyle = '#26394d'; ctx.fillRect(x - 1.7 * p, y - 1.9 * p, 1.1 * p, 1 * p); ctx.fillRect(x + .6 * p, y - 1.9 * p, 1.1 * p, 1 * p);
    ctx.fillStyle = '#ff8c1a'; ctx.fillRect(x - 1.8 * p, y - 2.8 * p, 3.6 * p, .65 * p);
    ctx.fillStyle = '#5a2a0d'; ctx.font = `800 ${Math.max(6, p * .36)}px Rubik, system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.fillText('БУРГЕР КИНГ', x, y - 2.36 * p);
    const by = y - 3.4 * p;
    ctx.fillStyle = '#e09a45'; ctx.beginPath(); ctx.ellipse(x, by, 1.4 * p, .3 * p, 0, 0, TAU); ctx.fill();
    ctx.fillStyle = '#5a2e14'; ctx.beginPath(); ctx.ellipse(x, by - .35 * p, 1.5 * p, .28 * p, 0, 0, TAU); ctx.fill();
    ctx.fillStyle = '#ffd226'; ctx.fillRect(x - 1.3 * p, by - .6 * p, 2.6 * p, .14 * p);
    ctx.fillStyle = '#5cc23a'; ctx.beginPath(); ctx.ellipse(x, by - .62 * p, 1.55 * p, .14 * p, 0, 0, TAU); ctx.fill();
    ctx.fillStyle = '#e8a04a'; ctx.beginPath(); ctx.ellipse(x, by - .75 * p, 1.45 * p, .9 * p, 0, Math.PI, TAU); ctx.fill();
    ctx.fillStyle = '#fff6dc'; for (const [a, b] of [[-.6, -1.2], [0, -1.45], [.6, -1.15], [-.2, -1.05], [.3, -1.35]]) { ctx.beginPath(); ctx.ellipse(x + a * p, by + b * p, .09 * p, .05 * p, 0, 0, TAU); ctx.fill(); }
  }
}
function drawSausage(c, x, y, len, r, ang) {
  c.save(); c.translate(x, y); c.rotate(ang);
  c.fillStyle = '#a8442a'; c.beginPath(); c.ellipse(0, 0, len / 2 + r, r, 0, 0, TAU); c.fill();
  c.fillStyle = 'rgba(255,190,150,.45)'; c.beginPath(); c.ellipse(-len * .05, -r * .35, len * .35, r * .3, 0, 0, TAU); c.fill();
  c.fillStyle = '#d98c66'; c.beginPath(); c.arc(-len / 2 - r * .8, 0, r * .35, 0, TAU); c.arc(len / 2 + r * .8, 0, r * .35, 0, TAU); c.fill();
  c.restore();
}
function flame(c, x, y, s) {
  const t = T * 14;
  c.fillStyle = '#ff7a1a'; c.beginPath(); c.moveTo(x - s * .5, y); c.quadraticCurveTo(x - s * .4, y - s * 1.2, x + Math.sin(t) * s * .15, y - s * 1.9);
  c.quadraticCurveTo(x + s * .4, y - s * 1.1, x + s * .5, y); c.closePath(); c.fill();
  c.fillStyle = '#ffd84a'; c.beginPath(); c.ellipse(x, y - s * .45, s * .22, s * .5, 0, 0, TAU); c.fill();
}
// Саня Финд: светлые лохматые волосы, чёрный пуховик, оранжевые глаза, шампур с сосиской, горящая сосиска в руке
function ccirc(c, x, y, r, col) { c.fillStyle = col; c.beginPath(); c.arc(x, y, r, 0, TAU); c.fill(); }
function drawSanya(c, x, y, p, e) {
  const ch = e && e.channel, sp = ch ? 6 : 1;
  if (ch) { const g = c.createRadialGradient(x, y - 2 * p, 0, x, y - 2 * p, 3 * p); g.addColorStop(0, 'rgba(255,120,30,.45)'); g.addColorStop(1, 'rgba(255,120,30,0)');
    c.fillStyle = g; c.fillRect(x - 3 * p, y - 5 * p, 6 * p, 6 * p); }
  // сосиски вокруг (сзади)
  const n = Math.min(10, (e && e.sausage) || 0), orb = [];
  for (let k = 0; k < n; k++) { const a = T * sp + TAU * k / n, rr = ch ? .8 : 1.6; orb.push({ x: x + Math.cos(a) * rr * p, y: y - 2.2 * p + Math.sin(a) * rr * .45 * p + Math.sin(a * 3) * .1 * p, back: Math.sin(a) < 0, a }); }
  for (const o of orb) if (o.back) drawSausage(c, o.x, o.y, .38 * p, .1 * p, o.a + Math.PI / 2);
  // ноги и белые кроссовки
  c.fillStyle = '#202028'; c.fillRect(x - .55 * p, y - 1.25 * p, .45 * p, 1.15 * p); c.fillRect(x + .1 * p, y - 1.25 * p, .45 * p, 1.15 * p);
  c.fillStyle = '#efefef'; c.beginPath(); c.ellipse(x - .38 * p, y - .08 * p, .36 * p, .18 * p, 0, 0, TAU); c.ellipse(x + .38 * p, y - .08 * p, .36 * p, .18 * p, 0, 0, TAU); c.fill();
  // пуховик
  c.fillStyle = '#111114'; c.beginPath(); c.roundRect ? c.roundRect(x - .9 * p, y - 3.05 * p, 1.8 * p, 1.95 * p, .45 * p) : c.rect(x - .9 * p, y - 3.05 * p, 1.8 * p, 1.95 * p); c.fill();
  c.strokeStyle = '#2c2c34'; c.lineWidth = Math.max(1, p * .06);
  for (const yy of [2.55, 2.05, 1.55]) { c.beginPath(); c.moveTo(x - .85 * p, y - yy * p); c.quadraticCurveTo(x, y - (yy - .12) * p, x + .85 * p, y - yy * p); c.stroke(); }
  c.strokeStyle = '#8c8c94'; c.lineWidth = Math.max(1, p * .045); c.beginPath(); c.moveTo(x, y - 3 * p); c.lineTo(x, y - 1.15 * p); c.stroke();
  c.fillStyle = '#18181c'; c.beginPath(); c.ellipse(x, y - 3.05 * p, .75 * p, .25 * p, 0, 0, TAU); c.fill();   // воротник
  // правая рука вниз — шампур с сосиской
  c.save(); c.translate(x + .85 * p, y - 2.8 * p); c.rotate(-.25); c.fillStyle = '#111114'; c.fillRect(-.22 * p, 0, .44 * p, 1.35 * p); c.restore();
  ccirc(c, x + 1.25 * p, y - 1.4 * p, .24 * p, '#f2c8ad');
  c.strokeStyle = '#5a3418'; c.lineWidth = .14 * p; c.beginPath(); c.moveTo(x + 1.25 * p, y - .5 * p); c.lineTo(x + 1.25 * p, y - 2.2 * p); c.stroke();
  c.strokeStyle = '#c0c2c8'; c.lineWidth = .07 * p; c.beginPath(); c.moveTo(x + 1.15 * p, y - 2.2 * p); c.lineTo(x + 1.15 * p, y - 3.1 * p); c.moveTo(x + 1.35 * p, y - 2.2 * p); c.lineTo(x + 1.35 * p, y - 3.1 * p); c.stroke();
  drawSausage(c, x + 1.25 * p, y - 2.7 * p, .45 * p, .12 * p, 0);
  // левая рука вверх — горящая сосиска
  c.save(); c.translate(x - .85 * p, y - 2.8 * p); c.rotate(2.6); c.fillStyle = '#111114'; c.fillRect(-.22 * p, 0, .44 * p, 1.2 * p); c.restore();
  ccirc(c, x - 1.4 * p, y - 3.75 * p, .24 * p, '#f2c8ad');
  drawSausage(c, x - 1.4 * p, y - 4.15 * p, .35 * p, .11 * p, -1.35);
  flame(c, x - 1.38 * p, y - 4.45 * p, .32 * p);
  // шея и голова
  c.fillStyle = '#f2c8ad'; c.fillRect(x - .17 * p, y - 3.35 * p, .34 * p, .35 * p);
  c.beginPath(); c.ellipse(x, y - 3.85 * p, .55 * p, .64 * p, 0, 0, TAU); c.fill();
  // глаза светятся
  c.save(); c.shadowColor = '#ff8a1a'; c.shadowBlur = p * .5;
  ccirc(c, x - .2 * p, y - 3.85 * p, .09 * p, '#ff8c1a'); ccirc(c, x + .2 * p, y - 3.85 * p, .09 * p, '#ff8c1a'); c.restore();
  ccirc(c, x - .2 * p, y - 3.85 * p, .04 * p, '#fff2c0'); ccirc(c, x + .2 * p, y - 3.85 * p, .04 * p, '#fff2c0');
  c.strokeStyle = '#8a3c30'; c.lineWidth = Math.max(1, p * .05); c.beginPath(); c.arc(x, y - 3.5 * p, .16 * p, .2, Math.PI - .2); c.stroke();
  // светлые лохматые волосы
  c.fillStyle = '#d2a05e';
  c.beginPath(); c.ellipse(x, y - 4.25 * p, .6 * p, .38 * p, 0, Math.PI, TAU); c.fill();
  for (let k = 0; k < 13; k++) { const a = Math.PI + k / 12 * Math.PI, bx = x + Math.cos(a) * .55 * p, by = y - 4.2 * p + Math.sin(a) * .38 * p, len = (.35 + ((k * 37) % 7) / 18) * p, ta = a + (((k * 53) % 5) - 2) * .12;
    c.fillStyle = k % 3 ? '#d2a05e' : '#a87a40'; c.beginPath(); c.moveTo(bx - Math.sin(a) * .13 * p, by + Math.cos(a) * .13 * p); c.lineTo(bx + Math.cos(ta) * len, by + Math.sin(ta) * len); c.lineTo(bx + Math.sin(a) * .13 * p, by - Math.cos(a) * .13 * p); c.fill(); }
  c.fillStyle = '#c8945a'; for (let k = 0; k < 5; k++) { const fx0 = x - .4 * p + k * .2 * p; c.beginPath(); c.moveTo(fx0 - .1 * p, y - 4.25 * p); c.lineTo(fx0 + .03 * p, y - 3.95 * p); c.lineTo(fx0 + .12 * p, y - 4.25 * p); c.fill(); }
  // сосиски вокруг (спереди)
  for (const o of orb) if (!o.back) { drawSausage(c, o.x, o.y, .38 * p, .1 * p, o.a + Math.PI / 2); flame(c, o.x, o.y - .05 * p, .14 * p); }
}
const portraitCache = {};
function heroPortrait(def) {
  if (!def || def === HERO) return [IMG_SRC.faceL, IMG_SRC.faceR];
  if (def === MAX) return [typeof MAX_FACE !== 'undefined' ? MAX_FACE : ''];
  if (!portraitCache[def.id]) {
    const c = document.createElement('canvas'); c.width = 120; c.height = 120; const x = c.getContext('2d');
    x.fillStyle = '#3a1d12'; x.fillRect(0, 0, 120, 120);
    const g = x.createRadialGradient(60, 50, 5, 60, 50, 70); g.addColorStop(0, 'rgba(255,130,40,.55)'); g.addColorStop(1, 'rgba(255,130,40,0)'); x.fillStyle = g; x.fillRect(0, 0, 120, 120);
    drawSanya(x, 60, 225, 46, { sausage: 0 });
    portraitCache[def.id] = c.toDataURL();
  }
  return [portraitCache[def.id]];
}
function drawHero(e, x, y, p) {
  const c = TEAM_COL[e.team];
  shadow(e.x, e.y, 1.3);
  ctx.strokeStyle = c; ctx.lineWidth = Math.max(2, p * .12); ctx.beginPath(); ctx.ellipse(x, y, 1.15 * p, .45 * p, 0, 0, TAU); ctx.stroke();
  if (T < e.st.immune) { ctx.strokeStyle = '#ffd84a'; ctx.beginPath(); ctx.ellipse(x, y - 1.6 * p, 1.6 * p, 2.1 * p, 0, 0, TAU); ctx.stroke(); }
  if (e.def === SANYA) return drawSanya(ctx, x, y, p, e);
  if (e.def === MAX) return drawMax(ctx, x, y, p, e);
  // ноги и кроссовки
  ctx.fillStyle = '#111116'; ctx.fillRect(x - .7 * p, y - 1.1 * p, .55 * p, 1 * p); ctx.fillRect(x + .15 * p, y - 1.1 * p, .55 * p, 1 * p);
  ctx.fillStyle = '#eee'; ctx.fillRect(x - .72 * p, y - 1.05 * p, .06 * p, .9 * p); ctx.fillRect(x + .66 * p, y - 1.05 * p, .06 * p, .9 * p);
  ctx.fillStyle = '#d91a1a'; ctx.beginPath(); ctx.ellipse(x - .45 * p, y - .08 * p, .42 * p, .2 * p, 0, 0, TAU); ctx.fill();
  ctx.fillStyle = '#f4f4f4'; ctx.beginPath(); ctx.ellipse(x + .45 * p, y - .08 * p, .42 * p, .2 * p, 0, 0, TAU); ctx.fill();
  // туловище-олимпийка с пузом
  ctx.fillStyle = '#121216'; ctx.beginPath(); ctx.ellipse(x, y - 1.9 * p, 1.15 * p, 1.05 * p, 0, 0, TAU); ctx.fill();
  ctx.fillStyle = '#1b1b22'; ctx.beginPath(); ctx.ellipse(x, y - 1.6 * p, .8 * p, .7 * p, 0, 0, TAU); ctx.fill();
  ctx.strokeStyle = '#b8b8c2'; ctx.lineWidth = Math.max(1, p * .05); ctx.beginPath(); ctx.moveTo(x, y - 2.9 * p); ctx.lineTo(x, y - 1.2 * p); ctx.stroke();
  // руки с лампасами
  for (const s of [-1, 1]) { ctx.fillStyle = '#121216'; ctx.save(); ctx.translate(x + s * 1.15 * p, y - 2.1 * p); ctx.rotate(s * .25);
    ctx.fillRect(-.28 * p, 0, .56 * p, 1.3 * p); ctx.fillStyle = '#eee'; ctx.fillRect(s > 0 ? .18 * p : -.24 * p, 0, .06 * p, 1.3 * p); ctx.restore();
    circ(x + s * 1.5 * p, y - .75 * p, .3 * p, '#f2c3a3'); }
  // энергетик и дубина
  ctx.fillStyle = '#2fbf3a'; ctx.fillRect(x - 1.75 * p, y - 1.35 * p, .42 * p, .65 * p); ctx.fillStyle = '#ffe226'; ctx.fillRect(x - 1.58 * p, y - 1.25 * p, .08 * p, .4 * p);
  ctx.strokeStyle = '#6b4320'; ctx.lineWidth = .2 * p; ctx.beginPath(); ctx.moveTo(x + 1.5 * p, y - .7 * p); ctx.lineTo(x + 1.75 * p, y - 2.1 * p); ctx.stroke();
  circ(x + 1.8 * p, y - 2.4 * p, .5 * p, '#9a6232');
  ctx.fillStyle = '#555'; for (const a of [0, 1.3, 2.6, 3.9, 5.2]) ctx.fillRect(x + 1.8 * p + Math.cos(a) * .5 * p - .07 * p, y - 2.4 * p + Math.sin(a) * .5 * p - .07 * p, .14 * p, .14 * p);
  // цепь
  ctx.strokeStyle = '#ffcc33'; ctx.lineWidth = Math.max(1.5, p * .09); ctx.beginPath(); ctx.arc(x, y - 3.1 * p, .75 * p, .35, Math.PI - .35); ctx.stroke();
  ctx.fillStyle = '#ffcc33'; ctx.fillRect(x - .6 * p, y - 2.55 * p, 1.2 * p, .3 * p);
  if (p > 18) { ctx.fillStyle = '#3a2405'; ctx.font = `800 ${p * .15}px Rubik, system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.fillText('#ЗА КУРЧАТОВ УБЬЮ', x, y - 2.34 * p); }
  // две головы
  for (const [s, im] of [[-1, IMG.faceL], [1, IMG.faceR]]) {
    const hx = x + s * .55 * p, hy = y - 3.6 * p, r = .62 * p;
    circ(hx, hy, r, '#f2c3a3');
    if (im.complete) { ctx.save(); ctx.beginPath(); ctx.arc(hx, hy, r, 0, TAU); ctx.clip(); ctx.drawImage(im, hx - r, hy - r, r * 2, r * 2); ctx.restore(); }
    ctx.strokeStyle = 'rgba(0,0,0,.35)'; ctx.lineWidth = 1; ctx.beginPath(); ctx.arc(hx, hy, r, 0, TAU); ctx.stroke();
  }
}
function drawCreep(e, x, y, p) {
  if (e.boss) p *= 1.4;
  const c = e.neutral ? (e.boss ? '#3a3a44' : '#5c5c66') : TEAM_COL[e.team];
  shadow(e.x, e.y, .55);
  ctx.fillStyle = '#18181d'; ctx.fillRect(x - .3 * p, y - .55 * p, .22 * p, .5 * p); ctx.fillRect(x + .08 * p, y - .55 * p, .22 * p, .5 * p);
  ctx.fillStyle = c; ctx.beginPath(); ctx.ellipse(x, y - .95 * p, .45 * p, .5 * p, 0, 0, TAU); ctx.fill();
  circ(x, y - 1.6 * p, .27 * p, '#f2c6aa');
  ctx.fillStyle = TEAM_DARK[e.team]; ctx.beginPath(); ctx.ellipse(x, y - 1.72 * p, .3 * p, .14 * p, 0, Math.PI, TAU); ctx.fill(); ctx.fillRect(x, y - 1.75 * p, .38 * p, .07 * p);
  ctx.strokeStyle = '#6b4320'; ctx.lineWidth = Math.max(1, .09 * p); ctx.beginPath();
  if (e.ranged) { ctx.moveTo(x + .45 * p, y - .7 * p); ctx.lineTo(x + .45 * p, y - 1.05 * p); ctx.lineTo(x + .3 * p, y - 1.3 * p); ctx.moveTo(x + .45 * p, y - 1.05 * p); ctx.lineTo(x + .6 * p, y - 1.3 * p); }
  else { ctx.moveTo(x + .4 * p, y - .7 * p); ctx.lineTo(x + .75 * p, y - 1.6 * p); }
  ctx.stroke();
}
function drawProj(q) {
  const p = cam.ppu, x = sx(q.x), y = sy(q.y);
  if (q.look === 'pizza' || q.look === 'pizzaT') {
    const r = (q.look === 'pizza' ? .55 : .4) * p; ctx.save(); ctx.translate(x, y); ctx.rotate(q.spin);
    circ(0, 0, r, '#d99447'); circ(0, 0, r * .82, '#ffd84d');
    for (let i = 0; i < 4; i++) circ(Math.cos(i * 1.6) * r * .45, Math.sin(i * 1.6) * r * .45, r * .16, '#b3261a'); ctx.restore();
  } else if (q.look === 'sauce') { circ(x, y, .35 * p, '#e02a1a'); circ(x - .1 * p, y - .1 * p, .12 * p, '#ff7a5a'); }
  else if (q.look === 'burger') { ctx.fillStyle = '#e8a04a'; ctx.beginPath(); ctx.ellipse(x, y - .1 * p, .4 * p, .25 * p, 0, Math.PI, TAU); ctx.fill();
    ctx.fillStyle = '#5a2e14'; ctx.fillRect(x - .4 * p, y - .1 * p, .8 * p, .14 * p); ctx.fillStyle = '#e09a45'; ctx.fillRect(x - .36 * p, y + .04 * p, .72 * p, .1 * p); }
  else if (q.look === 'sausage') drawSausage(ctx, x, y, .5 * p, .12 * p, q.spin);
  else if (q.look === 'goldbullet' || q.look === 'goldbig') drawGoldBullet(q, x, y, p);
  else circ(x, y, .15 * p, '#8a7a6a');
}
function drawBar(e) {
  const p = cam.ppu, big = e.kind === 'tower' || e.kind === 'throne';
  const w = (e.kind === 'throne' ? 6 : e.kind === 'tower' ? 4 : e.kind === 'hero' ? 2.6 : 1.3) * p, h = Math.max(3, (big ? .3 : .18) * p);
  const top = { throne: 11.6, tower: 6.2, hero: 4.6, creep: 2.2 }[e.kind];
  const x = sx(e.x) - w / 2, y = sy(e.y) - top * p;
  ctx.fillStyle = '#000'; ctx.fillRect(x - 1, y - 1, w + 2, h + 2);
  ctx.fillStyle = e.invuln ? '#888' : e === hero ? '#3fd04f' : TEAM_COL[e.team]; ctx.fillRect(x, y, w * clamp(e.hp / e.maxHp, 0, 1), h);
  if (e.kind === 'hero' || big) {
    ctx.font = `700 ${Math.max(9, p * .4)}px Rubik, system-ui, sans-serif`; ctx.textAlign = 'center'; ctx.fillStyle = e.kind === 'hero' && hero && enemies(e, hero) ? '#ff8a80' : '#fff';
    ctx.fillText(e.name + (e.invuln ? ' (неуязвим)' : ''), sx(e.x), y - 3);
  }
  const st = []; if (stunned(e)) st.push('оглушён'); if (e.st.dots.length) st.push('горит'); if (e.st.slows.length) st.push('замедлен');
  if (st.length) { ctx.font = `700 ${Math.max(8, p * .32)}px Rubik, system-ui, sans-serif`; ctx.fillStyle = '#ffd84a'; ctx.textAlign = 'center'; ctx.fillText(st.join(' '), sx(e.x), y + h + Math.max(9, p * .4)); }
}

// ---------- Мини-карта ----------
const mm = $('minimap'), mctx = mm.getContext('2d');
let mmBg = null;
function buildMinimapBg() {
  const c = document.createElement('canvas'); c.width = c.height = 190; const g = c.getContext('2d');
  g.fillStyle = '#243d1d'; g.fillRect(0, 0, 190, 190);
  for (const d of decor) { g.fillStyle = d.kind === 'rock' ? '#555' : '#173016'; const [x, y] = mmXY(d.x, d.y); g.fillRect(x - 1.5, y - 1.5, 3, 3); }
  g.save(); g.translate(95, 95); g.rotate(-Math.PI / 4); g.fillStyle = '#8a7146'; g.fillRect(-140, -6.5, 280, 13); g.restore();
  g.fillStyle = '#8a7146';
  for (const [x1, y1, x2, y2] of SIDE_RECTS) { const [a, b] = mmXY(x1, y1), [c, d] = mmXY(x2, y2); g.fillRect(a, b, c - a, d - b); }
  for (const c of CAMPS) { const [x, y] = mmXY(c.x, c.y); g.fillStyle = '#5a3d12'; g.beginPath(); g.arc(x, y, 3.5, 0, TAU); g.fill(); }
  for (const [x, y, t] of [[-45, 45, K], [45, -45, M]]) { const [a, b] = mmXY(x, y); const gr = g.createRadialGradient(a, b, 0, a, b, 34);
    gr.addColorStop(0, TEAM_COL[t] + '88'); gr.addColorStop(1, TEAM_COL[t] + '00'); g.fillStyle = gr; g.fillRect(a - 34, b - 34, 68, 68); }
  for (const b of BATHS) { const [x, y] = mmXY(b.x, b.y); g.fillStyle = '#e8a018'; g.beginPath(); g.arc(x, y, 6, 0, TAU); g.fill(); }
  mmBg = c;
}
const mmXY = (x, y) => [(x + 60) / 120 * 190, (y + 60) / 120 * 190];
function drawMinimap() {
  if (!mmBg) return;
  mctx.drawImage(mmBg, 0, 0);
  // туман на мини-карте
  mctx.save(); mctx.fillStyle = 'rgba(0,0,0,.45)'; mctx.fillRect(0, 0, 190, 190);
  mctx.globalCompositeOperation = 'destination-out';
  for (const s of visionSources(playerTeam)) { const [x, y] = mmXY(s.x, s.y); mctx.beginPath(); mctx.arc(x, y, s.r / 120 * 190, 0, TAU); mctx.fill(); }
  mctx.restore();
  mctx.globalCompositeOperation = 'destination-over'; mctx.drawImage(mmBg, 0, 0); mctx.globalCompositeOperation = 'source-over';
  for (const e of ents) { if (!e.alive || !canSee(playerTeam, e)) continue; const [x, y] = mmXY(e.x, e.y); mctx.fillStyle = TEAM_COL[e.team];
    if (e.kind === 'tower' || e.kind === 'throne') { const s = e.kind === 'throne' ? 12 : 8; mctx.fillStyle = '#000'; mctx.fillRect(x - s / 2 - 1, y - s / 2 - 1, s + 2, s + 2); mctx.fillStyle = e.invuln ? '#999' : TEAM_COL[e.team]; mctx.fillRect(x - s / 2, y - s / 2, s, s); }
    else if (e.kind === 'hero') { mctx.fillStyle = '#fff'; mctx.beginPath(); mctx.arc(x, y, 6.5, 0, TAU); mctx.fill(); mctx.fillStyle = TEAM_COL[e.team]; mctx.beginPath(); mctx.arc(x, y, 4.5, 0, TAU); mctx.fill(); }
    else { mctx.beginPath(); mctx.arc(x, y, 2.4, 0, TAU); mctx.fill(); } }
  const a = toWorld(0, 0), b = toWorld(W, H), [x1, y1] = mmXY(a.x, a.y), [x2, y2] = mmXY(b.x, b.y);
  mctx.strokeStyle = 'rgba(255,255,255,.85)'; mctx.lineWidth = 1.5; mctx.strokeRect(x1, y1, x2 - x1, y2 - y1);
}
mm.addEventListener('pointerdown', ev => {
  ev.preventDefault(); ev.stopPropagation(); if (paused || over) return;
  const r = mm.getBoundingClientRect(), x = (ev.clientX - r.left) / r.width * 120 - 60, y = (ev.clientY - r.top) / r.height * 120 - 60;
  if (tpPick) { tpPick = false; ticketAt(x, y); return; }
  if (hero.alive) { hero.order = { type: 'move', x, y }; pending = null; cam.follow = true; }
  else { cam.x = x; cam.y = y; cam.follow = false; }
});

// ===================== ВВОД =====================
const pointers = new Map(); let pinch0 = 0, ppu0 = 26, tapStart = null, dragged = false;
cv.addEventListener('contextmenu', e => e.preventDefault());
cv.addEventListener('pointerdown', ev => {
  cv.setPointerCapture(ev.pointerId); pointers.set(ev.pointerId, { x: ev.clientX, y: ev.clientY });
  hideTip();
  if (pointers.size === 2) { const [a, b] = [...pointers.values()]; pinch0 = Math.hypot(a.x - b.x, a.y - b.y); ppu0 = cam.ppu; dragged = true; }
  else { tapStart = { x: ev.clientX, y: ev.clientY, t: performance.now(), btn: ev.button, lx: ev.clientX, ly: ev.clientY }; dragged = false; }
});
cv.addEventListener('pointermove', ev => {
  if (!pointers.has(ev.pointerId)) return;
  pointers.set(ev.pointerId, { x: ev.clientX, y: ev.clientY });
  if (pointers.size >= 2) { const [a, b] = [...pointers.values()]; const d = Math.hypot(a.x - b.x, a.y - b.y);
    if (pinch0) cam.ppu = clamp(ppu0 * d / pinch0, 12, 60); return; }
  if (tapStart) {
    if (Math.hypot(ev.clientX - tapStart.x, ev.clientY - tapStart.y) > 14) dragged = true;
    if (dragged && ev.pointerType !== 'mouse') { cam.x -= (ev.clientX - tapStart.lx) / cam.ppu; cam.y -= (ev.clientY - tapStart.ly) / cam.ppu; cam.follow = false; }
    tapStart.lx = ev.clientX; tapStart.ly = ev.clientY;
  }
});
const endPointer = ev => {
  pointers.delete(ev.pointerId);
  if (pointers.size < 2) pinch0 = 0;
  if (tapStart && !dragged && pointers.size === 0 && performance.now() - tapStart.t < 600) tap(ev.clientX, ev.clientY, tapStart.btn);
  if (pointers.size === 0) tapStart = null;
};
cv.addEventListener('pointerup', endPointer); cv.addEventListener('pointercancel', ev => { pointers.delete(ev.pointerId); tapStart = null; });
cv.addEventListener('wheel', ev => { ev.preventDefault(); cam.ppu = clamp(cam.ppu * (ev.deltaY < 0 ? 1.1 : .9), 12, 60); }, { passive: false });

function pick(w) {
  let best = null, bd = 1e9;
  for (const e of ents) { if (!e.alive || e === hero || !canSee(playerTeam, e)) continue;
    const cy = e.y - (e.kind === 'throne' ? 2 : e.kind === 'tower' ? 1.5 : e.kind === 'hero' ? 1.8 : .8);
    const d = Math.min(Math.hypot(w.x - e.x, w.y - e.y), Math.hypot(w.x - e.x, w.y - cy)), lim = e.r + (e.kind === 'creep' ? 1 : 1.2);
    if (d < lim && d < bd) { bd = d; best = e; } }
  if (!best && hero.alive && Math.hypot(w.x - hero.x, w.y - (hero.y - 1.8)) < 2.2) best = hero;
  return best;
}
function tap(px, py, btn) {
  if (paused || over) return;
  const w = toWorld(px, py), u = pick(w);
  if (tpPick) { tpPick = false; ticketAt(w.x, w.y); return; }
  if (targeting >= 0) {
    const i = targeting; targeting = -1;
    if (AB(hero)[i].target === 'point') beginCast(i, { x: w.x, y: w.y, r: 0, point: true });
    else if (validTarget(i, u)) beginCast(i, u); else say('Неверная цель');
    return;
  }
  if (!hero.alive) return;
  if (hero.channel && hero.channel.kind === 'aim') { hero.channel = null; say('Прицел сброшен'); }
  pending = null; hero.tp = null;
  if (u && u !== hero && enemies(hero, u)) { hero.order = { type: 'attack', t: u }; ring(u.x, u.y, '#ff4a3a'); }
  else if (u && u.kind === 'creep' && !u.courier && !u.neutral && u.team === hero.team && u.hp < u.maxHp * .5) { hero.order = { type: 'attack', t: u }; ring(u.x, u.y, '#ffb03a'); }
  else { hero.order = { type: 'move', x: w.x, y: w.y }; ring(w.x, w.y, '#7dff8a'); }
  cam.follow = true;
}
function ring(x, y, c) { for (let i = 0; i < 6; i++) parts.push({ x: x + Math.cos(i) * .6, y: y + Math.sin(i) * .3, z: 0, vx: Math.cos(i) * 2, vy: Math.sin(i), vz: 2, life: .35, max: .35, s: .18, c }); }

document.addEventListener('keyup', ev => { if (ev.code === 'Backquote') showScore(false); });
document.addEventListener('keydown', ev => {
  if ($('menu').hidden === false || lb) return;
  const k = ev.key.toLowerCase(), map = { q: 0, 'й': 0, w: 1, 'ц': 1, e: 2, 'у': 2, r: 3, 'к': 3 };
  if (k === 'escape') { if (!$('shop').hidden) closeShop(); else togglePause(); return; }
  if (paused || over) return;
  if (k in map) { if (ev.ctrlKey) { ev.preventDefault(); learn(map[k]); } else pressAbility(map[k]); }
  if (k >= '1' && k <= '6') useItem(+k - 1);
  if (k === 'b' || k === 'и') toggleShop();
  if (k === 'g' || k === 'п') { useGlyph(playerTeam); refreshHud(true); }
  if (k === 't' || k === 'е') pressTicket();
  if (k === 'f' || k === 'а') courierDeliver();
  if (ev.code === 'Backquote') showScore(true);
  if (k === 's' || k === 'ы') { hero.order = null; pending = null; }
  if (k === ' ') { cam.follow = true; }
});

// ===================== HUD =====================
const abilBox = $('abils'), itemBox = $('items');
function buildAbilHud() {
  abilBox.innerHTML = '';
  AB(hero).forEach((a, i) => {
  const b = document.createElement('button'); b.className = 'abil'; const ic = abilIcon(a);
  b.style.background = ic ? `center/cover url(${ic})` : `linear-gradient(160deg, ${a.color}, ${a.color}66)`; if (ic) b.classList.add('pic');
  b.innerHTML = `<span class="key">${a.key}</span><span class="nm">${a.short || a.name}</span><span class="mc"></span><span class="chg"></span><div class="pips">${'<i></i>'.repeat(a.max)}</div><div class="cdv" hidden></div>`;
  b.addEventListener('pointerdown', ev => { ev.preventDefault(); ev.stopPropagation(); if (!hero.abil[i].lvl && canLearn(i)) { learn(i); showAbilTip(i); } else pressAbility(i); refreshHud(true); });
  const plus = document.createElement('button'); plus.className = 'plus'; plus.textContent = '+'; plus.hidden = true;
  plus.addEventListener('pointerdown', ev => { ev.preventDefault(); ev.stopPropagation(); learn(i); showAbilTip(i); });
  const w = document.createElement('div'); w.className = 'aw'; w.appendChild(b); w.appendChild(plus); abilBox.appendChild(w);
  });
  const pf = heroPortrait(hero.def);
  $('pfL').src = pf[0]; $('pfR').src = pf[1] || ''; $('pfR').hidden = !pf[1];
  $('pfL').classList.toggle('solo', !pf[1]);
  $('heroName').textContent = hero.def.name.toUpperCase();
}
for (let i = 0; i < 9; i++) {
  const s = document.createElement('button'); s.className = 'slot'; if (i > 5) s.style.opacity = .55;
  let lp = null, longDone = false;
  s.addEventListener('pointerdown', ev => { ev.preventDefault(); ev.stopPropagation(); longDone = false;
    lp = setTimeout(() => { longDone = true; slotInfo(i); }, 450); });
  s.addEventListener('pointerup', ev => { ev.stopPropagation(); clearTimeout(lp); if (!longDone) slotTap(i); });
  s.addEventListener('pointerleave', () => clearTimeout(lp));
  s.addEventListener('contextmenu', ev => { ev.preventDefault(); slotInfo(i); });
  itemBox.appendChild(s);
}
function slotTap(i) {
  if (selSlot >= 0) { const a = hero.inv[selSlot]; hero.inv[selSlot] = hero.inv[i]; hero.inv[i] = a; selSlot = -1; recalc(hero, true); hideTip(); refreshHud(true); return; }
  const it = hero.inv[i];
  if (it && it.d.act && i < 6) { useItem(i); return; }
  slotInfo(i);
}
// Долгое нажатие (или ПКМ) — подробности, переставить, продать
function slotInfo(i) {
  const it = hero.inv[i];
  if (!it) { showTip(`<h4>${i < 6 ? 'Пустой слот' : 'Рюкзак'}</h4>${i < 6 ? 'Покупай предметы в магазине у своей бани.' : 'Здесь предметы лежат, но не работают.'}`); return; }
  showTip(`<h4>${it.d.name}</h4>${describe(it.d)}<div style="color:#bba;margin-top:4px">${it.d.act && i < 6 ? 'Тап — применить. ' : ''}Долгое нажатие — это меню.</div><div style="display:flex;gap:6px;margin-top:8px;flex-wrap:wrap">
    <button class="shopbtn" id="tipMove">Переставить</button>${nearShop() ? `<button class="shopbtn" id="tipSell">Продать за ${Math.floor(totalCost(it.d) / 2)}</button>` : ''}</div>`, true);
  $('tipMove').onclick = () => { selSlot = i; hideTip(); toast('Теперь нажми слот, куда переставить'); refreshHud(true); };
  if ($('tipSell')) $('tipSell').onclick = () => { sell(i); hideTip(); };
}
let tipTimer;
function showTip(html, interactive) { const t = $('tip'); t.innerHTML = html; t.hidden = false; t.style.pointerEvents = interactive ? 'auto' : 'none';
  clearTimeout(tipTimer); tipTimer = setTimeout(hideTip, interactive ? 6000 : 3200); }
function hideTip() { $('tip').hidden = true; }
function showAbilTip(i) {
  const a = AB(hero)[i], s = hero.abil[i];
  const args = a.v.map(v => joinLv(v, s.lvl));
  const cost = a.target === 'passive' ? 'Пассивная' : `Мана ${joinLv(a.mana, s.lvl)} · ` + (a.charges ? `${a.charges} заряда, новый — раз в ${a.chargeCd} сек.` : `перезарядка ${joinLv(a.cd, s.lvl)} сек.`) + (a.target === 'none' ? '' : ` · дальность ${a.range}`);
  const need = a.ult ? (s.lvl + 1) * 6 : s.lvl * 2 + 1;
  const req = s.lvl >= a.max ? 'Максимальный уровень' : hero.lvl >= need ? (hero.points ? 'Можно прокачать — жми «+»' : 'Нет очков навыков') : `Следующий уровень — с ${need}-го уровня героя`;
  showTip(`<h4>${a.name} [${a.key}] · ур. ${s.lvl}/${a.max}</h4><div class="cost">${cost}</div>${a.desc.replace(/\{(\d)\}/g, (_, n) => args[n])}<div style="color:#bba;margin-top:4px">${req}</div>`);
}

let hudT = 0;
function refreshHud(force) {
  if (!hero) return;
  hudT += 1; if (!force && hudT % 6) return;
  $('hpBar').style.width = (hero.hp / hero.maxHp * 100) + '%'; $('hpTxt').textContent = `${Math.ceil(hero.hp)} / ${Math.ceil(hero.maxHp)}   +${hero.regen.toFixed(1)}`;
  $('mpBar').style.width = (hero.mp / hero.maxMp * 100) + '%'; $('mpTxt').textContent = `${Math.ceil(hero.mp)} / ${Math.ceil(hero.maxMp)}   +${hero.mpr.toFixed(1)}`;
  $('xpBar').style.width = (hero.lvl >= 18 ? 100 : hero.xp / xpNeed() * 100) + '%';
  $('lvl').textContent = hero.lvl; $('gold').textContent = hero.gold;
  const dm = $('deadMsg'); dm.hidden = hero.alive; if (!hero.alive) dm.textContent = Math.ceil(hero.respawn - T);
  $('stats').innerHTML = `<span>Урон</span><b>${Math.round(hero.dmg)}</b><span>Скор. атаки</span><b>${Math.round(hero.as)}</b>` +
    `<span>Броня</span><b>${hero.armor.toFixed(1)}</b><span>Скорость</span><b>${Math.round(speedOf(hero) * 100)}</b>` +
    `<span style="color:#ff7a66">СИЛ ${Math.round(hero.str)}</span><b style="color:#6fe07a">ЛОВ ${Math.round(hero.agi)}</b>`;
  { const gl = glyph[playerTeam], b = $('glyph'), c = b.querySelector('.gcd'), left = gl.ready - T;
    b.classList.toggle('on', T < gl.end); c.hidden = !(left > 0 && T >= gl.end); if (left > 0) c.textContent = left >= 60 ? Math.floor(left / 60) + ':' + String(Math.floor(left % 60)).padStart(2, '0') : Math.ceil(left); }
  $('points').textContent = (hero.def && hero.def.perSausage ? `Сосиски: ${hero.sausage || 0}/${hero.def.maxSausage}   ` : '') + (hero.points && AB(hero).some((_, i) => canLearn(i)) ? `Очки навыков: ${hero.points}` : '');
  [...abilBox.children].forEach((w, i) => {
    const b = w.querySelector('.abil'), a = AB(hero)[i], s = hero.abil[i];
    b.classList.toggle('locked', !s.lvl); b.classList.toggle('passive', a.target === 'passive'); b.classList.toggle('targeting', targeting === i);
    b.classList.toggle('nomana', s.lvl > 0 && a.target !== 'passive' && hero.mp < at(a.mana, s.lvl));
    b.querySelector('.mc').textContent = a.target === 'passive' || !s.lvl ? '' : at(a.mana, s.lvl);
    [...b.querySelectorAll('.pips i')].forEach((p, k) => p.classList.toggle('on', k < s.lvl));
    const cd = b.querySelector('.cdv'); cd.hidden = !(s.cd > 0); if (s.cd > 0) cd.textContent = Math.ceil(s.cd);
    b.querySelector('.chg').textContent = a.charges && s.lvl ? s.ch : '';
    w.querySelector('.plus').hidden = !canLearn(i);
  });
  [...itemBox.children].forEach((s, i) => {
    const it = hero.inv[i]; s.classList.toggle('sel', selSlot === i);
    const pic = it && ITEM_IMG[it.d.id];
    s.style.background = pic ? `center/cover url(${pic})` : it ? `linear-gradient(160deg, ${it.d.c}, ${it.d.c}55)` : '';
    s.innerHTML = (it && !pic ? `<span>${it.d.name}</span>` : '') + (i < 6 ? `<span class="n">${i + 1}</span>` : '') + (it && it.cd > 0 ? `<div class="cdv">${Math.ceil(it.cd)}</div>` : '');
  });
  // верхняя панель: 5 на 5
  const side = t => heroes.filter(h => h.team === t).map((h, k) => {
    const dead = !h.alive, left = Math.ceil(h.respawn - T), seen = canSee(playerTeam, h), ally = t === playerTeam, u = ultOf(h);
    const hpb = !dead && (ally || seen) ? `<span class="hpb"><i style="width:${Math.round(h.hp / h.maxHp * 100)}%"></i></span>` : '';
    const ult = ally && !dead && u && u.lvl ? `<span class="ult${u.cd > 0 ? ' cd' : ''}"></span>` : '';
    return `<div class="tbh${dead ? ' dead' : ''}${h === hero ? ' me' : ''}${!dead && !seen ? ' fog' : ''}" data-hi="${heroes.indexOf(h)}" style="--c:${TEAM_COL[t]};--sc:${SLOT_COL[(t === K ? 0 : 5) + k]}">${faces(h.def)}${dead ? `<span class="tm">${left}</span>` : `<span class="lv">${h.lvl}</span>`}${hpb}${ult}</div>`; }).join('');
  $('tbL').innerHTML = side(K); $('tbR').innerHTML = side(M);
  $('scL').textContent = teamKills[K]; $('scL').style.color = TEAM_COL[K];
  $('scR').textContent = teamKills[M]; $('scR').style.color = TEAM_COL[M];
  const m = Math.floor(T / 60), sct = Math.floor(T % 60);
  $('clock').innerHTML = `<span class="sun">${Math.floor(T / 300) % 2 ? '☾' : '☀'}</span> ${m}:${String(sct).padStart(2, '0')}`; $('wave').textContent = Math.max(0, Math.ceil(nextWave - T));
  $('towK').textContent = ents.filter(e => e.kind === 'tower' && e.team === K && e.alive).length;
  $('towM').textContent = ents.filter(e => e.kind === 'tower' && e.team === M && e.alive).length;
  if (!$('shop').hidden && hudT % 30 === 0) renderShop();
  // мои У/С/П и добитые / денаи
  $('myStats').innerHTML = `У/С/П <b><span style="color:#7dff8a">${hero.kills}</span> / <span style="color:#ff7a6e">${hero.deaths}</span> / <span style="color:#9db7ff">${hero.assists}</span></b><br>Добито / ден. <b><span style="color:#ffd84a">${hero.lh}</span> / <span style="color:#ff9a6e">${hero.dn}</span></b>`;
  // электричка
  { const b = $('tpBtn'); if (!b.style.backgroundImage && ITEM_IMG.ticket) b.style.backgroundImage = `url(${ITEM_IMG.ticket})`;
    $('tpN').textContent = hero.tpN; b.classList.toggle('on', tpPick); b.classList.toggle('empty', hero.tpN <= 0);
    const c = $('tpCd'); c.hidden = !(hero.tpCd > 0); if (hero.tpCd > 0) c.textContent = Math.ceil(hero.tpCd); }
  // курьер и тайник
  if (courier) { const b = $('courBtn'), im = $('courImg'); if (!im.src && typeof COURIER_PIC !== 'undefined') im.src = COURIER_PIC;
    b.classList.toggle('dead', !courier.alive); const c = $('courCd'); c.hidden = courier.alive; if (!courier.alive) c.textContent = Math.ceil(courier.respawn - T);
    const n = courier.carry.length + hero.stash.length; $('courN').textContent = n ? n : ''; }
  { const st = $('stash'); st.hidden = !hero.stash.length;
    if (hero.stash.length) st.innerHTML = `<div class="h">ТАЙНИК — F / курьер привезёт</div><div class="row">${hero.stash.map(d => { const pic = ITEM_IMG[d.id]; return `<i style="${pic ? `background-image:url(${pic})` : `background:${d.c}`}">${pic ? '' : d.name}</i>`; }).join('')}</div>`; }
  // счёт и карточка героя
  const so = scoreHold || scoreTap; $('scoreOv').hidden = !so; $('scoreBtn').classList.toggle('on', so);
  if (so && (force || hudT % 12 === 0)) renderScore();
  $('heroCard').hidden = !inspect || so;
  if (inspect && !so && (force || hudT % 12 === 0)) renderInspect();
}
$('tbL').addEventListener('pointerdown', ev => { const el = ev.target.closest('.tbh'); if (el) { ev.stopPropagation(); openInspect(heroes[+el.dataset.hi]); } });
$('tbR').addEventListener('pointerdown', ev => { const el = ev.target.closest('.tbh'); if (el) { ev.stopPropagation(); openInspect(heroes[+el.dataset.hi]); } });
$('heroCard').addEventListener('pointerdown', ev => { ev.stopPropagation(); if (ev.target.closest('[data-x]')) { inspect = null; refreshHud(true); } });
$('scoreOv').addEventListener('pointerdown', ev => { ev.stopPropagation(); const r = ev.target.closest('.row'); if (r) { scoreTap = false; scoreHold = false; inspect = heroes[+r.dataset.hi]; refreshHud(true); } });
$('scoreBtn').addEventListener('pointerdown', ev => { ev.preventDefault(); ev.stopPropagation(); scoreTap = !scoreTap; if (scoreTap) inspect = null; refreshHud(true); });
$('tpBtn').addEventListener('pointerdown', ev => { ev.preventDefault(); ev.stopPropagation(); pressTicket(); });
$('courBtn').addEventListener('pointerdown', ev => { ev.preventDefault(); ev.stopPropagation(); courierDeliver(); });

$('portrait').addEventListener('pointerdown', ev => { ev.stopPropagation(); cam.follow = true; });

// ===================== МАГАЗИН =====================
let shopTab = 0, shopSel = ITEM.shawarma;
function toggleShop() { if ($('shop').hidden) openShop(); else closeShop(); }
function openShop() { $('shop').hidden = false; renderShop(); }
function closeShop() { $('shop').hidden = true; }
function iconHtml(d) { const im = ITEM_IMG[d.id]; return im ? `<div class="icon pic" style="background-image:url(${im})"><span>${d.name}</span></div>` : `<div class="icon" style="--c:${d.c};background:linear-gradient(160deg,${d.c}aa,${d.c}33)"><span>${d.name}</span></div>`; }
function cardHtml(d, withPrice) {
  const own = hero.inv.some(x => x && x.d.id === d.id), pc = planCost(d).cost;
  return `<div class="card${shopSel === d ? ' sel' : ''}${own ? ' own' : ''}" data-id="${d.id}">${iconHtml(d)}${withPrice ? `<div class="price${hero.gold < pc ? ' poor' : ''}">${pc}</div>` : ''}</div>`;
}
function renderShop() {
  if ($('shop').hidden) return;
  $('shopGold').textContent = 'Золото: ' + hero.gold;
  const near = nearShop(); const n = $('shopNote');
  n.textContent = near ? 'Ты у бани — можно покупать. Тап — выбрать, двойной тап — купить.' : 'Покупать можно только у своей пивной бани (или пока ждёшь воскрешения).';
  n.style.color = near ? '#7dff8a' : '#ff8a80';
  $('tabs').innerHTML = CATS.map((c, i) => `<button class="tab${i === shopTab ? ' on' : ''}" data-t="${i}">${c}</button>`).join('');
  $('grid').innerHTML = ITEMS.filter(d => d.cat === shopTab).map(d => cardHtml(d, true)).join('');
  const d = shopSel, tc = totalCost(d), pc = planCost(d).cost;
  let html = `<div class="det-head">${iconHtml(d)}<div><h4>${d.name}</h4><div class="p">Цена: ${tc}</div>${pc < tc ? `<div class="p pay">Тебе доплатить: ${pc}</div>` : ''}</div></div>`;
  html += `<div class="st">${describe(d)}</div><div class="lore">${d.lore}</div>`;
  if (d.parts.length) {
    html += `<h5>СОБИРАЕТСЯ ИЗ:</h5><div class="tree"><div class="top">${cardHtml(d, false)}</div><div class="stem"></div><div class="row">`;
    for (const pid of d.parts) { const p = ITEM[pid], own = hero.inv.some(x => x && x.d.id === pid);
      html += `<div class="leaf"><div class="stem"></div><div style="position:relative">${cardHtml(p, false)}${own ? '<div class="has">есть</div>' : ''}</div></div>`; }
    if (d.cost > 0) html += `<div class="leaf"><div class="stem"></div><div class="recipe">РЕЦЕПТ<b>${d.cost}</b></div></div>`;
    html += `</div></div>`;
  }
  const ui = usedIn(d.id);
  if (ui.length) html += `<h5>ИСПОЛЬЗУЕТСЯ В:</h5><div class="used">${ui.map(x => cardHtml(x, false)).join('')}</div>`;
  $('det').innerHTML = html;
  const bb = $('buyBtn'); bb.textContent = `КУПИТЬ — ${pc}`; bb.disabled = !((near || d.id === 'scooter') && hero.gold >= pc);
  if (d.id === 'scooter' && hero.courier && hero.courier.up) { bb.textContent = 'КУРЬЕР УЖЕ НА САМОКАТЕ'; bb.disabled = true; }
}
let lastCardTap = { id: null, t: 0 };
$('shop').addEventListener('pointerdown', ev => ev.stopPropagation());
$('shop').addEventListener('click', ev => {
  const tab = ev.target.closest('.tab'); if (tab) { shopTab = +tab.dataset.t; renderShop(); return; }
  const card = ev.target.closest('.card');
  if (card) { const d = ITEM[card.dataset.id]; const now = performance.now();
    if (lastCardTap.id === d.id && now - lastCardTap.t < 400) buy(d); lastCardTap = { id: d.id, t: now };
    shopSel = d; renderShop(); }
});
$('shop').addEventListener('contextmenu', ev => { const card = ev.target.closest('.card'); if (card) { ev.preventDefault(); shopSel = ITEM[card.dataset.id]; buy(shopSel); } });
$('buyBtn').onclick = () => buy(shopSel);
$('shopClose').onclick = () => { if (lb && lb.phase === 'buy') lobbyNext(false); else closeShop(); };
$('shopBtn').addEventListener('pointerdown', ev => { ev.stopPropagation(); ev.preventDefault(); toggleShop(); });

// ===================== ЭКРАНЫ =====================
function togglePause() { if (over || $('menu').hidden === false) return; paused = !paused; $('pauseOv').hidden = !paused; }
$('pauseBtn').addEventListener('pointerdown', ev => { ev.stopPropagation(); togglePause(); });
$('glyph').addEventListener('pointerdown', ev => { ev.stopPropagation(); ev.preventDefault(); if (!paused && !over) { useGlyph(playerTeam); refreshHud(true); } });
$('resume').onclick = togglePause;
const toMenu = () => { stopSearch(); mmTab('home'); if (lb) clearInterval(lb.iv); lb = null; $('lobby').hidden = true; $('buyBar').hidden = true; paused = true; over = false; $('pauseOv').hidden = true; $('endOv').hidden = true; $('hud').hidden = true; closeShop(); $('menu').hidden = false; };
$('toMenu').onclick = toMenu; $('toMenu2').onclick = toMenu;
$('again').onclick = () => { $('endOv').hidden = true; findMatch(); };
// ---------- Главное меню ----------
const SETTINGS = { diff: 'normal', aim: '1', sound: '1' };
try { Object.assign(SETTINGS, JSON.parse(localStorage.getItem('mpka2_settings') || '{}')); } catch (e) {}
const saveSettings = () => { try { localStorage.setItem('mpka2_settings', JSON.stringify(SETTINGS)); } catch (e) {} };
let mmCat = 0, mmItem = ITEMS[0], mmHero = HERO, searchT = null;
function mmTab(t) {
  document.querySelectorAll('#mmTabs button').forEach(b => b.classList.toggle('on', b.dataset.tab === t));
  document.querySelectorAll('.mm-page').forEach(p => p.hidden = p.dataset.p !== t);
  if (t === 'heroes') renderMmHeroes();
  if (t === 'arsenal') renderMmArsenal();
  if (t === 'settings') renderSettings();
}
function renderMmHeroes() {
  $('mmHeroes').innerHTML = ATTRS.map(([n, c, k]) => `<div class="hcol" style="--c:${c}"><h5>${n}</h5>` +
    HEROES.filter(h => h.attr === k).map(h => `<button class="hcard${mmHero === h ? ' sel' : ''}" data-h="${h.id}">${faces(h)}<span>${h.name}</span></button>`).join('') +
    (HEROES.some(h => h.attr === k) ? '' : '<div class="hcard soon">скоро<br>новые герои</div>') + '</div>').join('');
  $('mmHeroDet').innerHTML = heroDetails(mmHero);
}
function renderMmArsenal() {
  $('mmCats').innerHTML = CATS.map((c, i) => `<button class="tab${i === mmCat ? ' on' : ''}" data-c="${i}">${c}</button>`).join('');
  $('mmItems').innerHTML = ITEMS.filter(d => d.cat === mmCat).map(d => `<div class="card${mmItem === d ? ' sel' : ''}" data-id="${d.id}">${iconHtml(d)}<div class="price">${totalCost(d)}</div></div>`).join('');
  const d = mmItem, ups = usedIn(d.id);
  $('mmItemDet').innerHTML = `<div class="det-head">${iconHtml(d)}<div><h4>${d.name}</h4><div class="p">Цена: ${totalCost(d)}</div></div></div>
    <div class="st">${describe(d)}</div>${d.lore ? `<div class="lore">${d.lore}</div>` : ''}
    ${d.parts.length ? `<h5>СОБИРАЕТСЯ ИЗ</h5><div class="st">${d.parts.map(p => ITEM[p].name).join(' + ')}${d.cost ? ' + рецепт ' + d.cost : ''}</div>` : ''}
    ${ups.length ? `<h5>НУЖЕН ДЛЯ</h5><div class="st">${ups.map(u => u.name).join(', ')}</div>` : ''}`;
}
function renderSettings() {
  document.querySelectorAll('#setDiff button').forEach(b => b.classList.toggle('on', b.dataset.v === SETTINGS.diff));
  document.querySelectorAll('#setAim button').forEach(b => b.classList.toggle('on', b.dataset.v === SETTINGS.aim));
  document.querySelectorAll('#setSound button').forEach(b => b.classList.toggle('on', b.dataset.v === SETTINGS.sound));
}
$('mmTabs').addEventListener('click', ev => { const b = ev.target.closest('button'); if (b) mmTab(b.dataset.tab); });
$('mmHeroes').addEventListener('click', ev => { const c = ev.target.closest('.hcard[data-h]'); if (c) { mmHero = HEROES.find(h => h.id === c.dataset.h); renderMmHeroes(); speak({ def: mmHero }, 1, true); } });
$('mmCats').addEventListener('click', ev => { const c = ev.target.closest('.tab'); if (c) { mmCat = +c.dataset.c; mmItem = ITEMS.find(d => d.cat === mmCat); renderMmArsenal(); } });
$('mmItems').addEventListener('click', ev => { const c = ev.target.closest('.card'); if (c) { mmItem = ITEM[c.dataset.id]; renderMmArsenal(); } });
$('setDiff').addEventListener('click', ev => { const b = ev.target.closest('button'); if (b) { SETTINGS.diff = b.dataset.v; saveSettings(); renderSettings(); } });
$('setAim').addEventListener('click', ev => { const b = ev.target.closest('button'); if (b) { SETTINGS.aim = b.dataset.v; saveSettings(); renderSettings(); } });
$('setSound').addEventListener('click', ev => { const b = ev.target.closest('button'); if (b) { SETTINGS.sound = b.dataset.v; saveSettings(); renderSettings(); if (b.dataset.v === '1') speak({ def: SANYA }, 1, true); } });
$('setFull').onclick = () => { try { if (document.fullscreenElement) document.exitFullscreen(); else document.documentElement.requestFullscreen(); } catch (e) {} };
$('menu').addEventListener('pointerdown', ev => ev.stopPropagation());
function stopSearch() { clearInterval(searchT); searchT = null; const b = $('play'); b.classList.remove('search'); b.innerHTML = 'ИГРАТЬ'; }
$('play').onclick = () => {
  if (searchT) return stopSearch();
  const b = $('play'), t0 = performance.now(), wait = rnd(2000, 4000);
  b.classList.add('search');
  searchT = setInterval(() => { const s = (performance.now() - t0) / 1000;
    b.innerHTML = `ПОИСК ИГРЫ<small>0:${String(Math.floor(s)).padStart(2, '0')} · нажми, чтобы отменить</small>`;
    if (s * 1000 >= wait) { stopSearch(); findMatch(); } }, 200);
};

// ===================== ЛОББИ =====================
const ATTRS = [['СИЛА', '#ff7a66', 'str'], ['ЛОВКОСТЬ', '#6fe07a', 'agi'], ['ИНТЕЛЛЕКТ', '#6fa8ff', 'int'], ['УНИВЕРСАЛ', '#e0b35a', 'uni']];
const LANES = [
  { id: 'top', name: 'ВЕРХ', x: 14, y: 12, info: 'Длинная линия по левому и верхнему краю карты. Две башни на каждый город, крипы каждые 30 секунд.' },
  { id: 'mid', name: 'МИД', x: 50, y: 50, info: 'Короткая линия по диагонали — самая быстрая дорога к трону врага. Тут больше всего драк.' },
  { id: 'bot', name: 'НИЗ', x: 86, y: 88, info: 'Длинная линия по нижнему и правому краю. Подальше от чужих глаз.' },
  { id: 'jungle', name: 'ЛЕС', x: 32, y: 34, info: 'Между линиями стоят лагеря гопников. Появляются с 0:30 и раз в минуту. Главный гопник бьёт больно — приходи с полным здоровьем.' },
];
let myLane = 'mid';
let lb = null;   // состояние лобби
function faces(def) { const p = heroPortrait(def); return p.map(s => `<img src="${s}" alt=""${p.length === 1 ? ' style="width:100%"' : ''}>`).join(''); }
const freeHeroes = taken => HEROES.filter(h => h !== taken);
function findMatch() {
  $('menu').hidden = true; $('endOv').hidden = true; $('found').hidden = false;
  const end = performance.now() + 10000;
  clearInterval(window._foundT);
  window._foundT = setInterval(() => {
    const left = Math.max(0, end - performance.now());
    $('foundT').textContent = Math.ceil(left / 1000); $('foundRing').style.setProperty('--k', left / 10000);
    if (left <= 0) { clearInterval(window._foundT); $('found').hidden = true; $('menu').hidden = false; toast('Игра не принята', '#ff8a80'); }
  }, 100);
}
$('accept').onclick = () => { clearInterval(window._foundT); $('found').hidden = true; openLobby(); };
function openLobby() {
  buildWorld(); buildMinimapBg(); if (!groundPat) makeGround();
  paused = true; over = false;
  lb = { phase: '', end: 0, pick: null, myHero: null, picks: new Map(), lane: 'mid', lanesShown: false };
  $('lobby').hidden = false;
  clearInterval(lb.iv); lb.iv = setInterval(lobbyTick, 150);
  setPhase('pick');
}
function setPhase(ph) {
  lb.phase = ph;
  const dur = { pick: 30, lane: 15, buy: 30 }[ph];
  lb.end = performance.now() + dur * 1000;
  if (ph === 'pick') { lb.pick = HERO; bots.forEach(b => lb.picks.set(b, { def: null, at: performance.now() + rnd(1500, 9000) })); }
  if (ph === 'lane') { lb.botLaneAt = performance.now() + rnd(1000, 3000); assignAllyLanes(lb.lane); }
  if (ph === 'buy') {
    $('lobby').hidden = true; $('buyBar').hidden = false;
    shopTab = 0; shopSel = ITEM.shawarma; openShop();
    toast('Стартовое золото: ' + hero.gold, '#ffc83a');
  }
  renderLobby();
}
function lobbyTick() {
  if (!lb) return;
  const now = performance.now(), left = Math.max(0, Math.ceil((lb.end - now) / 1000));
  $('lbTime').textContent = left; $('buyT').textContent = left;
  if (lb.phase === 'pick') {
    let changed = false;
    for (const [b, p] of lb.picks) if (!p.def && now >= p.at) { p.def = pickRandom(HEROES); changed = true; }
    if (changed) { renderLobby(); if (lb.myHero && [...lb.picks.values()].every(p => p.def)) lb.end = Math.min(lb.end, now + 900); }
  }
  if (lb.phase === 'lane' && !lb.lanesShown && now >= lb.botLaneAt) { lb.lanesShown = true; renderLobby(); }
  if (lb.phase === 'buy' && (lb.n = (lb.n || 0) + 1) % 4 === 0) renderShop();
  if (now >= lb.end) lobbyNext(true);
}
function lobbyNext(timeout) {
  if (lb.phase === 'pick') {
    if (!lb.myHero) { lb.myHero = pickRandom(HEROES); if (timeout) toast('Время вышло — случайный герой: ' + lb.myHero.name, '#ffc83a'); }
    for (const [b, p] of lb.picks) { if (!p.def) p.def = pickRandom(HEROES); setHeroDef(b, p.def); botLearn(b); }
    setHeroDef(hero, lb.myHero); buildAbilHud();
    setPhase('lane');
  } else if (lb.phase === 'lane') { myLane = lb.lane; assignAllyLanes(myLane); setPhase('buy'); }
  else if (lb.phase === 'buy') { clearInterval(lb.iv); lb = null; $('buyBar').hidden = true; closeShop(); startGame(); }
}
function teamSlot(team) {
  const me = team === playerTeam, laneName = id => LANES.find(l => l.id === id).name;
  const members = (me ? [hero] : []).concat(bots.filter(b => b.team === team));
  const cards = members.map(m => {
    const isMe = m === hero, def = isMe ? lb.myHero : (lb.picks.get(m) || {}).def;
    let ln = '';
    if (lb.phase !== 'pick') ln = isMe ? laneName(lb.lane) : !me ? '???' : lb.lanesShown ? laneName(m.lane) : '…';
    return `<div class="lbp${isMe ? ' me' : ''}"><div class="pf">${def ? faces(def) : '?'}</div><span>${isMe ? 'Ты' : m.nick}</span>${ln ? `<i>${ln}</i>` : ''}</div>`;
  }).join('');
  return `<div class="nm">${TEAM_NAME[team].toUpperCase()}</div><div class="lbps">${cards}</div>`;
}
function renderLobby() {
  $('lbK').innerHTML = teamSlot(K); $('lbM').innerHTML = teamSlot(M);
  const go = $('lbGo'), body = $('lbBody');
  if (lb.phase === 'pick') {
    $('lbPhase').textContent = 'ВЫБОР ГЕРОЯ';
    body.innerHTML = `<div class="hgrid">${ATTRS.map(([n, c, k]) => `<div class="hcol" style="--c:${c}"><h5>${n}</h5>` +
      HEROES.filter(h => h.attr === k).map(h => `<button class="hcard${lb.pick === h ? ' sel' : ''}" data-h="${h.id}">${faces(h)}<span>${h.name}</span></button>`).join('') +
      (HEROES.some(h => h.attr === k) ? '' : '<div class="hcard soon">скоро<br>новые герои</div>') + '</div>').join('')}</div>` + heroDetails(lb.pick);
    go.textContent = lb.myHero ? 'ВЫБРАН' : 'ВЫБРАТЬ'; go.disabled = !!lb.myHero;
    $('lbHint').textContent = lb.myHero ? 'Ждём, пока все выберут героев…' : 'Тапни героя, потом «Выбрать». Не успеешь — дадут случайного.';
  } else if (lb.phase === 'lane') {
    $('lbPhase').textContent = 'ВЫБОР ЛИНИИ';
    const L = LANES.find(l => l.id === lb.lane);
    body.innerHTML = `<div class="lanemap" style="background-image:url(${mmBg.toDataURL()})">${LANES.map(l =>
      `<button class="lane-b${lb.lane === l.id ? ' sel' : ''}" data-l="${l.id}" style="left:${l.x}%;top:${l.y}%">${l.name}</button>`).join('')}</div>
      <div class="lane-info"><h4>${L.name}</h4>${L.info}<br><br><span style="color:var(--muted)">Твоя база — ${playerTeam === K ? 'внизу слева' : 'вверху справа'}. Союзные боты займут остальные линии, куда пойдут враги — секрет.</span></div>`;
    go.textContent = 'ГОТОВ'; go.disabled = false;
    $('lbHint').textContent = 'Выбери, куда встанешь в начале игры.';
  }
}
function heroDetails(h) {
  const attrName = ATTRS.find(a => a[2] === h.attr)[0], attrCol = ATTRS.find(a => a[2] === h.attr)[1], main = h['base' + { str: 'Str', agi: 'Agi', int: 'Int' }[h.attr]] || 0;
  return `<div class="hdet"><h4>${h.name}</h4><div class="attr" style="color:${attrCol}">${attrName} · ${h.ranged ? 'дальний бой' : 'ближний бой'}</div>
    Здоровье: <b>${Math.round(120 + 22 * h.baseStr)}</b> · Урон: <b>${h.baseDmg + main}</b> · Броня: <b>${(h.baseArmor + h.baseAgi / 6).toFixed(1)}</b> · Скорость: <b>${Math.round(h.speed * 60)}</b>
    ${h.innate ? `<div class="ab"><i style="background:#8a5a2a">✦</i><div><b>Врождённая</b><br><span style="color:var(--muted)">${h.innate}</span></div></div>` : ''}
    ${h.abil.map(a => `<div class="ab"><i style="background:${abilIcon(a) ? `center/cover url(${abilIcon(a)})` : a.color}">${abilIcon(a) ? '' : a.key}</i><div><b>${a.name}</b>${a.ult ? ' (ульта)' : ''}<br><span style="color:var(--muted)">${a.desc.replace(/\{(\d)\}/g, (_, n) => a.v[n].join('/'))}</span></div></div>`).join('')}</div>`;
}
$('lobby').addEventListener('click', ev => {
  if (!lb) return;
  const hc = ev.target.closest('.hcard[data-h]');
  if (hc && !lb.myHero) { lb.pick = HEROES.find(h => h.id === hc.dataset.h); renderLobby(); return; }
  const ln = ev.target.closest('.lane-b'); if (ln && !ln.disabled) { lb.lane = ln.dataset.l; assignAllyLanes(lb.lane); renderLobby(); return; }
});
$('lbGo').onclick = () => {
  if (lb.phase === 'pick') { if (lb.myHero) return; lb.myHero = lb.pick; renderLobby(); speak({ def: lb.myHero }, 1, true);
    if ([...lb.picks.values()].every(p => p.def)) setTimeout(() => lb && lb.phase === 'pick' && lobbyNext(false), 700); }
  else lobbyNext(false);
};
$('buyGo').onclick = () => lobbyNext(false);

function laneStart(team, lane) {
  if (lane === 'jungle') { const c = CAMPS.find(c => c.side === team && c.big); return { x: c.x + (team === K ? -4 : 4), y: c.y + (team === K ? 4 : -4) }; }
  const t = ents.find(e => e.kind === 'tower' && e.team === team && e.lane === lane && e.tier === 1);
  const th = ents.find(e => e.kind === 'throne' && e.team === team), dx = th.x - t.x, dy = th.y - t.y, d = Math.hypot(dx, dy);
  return { x: t.x + dx / d * 4, y: t.y + dy / d * 4 };
}
function startGame() {
  targeting = -1; pending = null; selSlot = -1; matchT0 = T; tpPick = false; scoreHold = false; inspect = null;
  { const s = laneStart(hero.team, myLane); hero.order = { type: 'move', x: s.x, y: s.y };
    setTimeout(() => toast('Идёшь на линию: ' + LANES.find(l => l.id === myLane).name.toLowerCase(), '#7dff8a'), 7600); }
  cam.ppu = clamp(Math.min(W, H) / 15, 14, 34);
  $('menu').hidden = true; $('hud').hidden = false; $('pauseOv').hidden = true; $('endOv').hidden = true;
  paused = false;
  const bn = $('banner'); bn.textContent = `ТЫ ИГРАЕШЬ ЗА ${playerTeam === K ? 'КУРЧАТОВ' : 'МОСКВУ'}!`; bn.style.color = TEAM_COL[playerTeam];
  setTimeout(() => toast('5 на 5: с тобой 4 союзных бота', '#7dff8a'), 3800);
  bn.hidden = false; bn.style.opacity = 1; setTimeout(() => bn.style.opacity = 0, 3000); setTimeout(() => bn.hidden = true, 3700);
  refreshHud(true);
  try { if (document.documentElement.requestFullscreen && matchMedia('(pointer:coarse)').matches) document.documentElement.requestFullscreen().catch(() => {}); } catch (e) {}
}
function endGame(winner) {
  over = true; paused = true;
  const t = $('endTitle'); t.textContent = `Победил город ${TEAM_NAME[winner]}!`; t.style.color = TEAM_COL[winner];
  setTimeout(() => $('endOv').hidden = false, 600);
}

// ===================== ЦИКЛ =====================
let last = performance.now();
function frame(now) {
  const dt = Math.min(0.05, (now - last) / 1000); last = now;
  if (hero) {
    if (!paused) update(dt);
    draw();
    refreshHud(false);
  } else { ctx.setTransform(DPR, 0, 0, DPR, 0, 0); ctx.fillStyle = '#120b0c'; ctx.fillRect(0, 0, W, H); }
  requestAnimationFrame(frame);
}
requestAnimationFrame(frame);
