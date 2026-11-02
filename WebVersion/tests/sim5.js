const { chromium } = require('playwright');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 860, height: 400 }, hasTouch: true, isMobile: true, deviceScaleFactor: 2 });
  p.on('pageerror', e => console.log('PAGEERR ' + e.message + ' ' + (e.stack || '').split('\n')[1]));
  await p.goto(require('url').pathToFileURL(require('path').join(__dirname, '..', 'test.html')).href); await p.waitForTimeout(300);
  await p.evaluate(() => findMatch()); await p.tap('#accept'); await p.waitForTimeout(5000);
  await p.screenshot({ path: require('path').join(__dirname, 'v5_pick.png') });
  await p.tap('#lbGo'); await p.waitForTimeout(6000);
  await p.screenshot({ path: require('path').join(__dirname, 'v5_lane.png') });
  await p.evaluate(() => { lobbyNext(); lobbyNext(); });
  await p.waitForTimeout(500);
  for (let m = 1; m <= 8; m++) {
    const r = await p.evaluate(() => { for (let i = 0; i < 3600 && !over; i++) update(1/60);
      const tw = t => ents.filter(e => e.kind === 'tower' && e.alive && e.team === t).length;
      return `t=${Math.round(T)} kills K${teamKills[0]}:M${teamKills[1]} towers K${tw(0)} M${tw(1)} lvls K[${heroes.filter(h=>h.team===0).map(h=>h.lvl).join(',')}] M[${heroes.filter(h=>h.team===1).map(h=>h.lvl).join(',')}] lanes ${bots.map(b=>b.lane[0]).join('')} over=${over}`; });
    console.log(r);
    if (m === 2) { await p.evaluate(() => { cam.follow = false; const f = bots.find(b => b.alive && b.team !== playerTeam && b.lane === 'mid') || bots[5]; cam.x = hero.x; cam.y = hero.y; refreshHud(true); }); await p.waitForTimeout(300); await p.screenshot({ path: require('path').join(__dirname, 'v5_game.png') }); }
    if (r.includes('over=true')) break;
  }
  await b.close();
})();
