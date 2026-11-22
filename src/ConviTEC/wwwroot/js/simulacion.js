// =====================================================================
// ConviTEC · Simulación del backend (SOLO para probar la interfaz)
// Se usa mientras api.js tenga USAR_SIMULACION = true.
// No forma parte de la solución: la lógica real (estructuras, Dijkstra,
// reservas, mensajería) la implementa el backend en C#.
// Parámetros de prueba en la URL: ?sede=LIB|ALA|SJO|LIM  y  &desconectado
// =====================================================================

import { ErrorApi } from "./errores.js";

const parametros = new URLSearchParams(location.search); //opciones de prueba desde la URL

const SEDES = [ //sedes de la cadena (orden fijo para "cantidades")
  { codigo: "LIB", nombre: "Liberia", ubicacion: "Liberia" },
  { codigo: "ALA", nombre: "Alajuela", ubicacion: "Alajuela" },
  { codigo: "SJO", nombre: "San José", ubicacion: "San José" },
  { codigo: "LIM", nombre: "Limón", ubicacion: "Limón" },
];

const RUTAS = [ //mismas rutas de config/rutas.json
  ["R01", "Liberia", "Cañas", 65], ["R02", "Puntarenas", "Orotina", 45], ["R03", "Puntarenas", "Alajuela", 80],
  ["R04", "Orotina", "Alajuela", 55], ["R05", "Alajuela", "San José", 35], ["R06", "Alajuela", "Heredia", 20],
  ["R07", "San José", "Heredia", 20], ["R08", "San José", "Cartago", 45], ["R09", "Cartago", "Paraíso", 15],
  ["R10", "Paraíso", "Turrialba", 45], ["R11", "San José", "Guápiles", 70], ["R12", "Guápiles", "Siquirres", 55],
  ["R13", "Siquirres", "Limón", 65], ["R14", "Turrialba", "Limón", 75], ["R15", "Guápiles", "Turrialba", 80],
];

const PRODUCTOS = [ //catálogo global con existencias por sede [LIB, ALA, SJO, LIM]
  ["C001", "Café molido 250 g", "Café costarricense de tueste medio", "Bebidas calientes", 2800, [6, 1, 20, 8]],
  ["G014", "Galletas de vainilla", "Paquete de 6 unidades", "Snacks", 950, [15, 30, 25, 10]],
  ["A003", "Agua 600 ml", "Agua purificada sin gas", "Bebidas", 700, [40, 0, 50, 30]],
  ["P020", "Pan cuadrado", "Pan blanco de molde, 500 g", "Panadería", 1650, [5, 12, 9, 0]],
  ["L005", "Leche 1 L", "Leche semidescremada", "Lácteos", 1100, [10, 18, 22, 6]],
  ["R010", "Refresco cola 2 L", "Bebida gaseosa", "Bebidas", 1850, [12, 9, 14, 11]],
  ["S007", "Papas tostadas 150 g", "Papas fritas con sal", "Snacks", 1200, [0, 20, 16, 7]],
  ["H002", "Huevos (12 unidades)", "Huevos blancos medianos", "Abarrotes", 2400, [8, 6, 0, 10]],
  ["A015", "Arroz 1 kg", "Arroz blanco 99 % grano entero", "Abarrotes", 1350, [20, 15, 30, 25]],
  ["J004", "Jabón de baño", "Barra de 120 g", "Higiene", 650, [0, 0, 0, 0]],
].map(([codigo, nombre, descripcion, categoria, precio, cantidades]) =>
  ({ codigo, nombre, descripcion, categoria, precio, cantidades, reservado: [0, 0, 0, 0] }));

const indiceSede = Math.max(0, SEDES.findIndex((s) => s.codigo === (parametros.get("sede") || "ALA").toUpperCase()));
const sede = SEDES[indiceSede]; //sede que simula ser esta instancia
const conectado = !parametros.has("desconectado");

