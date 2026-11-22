// ConviTEC · dibujo del grafo de distribución en SVG
// Las posiciones se calculan aquí solo para dibujar (no forman parte de la lógica del sistema):
// el grafo llega del backend sin coordenadas y puede cambiar en la defensa.
import { esc } from "./ui.js";

const ANCHO = 680, ALTO = 460, R = 18; //tamaño del lienzo y radio de nodo

function componentes(nombres, aristas) { //grupos de nodos conectados (para separar los aislados)
  const grupo = {};
  let n = 0;
  nombres.forEach((inicio) => {
    if (grupo[inicio] !== undefined) return;
    const cola = [inicio];
    grupo[inicio] = n;
    while (cola.length) {
      const u = cola.shift();
      aristas.forEach((a) => {
        const v = a.origen === u ? a.destino : a.destino === u ? a.origen : null;
        if (v !== null && grupo[v] === undefined) { grupo[v] = n; cola.push(v); }
      });
    }
    n++;
  });
  return grupo;
}

// Coordenadas aproximadas (longitud, latitud) de ciudades de Costa Rica: sirven como punto de
// partida para que el dibujo se parezca al mapa. Las ubicaciones que no estén aquí se acomodan solas.
const GEO = {
  liberia: [-85.44, 10.63], canas: [-85.09, 10.43], tilaran: [-84.97, 10.47], upala: [-85.02, 10.9], nicoya: [-85.45, 10.15],
  "santa cruz": [-85.59, 10.26], puntarenas: [-84.83, 9.98], orotina: [-84.52, 9.91], jaco: [-84.63, 9.62], quepos: [-84.16, 9.43],
  "san ramon": [-84.47, 10.09], grecia: [-84.31, 10.07], atenas: [-84.38, 9.98], "ciudad quesada": [-84.43, 10.32], "san carlos": [-84.43, 10.32],
  "los chiles": [-84.71, 11.03], alajuela: [-84.21, 10.02], heredia: [-84.12, 10.0], "san jose": [-84.08, 9.93], cartago: [-83.92, 9.86],
  paraiso: [-83.87, 9.84], turrialba: [-83.68, 9.9], sarapiqui: [-84.01, 10.45], guapiles: [-83.78, 10.21], siquirres: [-83.51, 10.1],
  limon: [-83.03, 9.99], "perez zeledon": [-83.7, 9.37], "san isidro": [-83.7, 9.37], "palmar norte": [-83.46, 8.96], golfito: [-83.17, 8.64],
};
const normal = (t) => t.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/\s*\(.*\)/, "").toLowerCase().trim(); //sin tildes

function ubicar(miembros, aristas, caja) { //acomoda un componente dentro de una caja
  const pos = {}, ancla = {};
  const propias = aristas.filter((a) => miembros.includes(a.origen) && miembros.includes(a.destino));
  const conGeo = miembros.filter((n) => GEO[normal(n)]);
  if (conGeo.length > 1) { //punto de partida: coordenadas geográficas escaladas a la caja
    const lon = conGeo.map((n) => GEO[normal(n)][0]), lat = conGeo.map((n) => GEO[normal(n)][1]);
    const [x0, x1, y0, y1] = [Math.min(...lon), Math.max(...lon), Math.min(...lat), Math.max(...lat)];
    conGeo.forEach((n) => {
      const [lo, la] = GEO[normal(n)];
      ancla[n] = { x: caja.x + (x1 > x0 ? (lo - x0) / (x1 - x0) : 0.5) * caja.w, y: caja.y + (y1 > y0 ? (y1 - la) / (y1 - y0) : 0.5) * caja.h };
      pos[n] = { ...ancla[n] };
    });
  }
  const cx = caja.x + caja.w / 2, cy = caja.y + caja.h / 2;
  miembros.filter((n) => !pos[n]).forEach((n, i, sin) => { //sin coordenadas: en círculo
    const a = (2 * Math.PI * i) / Math.max(1, sin.length) + 0.7;
    pos[n] = { x: cx + Math.cos(a) * caja.w * 0.35, y: cy + Math.sin(a) * caja.h * 0.35 };
  });
  if (miembros.length > 2) {
    const k = Math.sqrt((Math.max(caja.w, 60) * Math.max(caja.h, 60)) / miembros.length) * 1.05;
    for (let it = 0, t = 30; it < 400; it++, t *= 0.99) { //separa nodos cercanos manteniendo la forma
      const d = {};
      miembros.forEach((n) => (d[n] = { x: 0, y: 0 }));
      miembros.forEach((u, i) => miembros.slice(i + 1).forEach((v) => { //repulsión
        const dx = pos[u].x - pos[v].x, dy = pos[u].y - pos[v].y, dist = Math.max(0.5, Math.hypot(dx, dy)), f = (k * k) / dist;
        d[u].x += (dx / dist) * f; d[u].y += (dy / dist) * f; d[v].x -= (dx / dist) * f; d[v].y -= (dy / dist) * f;
      }));
      propias.forEach(({ origen: u, destino: v }) => { //atracción por aristas
        const dx = pos[u].x - pos[v].x, dy = pos[u].y - pos[v].y, dist = Math.max(0.5, Math.hypot(dx, dy)), f = ((dist * dist) / k) * 0.4;
        d[u].x -= (dx / dist) * f; d[u].y -= (dy / dist) * f; d[v].x += (dx / dist) * f; d[v].y += (dy / dist) * f;
      });
      miembros.forEach((n) => { //resorte suave hacia la posición geográfica o al centro
        const objetivo = ancla[n] || { x: cx, y: cy }, fuerza = ancla[n] ? 0.25 : 0.05;
        d[n].x += (objetivo.x - pos[n].x) * fuerza; d[n].y += (objetivo.y - pos[n].y) * fuerza;
        const m = Math.max(0.01, Math.hypot(d[n].x, d[n].y));
        pos[n].x += (d[n].x / m) * Math.min(m, t); pos[n].y += (d[n].y / m) * Math.min(m, t);
      });
    }
  }
  const xs = miembros.map((n) => pos[n].x), ys = miembros.map((n) => pos[n].y); //ajusta a la caja
  const [a0, a1, b0, b1] = [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)];
  miembros.forEach((n) => {
    pos[n].x = caja.x + (a1 - a0 > 1 ? (pos[n].x - a0) / (a1 - a0) : 0.5) * caja.w;
    pos[n].y = caja.y + (b1 - b0 > 1 ? (pos[n].y - b0) / (b1 - b0) : 0.5) * caja.h;
  });
  if (miembros.length === 2 && Math.hypot(pos[miembros[0]].x - pos[miembros[1]].x, pos[miembros[0]].y - pos[miembros[1]].y) < 60) { //evita que dos nodos queden encimados
    pos[miembros[0]] = { x: cx, y: caja.y }; pos[miembros[1]] = { x: cx, y: caja.y + caja.h };
  }
  return pos;
}

