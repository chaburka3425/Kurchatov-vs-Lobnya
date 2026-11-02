const { chromium } = require('playwright');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 860, height: 400 }, hasTouch: true, isMobile: true, deviceScaleFactor: 2 });
  p.on('pageerror', e => console.log('PAGEERR ' + e.message + ' ' + (e.stack || '').split('\n')[1]));
  await p.goto(require('url').pathToFileURL(require('path').join(__dirname, '..', 'test.html')).href); await p.waitForTimeout(300);
  await p.evaluate(() => findMatch()); await p.tap('#accept'); await p.waitForTimeout(4500);
  await p.evaluate(() => { lobbyNext(); lobbyNext(); lobbyNext(); }); await p.waitForTimeout(400);
  await p.evaluate(() => { if (typeof closeShop === 'function') closeShop(); });
  const r1 = await p.evaluate(() => { for (let i = 0; i < 60 * 20; i++) update(1/60); refreshHud(true);
    return `T=${T.toFixed(0)} t0=${matchT0} gold=${hero.gold} tp=${hero.tpN} courier=${courier && courier.alive} kills=${teamKills}`; });
  console.log(r1);
  await p.screenshot({ path: require('path').join(__dirname, 'mob1.png') });
  // далеко от бани: покупка в тайник и доставка
  const r2 = await p.evaluate(() => { const t2 = ents.find(e => e.kind === 'tower' && e.team === hero.team && e.lane === 'mid' && e.tier === 1); hero.x = t2.x + 2; hero.y = t2.y + 2; hero.order = null; hero.maxHp = hero.hp = 99999; hero.gold += 2000; buy(ITEM.dumbbell); buy(ITEM.ticket); refreshHud(true);
    const st = hero.stash.map(d => d.id).join(','); courierDeliver();
    for (let i = 0; i < 60 * 25 && courier.state !== 'back'; i++) update(1/60);
    return `alive=${hero.alive} near=${nearShop()} carry=${courier.carry.length} stash=${st} -> now ${hero.stash.length} state=${courier.state} inv=${hero.inv.filter(Boolean).map(x => x.d.id)} tp=${hero.tpN}`; });
  console.log(r2);
  await p.screenshot({ path: require('path').join(__dirname, 'mob2.png') });
  // электричка к башне
  const r3 = await p.evaluate(() => { const tw = ents.find(e => e.alive && e.kind === 'tower' && e.team === hero.team && e.lane === 'top');
    pressTicket(); ticketAt(tw.x + 2, tw.y + 2); const st = !!hero.tp; for (let i = 0; i < 60 * 3.5; i++) update(1/60);
    return `tpStarted=${st} dist to tower=${Math.hypot(hero.x - tw.x, hero.y - tw.y).toFixed(1)} tpN=${hero.tpN} cd=${hero.tpCd.toFixed(0)}`; });
  console.log(r3);
  // денай
  const r4 = await p.evaluate(() => { const c = ents.find(e => e.alive && e.kind === 'creep' && !e.courier && e.team === hero.team && !e.neutral);
    if (!c) return 'no creep'; c.hp = c.maxHp * .3; hero.x = c.x + 1; hero.y = c.y; hero.order = { type: 'attack', t: c }; const dn0 = hero.dn;
    c.hp = 5; hero.cd = 0; attack(hero, c); for (let i = 0; i < 60 * 2 && c.alive; i++) update(1/60);
    return `deny alive=${c.alive} dn ${dn0}->${hero.dn}`; });
  console.log(r4);
  // карточка и счёт
  await p.evaluate(() => { openInspect(heroes[6]); }); await p.waitForTimeout(200); await p.screenshot({ path: require('path').join(__dirname, 'mob_card.png') });
  await p.evaluate(() => { inspect = null; showScore(true); }); await p.waitForTimeout(200); await p.screenshot({ path: require('path').join(__dirname, 'mob_score.png') });
  await p.evaluate(() => showScore(false));
  for (let m = 1; m <= 6; m++) {
    const s = await p.evaluate(() => { for (let i = 0; i < 3600 && !over; i++) update(1/60);
      return `t=${Math.round(T)} K${teamKills[0]}:M${teamKills[1]} me ${hero.kills}/${hero.deaths}/${hero.assists} lh=${hero.lh} dn=${hero.dn} bots lh=${bots.map(b=>b.lh).join(',')} dn=${bots.map(b=>b.dn).join(',')} gold=${hero.gold}`; });
    console.log(s);
  }
  await p.evaluate(() => refreshHud(true)); await p.screenshot({ path: require('path').join(__dirname, 'mob3.png') });
  await b.close();
})();