// ---------- grafo y Dijkstra (solo para que la simulación sea coherente) ----------
const nodos = []; //nombres de ubicaciones
RUTAS.forEach(([, a, b]) => { if (!nodos.includes(a)) nodos.push(a); if (!nodos.includes(b)) nodos.push(b); });

function dijkstra(origen) { //distancias y predecesores desde origen
  const dist = {}, previo = {}, visitado = {};
  nodos.forEach((n) => { dist[n] = Infinity; previo[n] = null; });
  dist[origen] = 0;
  for (let i = 0; i < nodos.length; i++) {
    let u = null;
    nodos.forEach((n) => { if (!visitado[n] && (u === null || dist[n] < dist[u])) u = n; });
    if (u === null || dist[u] === Infinity) break;
    visitado[u] = true;
    RUTAS.forEach(([, a, b, t]) => { //relaja las aristas vecinas de u
      const v = a === u ? b : b === u ? a : null;
      if (v && dist[u] + t < dist[v]) { dist[v] = dist[u] + t; previo[v] = u; }
    });
  }
  return { dist, previo };
}

function camino(previo, destino) { //reconstruye la ruta hasta destino
  const ruta = [];
  for (let n = destino; n; n = previo[n]) ruta.unshift(n);
  return ruta;
}

// ---------- estado simulado de esta sede ----------
let consecutivo = 30; //para ids P-00XX
const pedidos = []; //historial de pedidos de la sede (más reciente al final)
const ventas = [];
let recorrido = { activo: false };

const isoLocal = (d = new Date()) => { const z = (n) => String(n).padStart(2, "0"); return `${d.getFullYear()}-${z(d.getMonth() + 1)}-${z(d.getDate())}T${z(d.getHours())}:${z(d.getMinutes())}:00`; }; //fecha local sin zona
const nuevoId = () => `P-${String(++consecutivo).padStart(4, "0")}`;
const hace = (dias, hora) => { const d = new Date(); d.setDate(d.getDate() - dias); d.setHours(hora, 15 + dias * 7 % 40, 0, 0); return isoLocal(d); };
const linea = (codigo, cantidad) => { const p = PRODUCTOS.find((x) => x.codigo === codigo); return { codigo, nombre: p.nombre, cantidad, precioUnitario: p.precio }; };
const totalDe = (lineas) => lineas.reduce((s, l) => s + l.cantidad * l.precioUnitario, 0);

(function datosIniciales() { //historial de ejemplo según los destinos alcanzables de la sede
  const { dist } = dijkstra(sede.ubicacion);
  const alcanzables = nodos.filter((n) => n !== sede.ubicacion && dist[n] < Infinity).sort((a, b) => dist[a] - dist[b]);
  const destino = (i) => alcanzables[i % alcanzables.length];
  const entregados = [
    [6, [["C001", 3], ["A003", 2]]], [5, [["G014", 4]]], [4, [["L005", 2], ["P020", 1]]],
    [3, [["C001", 5], ["R010", 2]]], [2, [["A015", 3], ["H002", 1]]], [1, [["C001", 2], ["S007", 3]]],
  ];
  entregados.forEach(([dias, ls], i) => { //pedidos ya entregados y sus ventas
    const lineas = ls.map(([c, n]) => linea(c, n));
    const p = { id: nuevoId(), fecha: hace(dias, 9), estado: "Entregado", destino: destino(i + 1), lineas, total: totalDe(lineas), sedesParticipantes: [sede.codigo], motivo: null };
    pedidos.push(p);
    ventas.push({ pedidoId: p.id, fecha: hace(dias, 11), destino: p.destino, lineas, total: p.total, tiempoTraslado: dist[p.destino] + (i % 3) * 25 });
  });
  const cancelado = { id: nuevoId(), fecha: hace(1, 14), estado: "Cancelado", destino: sede.codigo === "LIB" ? "Cartago" : "Cañas", lineas: [linea("G014", 2)], total: 1900, sedesParticipantes: [sede.codigo], motivo: "No existe ruta desde la sede hasta el destino. Se liberaron las reservas." };
  pedidos.push(cancelado);
  [[0, [["C001", 3], ["A003", 2]]], [1, [["G014", 6]]], [2, [["L005", 1], ["P020", 2]]], [0, [["R010", 1]]]].forEach(([d, ls]) => { //pendientes
    const lineas = ls.map(([c, n]) => linea(c, n));
    pedidos.push({ id: nuevoId(), fecha: hace(0, 8), estado: "Pendiente", destino: destino(d === 1 ? 2 : d === 2 ? 4 : 0), lineas, total: totalDe(lineas), sedesParticipantes: [sede.codigo], motivo: null });
  });
})();

