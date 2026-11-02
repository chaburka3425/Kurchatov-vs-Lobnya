const { chromium } = require('playwright');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 860, height: 400 }, hasTouch: true, isMobile: true, deviceScaleFactor: 2 });
  p.on('pageerror', e => console.log('PAGEERR ' + e.message + ' ' + (e.stack || '').split('\n')[1]));
  p.on('console', m => { if (m.type() === 'error') console.log('CONSOLE ' + m.text()); });
  await p.goto(require('url').pathToFileURL(require('path').join(__dirname, '..', 'test.html')).href); await p.waitForTimeout(300);
  await p.evaluate(() => findMatch()); await p.tap('#accept'); await p.waitForTimeout(5000);
  await p.screenshot({ path: require('path').join(__dirname, 'max_pick.png') });
  await p.evaluate(() => { lobbyNext(); lobbyNext(); lobbyNext(); });
  await p.waitForTimeout(500);
  const info = await p.evaluate(() => {
    setHeroDef(hero, MAX); buildAbilHud();
    for (const bb of bots) { setHeroDef(bb, MAX); bb.lvl = 1; bb.points = 1; botLearn(bb); }
    hero.lvl = 6; hero.points = 6; recalc(hero); [3, 0, 1, 2, 0, 1].forEach(i => learn(i)); hero.mp = hero.maxMp;
    return `range=${hero.range} abil=${hero.abil.map(a => a.lvl + '/' + a.ch).join(' ')}`;
  });
  console.log(info);
  // каст Q в точку, E, R по ближайшему врагу-герою
  const r = await p.evaluate(() => {
    const foe = bots.find(b => b.team !== playerTeam);
    foe.x = hero.x + 10; foe.y = hero.y; hero.face = 0;
    doCast(0, { x: foe.x, y: foe.y, r: 0, point: true });
    const ch1 = hero.abil[0].ch;
    doCast(2, hero);
    const rng = hero.range;
    hero.mp = hero.maxMp;
    doCast(3, foe);
    return `ch after Q=${ch1} range on ladder=${rng} channel=${hero.channel && hero.channel.kind} foeHp=${Math.round(foe.hp)}`;
  });
  console.log(r);
  await p.evaluate(() => { for (let i = 0; i < 70; i++) update(1/60); cam.x = hero.x + 5; cam.y = hero.y; cam.follow = false; refreshHud(true); });
  await p.waitForTimeout(200); await p.screenshot({ path: require('path').join(__dirname, 'max_aim.png') });
  const r2 = await p.evaluate(() => { const foe = bots.find(b => b.team !== playerTeam); const hp0 = foe.hp; for (let i = 0; i < 80; i++) update(1/60);
    return `foe hp ${Math.round(hp0)} -> ${Math.round(foe.hp)} alive=${foe.alive} channel=${!!hero.channel} ladder=${hero.ladder > T}`; });
  console.log(r2);
  await p.evaluate(() => { cam.x = hero.x + 5; cam.y = hero.y; refreshHud(true); });
  await p.waitForTimeout(200); await p.screenshot({ path: require('path').join(__dirname, 'max_rain.png') });
  // зум на модель
  await p.evaluate(() => { cam.ppu = 55; cam.x = hero.x + 1; cam.y = hero.y - 1.5; hero.ladder = 0; recalc(hero, true); });
  await p.waitForTimeout(200); await p.screenshot({ path: require('path').join(__dirname, 'max_model.png') });
  // симуляция 6 минут, все боты — Максы
  for (let m = 1; m <= 6; m++) {
    const s = await p.evaluate(() => { for (let i = 0; i < 3600 && !over; i++) update(1/60);
      return `t=${Math.round(T)} kills K${teamKills[0]}:M${teamKills[1]} lv ${heroes.map(h => h.lvl).join(',')} rains=${fx.filter(f => f.kind === 'rain').length} aims=${heroes.filter(h => h.channel).length} Qch=${bots.map(b=>b.abil[0].ch).join('')}`; });
    console.log(s);
  }
  await b.close();
})();