export function calcularPosiciones(grafo, origen) { //posiciones de todos los nodos
  const nombres = grafo.nodos.map((n) => n.nombre);
  const grupo = componentes(nombres, grafo.aristas);
  const principal = grupo[origen] ?? grupo[nombres[0]]; //componente alcanzable desde la sede
  const tamano = {}; nombres.forEach((n) => (tamano[grupo[n]] = (tamano[grupo[n]] || 0) + 1));
  const mayor = Number(Object.keys(tamano).reduce((a, b) => (tamano[b] > tamano[a] ? b : a))); //el más grande ocupa el área principal
  const otros = [...new Set(Object.values(grupo))].filter((g) => g !== mayor);
  const xMain = otros.length ? 236 : 70;
  const pos = ubicar(nombres.filter((n) => grupo[n] === mayor), grafo.aristas, { x: xMain, y: 44, w: ANCHO - xMain - 70, h: ALTO - 96 });
  const cajasAisladas = [];
  let y = 12;
  otros.forEach((g) => { //componentes aislados en una columna a la izquierda
    const miembros = nombres.filter((n) => grupo[n] === g);
    const alto = Math.min(ALTO - 24, 110 + 80 * (miembros.length - 1));
    const caja = { x: 12, y, w: 176, h: alto };
    cajasAisladas.push(caja);
    Object.assign(pos, ubicar(miembros, grafo.aristas, { x: caja.x + 62, y: caja.y + 44, w: 52, h: Math.max(1, alto - 88) }));
    y += alto + 12;
  });
  return { pos, grupo, principal, cajasAisladas };
}

const par = (a, b) => [a, b].sort().join("|"); //clave de arista sin dirección
function pares(camino = []) { const s = new Set(); for (let i = 1; i < camino.length; i++) s.add(par(camino[i - 1], camino[i])); return s; }