// ---------- vistas de datos con la forma del contrato ----------
const productoLocal = (p) => ({ codigo: p.codigo, nombre: p.nombre, descripcion: p.descripcion, categoria: p.categoria, precio: p.precio, disponible: p.cantidades[indiceSede], reservado: p.reservado[indiceSede] });
const productoCatalogo = (p) => ({ codigo: p.codigo, nombre: p.nombre, descripcion: p.descripcion, categoria: p.categoria, precio: p.precio, cantidades: [...p.cantidades], total: p.cantidades.reduce((a, b) => a + b, 0) });
const sedesResumen = () => SEDES.map((s) => ({ codigo: s.codigo, nombre: s.nombre }));
const buscar = (codigo) => PRODUCTOS.find((p) => p.codigo.toUpperCase() === decodeURIComponent(codigo).toUpperCase());

function calcularRecorrido() { //siguiente destino más cercano desde la ubicación actual
  const { dist, previo } = dijkstra(recorrido.ubicacionActual);
  const grupos = {}; //pedidos pendientes del recorrido agrupados por destino
  recorrido.porEntregar.forEach((p) => { (grupos[p.destino] ||= []).push(p.id); });
  const destinos = Object.keys(grupos);
  const alcanzables = destinos.filter((d) => dist[d] < Infinity).sort((a, b) => dist[a] - dist[b]);
  const sig = alcanzables[0];
  recorrido.siguiente = sig ? { destino: sig, tiempo: dist[sig], camino: camino(previo, sig), pedidos: grupos[sig] } : null;
  recorrido.pendientes = destinos.filter((d) => d !== sig).map((d) => ({ destino: d, pedidos: grupos[d], tiempo: dist[d] < Infinity ? dist[d] : null }));
  recorrido.finalizado = recorrido.activo && !sig;
}

function estadoRecorrido() { //copia con la forma del contrato
  if (!recorrido.activo) {
    const pend = pedidos.filter((p) => p.estado === "Pendiente");
    const { dist } = dijkstra(sede.ubicacion);
    const grupos = {};
    pend.forEach((p) => { (grupos[p.destino] ||= []).push(p.id); });
    return { activo: false, finalizado: false, salida: null, origen: sede.ubicacion, ubicacionActual: sede.ubicacion, tiempoTotal: 0, entregas: [], siguiente: null,
      pendientes: Object.keys(grupos).map((d) => ({ destino: d, pedidos: grupos[d], tiempo: dist[d] < Infinity ? dist[d] : null })) };
  }
  const { porEntregar, ...resto } = recorrido;
  return structuredClone(resto);
}

