import fs from 'fs';
const [,, urlPath, from, to, backup] = process.argv;
const ws = new WebSocket('ws://supervisor/core/websocket');
let id = 1; const pending = {};
const send = (m) => new Promise(r => { m.id = id++; pending[m.id] = r; ws.send(JSON.stringify(m)); });
ws.onmessage = async (e) => {
  const m = JSON.parse(e.data);
  if (m.type === 'auth_required') ws.send(JSON.stringify({type:'auth', access_token: process.env.SUPERVISOR_TOKEN}));
  else if (m.type === 'auth_invalid') { console.error('auth failed'); process.exit(1); }
  else if (m.type === 'auth_ok') {
    const g = await send({type:'lovelace/config', url_path: urlPath});
    if (!g.success) { console.error(g); process.exit(1); }
    fs.writeFileSync(backup, JSON.stringify(g.result));
    const s = JSON.stringify(g.result);
    const n = s.split(from).length - 1;
    if (n !== 1) { console.error('match count', n); process.exit(1); }
    const r = await send({type:'lovelace/config/save', url_path: urlPath, config: JSON.parse(s.replace(from, to))});
    console.log('saved:', r.success, r.error || ''); process.exit(r.success ? 0 : 1);
  } else if (pending[m.id]) pending[m.id](m);
};
