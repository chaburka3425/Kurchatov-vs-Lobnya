const { chromium } = require('playwright');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 860, height: 400 }, hasTouch: true, isMobile: true, deviceScaleFactor: 2 });
  p.on('pageerror', e => console.log('PAGEERR ' + e.message));
  await p.goto(require('url').pathToFileURL(require('path').join(__dirname, '..', 'test.html')).href); await p.waitForTimeout(300);
  await p.evaluate(() => findMatch()); await p.tap('#accept'); await p.waitForTimeout(4500);
  await p.tap('.hcard[data-h="max_gold"]'); await p.waitForTimeout(200);
  await p.screenshot({ path: require('path').join(__dirname, 'max_det.png') });
  await p.tap('#lbGo'); await p.waitForTimeout(300);
  await p.evaluate(() => { lobbyNext(); lobbyNext(); lobbyNext(); }); await p.waitForTimeout(400); await p.evaluate(() => { if (typeof closeShop === "function") closeShop(); });
  console.log(await p.evaluate(() => hero.def.id + ' ' + [...document.querySelectorAll('.abil')].length));
  await p.evaluate(() => { hero.lvl = 6; hero.points = 6; recalc(hero); [3, 0, 1, 2].forEach(i => learn(i)); refreshHud(true); });
  // Q через интерфейс: нажать Q, тапнуть по земле
  await p.evaluate(() => pressAbility(0));
  await p.tap('#game', { position: { x: 600, y: 200 } }); await p.waitForTimeout(300);
  console.log(await p.evaluate(() => `pending=${!!pending} rains=${fx.filter(f=>f.kind==='rain').length} ch=${hero.abil[0].ch}`));
  await p.evaluate(() => { for (let i = 0; i < 120; i++) update(1/60); refreshHud(true); });
  await p.screenshot({ path: require('path').join(__dirname, 'max_hud.png') });
  await b.close();
})();