// ---------- operaciones ----------
function registrarPedido({ destino, lineas }) {
  if (!destino || !nodos.includes(destino)) throw new ErrorApi("Seleccione un punto de entrega válido.", 400);
  if (!lineas?.length) throw new ErrorApi("El pedido debe tener al menos un producto.", 400);
  for (const l of lineas) { //valida existencias en toda la red
    const p = buscar(l.codigo);
    if (!p) throw new ErrorApi(`No existe un producto con el código ${l.codigo}.`, 404);
    const total = p.cantidades.reduce((a, b) => a + b, 0);
    if (l.cantidad < 1) throw new ErrorApi("Las cantidades deben ser mayores que 0.", 400);
    if (l.cantidad > total) return { aceptado: false, motivo: `No hay existencias suficientes de ${p.nombre}: se solicitaron ${l.cantidad} y hay ${total} disponibles en la red.`, reservasLiberadas: [] };
  }
  const aportes = []; //reserva primero en la sede local y luego en las demás
  lineas.forEach((l) => {
    const p = buscar(l.codigo); let falta = l.cantidad;
    [indiceSede, ...SEDES.keys()].filter((v, i, a) => a.indexOf(v) === i).forEach((s) => {
      const toma = Math.min(falta, p.cantidades[s]);
      if (toma > 0) { p.cantidades[s] -= toma; p.reservado[s] += toma; falta -= toma; aportes.push({ sede: SEDES[s].codigo, codigo: p.codigo, nombre: p.nombre, cantidad: toma }); }
    });
  });
  const liberar = () => aportes.forEach((a) => { const p = buscar(a.codigo); const s = SEDES.findIndex((x) => x.codigo === a.sede); p.cantidades[s] += a.cantidad; p.reservado[s] -= a.cantidad; });
  const externas = [...new Set(aportes.map((a) => a.sede).filter((c) => c !== sede.codigo))];
  const detalle = lineas.map((l) => linea(buscar(l.codigo).codigo, l.cantidad));
  const cancelar = (motivo) => { liberar(); pedidos.push({ id: nuevoId(), fecha: isoLocal(), estado: "Cancelado", destino, lineas: detalle, total: totalDe(detalle), sedesParticipantes: [sede.codigo, ...externas], motivo }); return { aceptado: false, motivo, reservasLiberadas: externas }; };

  if (!conectado && externas.length) return cancelar(`No se pudo contactar a la sede ${SEDES.find((s) => s.codigo === externas[0]).nombre}. El pedido se canceló y se liberaron las reservas.`);
  const { dist } = dijkstra(sede.ubicacion);
  if (dist[destino] === Infinity) return cancelar(`No existe ruta desde la sede ${sede.nombre} hasta ${destino}.`);
  aportes.forEach((a) => { const p = buscar(a.codigo); p.reservado[SEDES.findIndex((x) => x.codigo === a.sede)] -= a.cantidad; }); //confirma: la reserva pasa a descontada
  const pedido = { id: nuevoId(), fecha: isoLocal(), estado: "Pendiente", destino, lineas: detalle, total: totalDe(detalle), sedesParticipantes: [sede.codigo, ...externas], motivo: null };
  pedidos.push(pedido);
  return { aceptado: true, pedido, aportes, tiempoEstimado: dist[destino] };
}

function iniciarRecorrido({ salida }) {
  const porEntregar = pedidos.filter((p) => p.estado === "Pendiente");
  recorrido = { activo: porEntregar.length > 0, salida, origen: sede.ubicacion, ubicacionActual: sede.ubicacion, tiempoTotal: 0, entregas: [], porEntregar };
  if (recorrido.activo) calcularRecorrido();
  return estadoRecorrido();
}

function siguienteEntrega() {
  if (!recorrido.activo || !recorrido.siguiente) throw new ErrorApi("No hay una entrega siguiente en el recorrido.", 400);
  const s = recorrido.siguiente;
  recorrido.tiempoTotal += s.tiempo;
  recorrido.entregas.push({ orden: recorrido.entregas.length + 1, destino: s.destino, pedidos: s.pedidos, tramo: s.tiempo, acumulado: recorrido.tiempoTotal, camino: s.camino });
  recorrido.porEntregar.filter((p) => s.pedidos.includes(p.id)).forEach((p) => { //registra la venta de cada pedido entregado
    p.estado = "Entregado";
    ventas.push({ pedidoId: p.id, fecha: isoLocal(), destino: p.destino, lineas: p.lineas, total: p.total, tiempoTraslado: recorrido.tiempoTotal });
  });
  recorrido.porEntregar = recorrido.porEntregar.filter((p) => !s.pedidos.includes(p.id));
  recorrido.ubicacionActual = s.destino;
  calcularRecorrido();
  return estadoRecorrido();
}