export function dibujarGrafo(grafo, recorrido, origen, posiciones) { //devuelve el SVG como texto
  const { pos, grupo, principal, cajasAisladas } = posiciones;
  const recorridas = new Set(), siguiente = pares(recorrido?.siguiente?.camino);
  (recorrido?.entregas || []).forEach((e) => pares(e.camino).forEach((p) => recorridas.add(p)));
  const visitados = new Set((recorrido?.entregas || []).map((e) => e.destino));
  const pendientes = new Set([...(recorrido?.pendientes || []).map((p) => p.destino), recorrido?.siguiente?.destino].filter(Boolean));
  const actual = recorrido?.activo ? recorrido.ubicacionActual : null;
  const sedePor = {}; grafo.nodos.forEach((n) => (sedePor[n.nombre] = n.sede));

  let svg = `<svg viewBox="0 0 ${ANCHO} ${ALTO}" role="img" aria-label="Grafo de rutas de distribución"><title>Grafo de rutas de distribución</title>`;
  cajasAisladas.forEach((c) => { svg += `<rect x="${c.x}" y="${c.y}" width="${c.w}" height="${c.h}" rx="8" fill="none" stroke="var(--g-aislado)" stroke-width="1.5" stroke-dasharray="6 4"/><text x="${c.x + 8}" y="${c.y + 16}" font-size="11" font-weight="500" fill="var(--texto-2)">Componente aislado</text>`; });

  grafo.aristas.forEach((a) => { //aristas con su estado
    const p = pos[a.origen], q = pos[a.destino], k = par(a.origen, a.destino);
    const aislada = grupo[a.origen] !== principal;
    let estilo = `stroke="var(--g-arista)" stroke-width="2"`;
    if (aislada) estilo = `stroke="var(--g-aislado)" stroke-width="2" stroke-dasharray="6 4"`;
    if (recorridas.has(k)) estilo = `stroke="var(--g-recorrido)" stroke-width="5"`;
    if (siguiente.has(k)) estilo = `stroke="var(--g-siguiente)" stroke-width="4" stroke-dasharray="9 6"`;
    svg += `<line x1="${p.x}" y1="${p.y}" x2="${q.x}" y2="${q.y}" ${estilo} stroke-linecap="round"/>`;
  });
  grafo.aristas.forEach((a) => { //peso de cada arista en minutos
    const p = pos[a.origen], q = pos[a.destino], mx = (p.x + q.x) / 2, my = (p.y + q.y) / 2, txt = `${a.tiempo} min`, w = txt.length * 6.4 + 12;
    svg += `<rect x="${mx - w / 2}" y="${my - 10}" width="${w}" height="20" rx="10" fill="#fff" stroke="var(--borde)"/><text x="${mx}" y="${my + 4}" font-size="11" font-weight="500" text-anchor="middle" fill="var(--texto)">${txt}</text>`;
  });
  grafo.nodos.forEach(({ nombre }) => { //nodos con su estado
    const { x, y } = pos[nombre], sede = sedePor[nombre], aislado = grupo[nombre] !== principal;
    let anillo = "", insignia = "";
    if (visitados.has(nombre)) { anillo = "var(--exito)"; insignia = "check"; }
    if (pendientes.has(nombre)) { anillo = "var(--advertencia)"; insignia = "ubicacion"; }
    if (nombre === actual) { anillo = "var(--primaria)"; insignia = "camion"; }
    if (anillo) svg += `<circle cx="${x}" cy="${y}" r="${R + 6}" fill="none" stroke="${anillo}" stroke-width="3"/>`;
    svg += sede
      ? `<circle cx="${x}" cy="${y}" r="${R}" fill="var(--g-sede)" ${aislado ? 'stroke="#fff" stroke-width="2"' : ""}/><text x="${x}" y="${y + 4}" font-size="11" font-weight="600" text-anchor="middle" fill="#fff">${esc(sede)}</text>`
      : `<circle cx="${x}" cy="${y}" r="${R}" fill="#fff" stroke="${aislado ? "var(--g-aislado)" : "var(--g-punto)"}" stroke-width="2" ${aislado ? 'stroke-dasharray="5 3"' : ""}/>`;
    if (insignia) svg += `<circle cx="${x + R}" cy="${y - R}" r="11" fill="#fff" stroke="${anillo}" stroke-width="2"/><use href="img/iconos.svg#${insignia}" x="${x + R - 7}" y="${y - R - 7}" width="14" height="14" style="color:${anillo}"/>`;
    svg += `<text x="${x}" y="${y + R + 18}" font-size="12" font-weight="600" text-anchor="middle" fill="var(--texto)" paint-order="stroke" stroke="#fff" stroke-width="4" stroke-linejoin="round">${esc(nombre)}</text>`;
  });
  return svg + "</svg>";
}

export const LEYENDA = `<div class="leyenda" aria-label="Leyenda del grafo">
  <span><svg viewBox="0 0 28 16"><circle cx="14" cy="8" r="7" fill="var(--g-sede)"/></svg>Sede</span>
  <span><svg viewBox="0 0 28 16"><circle cx="14" cy="8" r="6" fill="#fff" stroke="var(--g-punto)" stroke-width="2"/></svg>Punto de entrega</span>
  <span><svg viewBox="0 0 28 16"><circle cx="14" cy="8" r="6" fill="none" stroke="var(--primaria)" stroke-width="3"/></svg>Vehículo aquí</span>
  <span><svg viewBox="0 0 28 16"><circle cx="14" cy="8" r="6" fill="none" stroke="var(--advertencia)" stroke-width="3"/></svg>Destino pendiente</span>
  <span><svg viewBox="0 0 28 16"><circle cx="14" cy="8" r="6" fill="none" stroke="var(--exito)" stroke-width="3"/></svg>Entregado</span>
  <span><svg viewBox="0 0 28 16"><line x1="2" y1="8" x2="26" y2="8" stroke="var(--g-recorrido)" stroke-width="5"/></svg>Tramo recorrido</span>
  <span><svg viewBox="0 0 28 16"><line x1="2" y1="8" x2="26" y2="8" stroke="var(--g-siguiente)" stroke-width="4" stroke-dasharray="6 4"/></svg>Siguiente tramo</span>
  <span><svg viewBox="0 0 28 16"><rect x="2" y="2" width="24" height="12" rx="3" fill="none" stroke="var(--g-aislado)" stroke-dasharray="4 3"/></svg>Componente aislado</span>
</div>`;