function estadisticas() {
  const porProducto = {}; //ingresos acumulados por producto
  ventas.forEach((v) => v.lineas.forEach((l) => {
    const x = (porProducto[l.codigo] ||= { codigo: l.codigo, nombre: l.nombre, unidades: 0, precioUnitario: l.precioUnitario, ingresos: 0 });
    x.unidades += l.cantidad; x.ingresos += l.cantidad * l.precioUnitario;
  }));
  const estrella = Object.values(porProducto).sort((a, b) => b.ingresos - a.ingresos)[0] || null;
  const peores = ventas.map((v) => ({ id: v.pedidoId, monto: v.total, tiempo: v.tiempoTraslado, rendimiento: v.tiempoTraslado > 0 ? v.total / v.tiempoTraslado : v.total }))
    .sort((a, b) => a.rendimiento - b.rendimiento).slice(0, 5);
  return { ingresosTotales: ventas.reduce((s, v) => s + v.total, 0), pedidosEntregados: ventas.length, productoEstrella: estrella, peoresPedidos: peores };
}

// ---------- enrutador de la simulación ----------
export async function responder(metodo, ruta, cuerpo) {
  await new Promise((r) => setTimeout(r, 180)); //simula la latencia de la red
  const [camino_, consulta] = ruta.split("?");
  const partes = camino_.split("/").filter(Boolean).slice(1); //sin "api"
  const clave = `${metodo} ${partes[0]}${partes[1] ? "/" + (["unidades", "iniciar", "siguiente"].includes(partes[1]) ? partes[1] : ":codigo") : ""}`;
  const copia = (x) => structuredClone(x);

  switch (clave) {
    case "GET sede": return { ...sede, conectado };
    case "GET inventario": return PRODUCTOS.map(productoLocal);
    case "GET inventario/:codigo": { const p = buscar(partes[1]); if (!p) throw new ErrorApi(`No existe un producto con el código ${decodeURIComponent(partes[1])}.`, 404); return productoLocal(p); }
    case "POST inventario/unidades": {
      const p = buscar(cuerpo.codigo || "");
      if (!p) throw new ErrorApi(`No existe un producto con el código ${cuerpo.codigo}.`, 404);
      if (!(cuerpo.cantidad > 0)) throw new ErrorApi("La cantidad debe ser mayor que 0.", 400);
      p.cantidades[indiceSede] += cuerpo.cantidad;
      return productoLocal(p);
    }
    case "GET catalogo": return { sedes: sedesResumen(), productos: PRODUCTOS.map(productoCatalogo) };
    case "GET catalogo/:codigo": { const p = buscar(partes[1]); if (!p) throw new ErrorApi(`No existe un producto con el código ${decodeURIComponent(partes[1])}.`, 404); return { sedes: sedesResumen(), productos: [productoCatalogo(p)] }; }
    case "GET destinos": return nodos.filter((n) => n !== sede.ubicacion);
    case "POST pedidos": return copia(registrarPedido(cuerpo));
    case "GET pedidos": {
      const estado = new URLSearchParams(consulta).get("estado") || "todos";
      return copia([...pedidos].reverse().filter((p) => estado === "todos" || p.estado.toLowerCase() === estado));
    }
    case "GET grafo": return { nodos: nodos.map((n) => ({ nombre: n, sede: SEDES.find((s) => s.ubicacion === n)?.codigo || null })), aristas: RUTAS.map(([id, origen, destino, tiempo]) => ({ id, origen, destino, tiempo })) };
    case "GET recorrido": return estadoRecorrido();
    case "POST recorrido/iniciar": return iniciarRecorrido(cuerpo);
    case "POST recorrido/siguiente": return siguienteEntrega();
    case "GET ventas": return copia([...ventas].reverse());
    case "GET estadisticas": return estadisticas();
    default: throw new ErrorApi(`Ruta no encontrada: ${metodo} ${ruta}`, 404);
  }
}
