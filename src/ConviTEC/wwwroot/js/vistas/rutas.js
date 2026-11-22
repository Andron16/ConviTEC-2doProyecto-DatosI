// Pantalla 6 · Rutas de entrega: recorrido del vehículo con Dijkstra paso a paso
import { api } from "../api.js";
import { encabezado, numero, minutos, esc, icono, cargando, errorCarga, vacio, toast, alerta, metrica } from "../ui.js";
import { calcularPosiciones, dibujarGrafo, LEYENDA } from "../grafo.js";

let grafo = null, posiciones = null, rec = null;
let salida = new Date().getHours() < 11 ? "11:00" : "15:00"; //salida sugerida según la hora
let ocupado = false; //evita dobles clics mientras el backend responde
let origen = "";
let raiz = null;

const SALIDAS = [["11:00", "11:00 a. m."], ["15:00", "3:00 p. m."]];

export async function mostrar(contenido, estado) {
  raiz = contenido;
  origen = estado.sede?.ubicacion || "";
  contenido.innerHTML = encabezado("Rutas de entrega", "Rutas de entrega", "El vehículo sale de la sede y en cada paso entrega en el destino pendiente más cercano.") + cargando("Cargando grafo de rutas…");
  try {
    [grafo, rec] = await Promise.all([api.grafo(), api.recorrido()]);
    origen = rec.origen || origen;
    posiciones = calcularPosiciones(grafo, origen);
    dibujar();
  } catch (e) {
    contenido.innerHTML = encabezado("Rutas de entrega", "Rutas de entrega") + errorCarga(e, () => mostrar(contenido, estado));
  }
}

function botones() { //acciones según el estado del recorrido
  const sel = `<div class="campo" style="min-width:180px"><label for="salida">Salida</label><div class="control"><select id="salida" ${rec.activo && !rec.finalizado ? "disabled" : ""}>${SALIDAS.map(([v, t]) => `<option value="${v}" ${salida === v ? "selected" : ""}>${t}</option>`).join("")}</select></div></div>`;
  const giro = ocupado ? icono("recargar", "girando") : "";
  if (!rec.activo || rec.finalizado)
    return `${sel}<button class="boton primario" id="iniciar" ${ocupado ? "disabled" : ""} style="align-self:flex-end">${giro || icono("camion")}${rec.finalizado ? "Iniciar nuevo recorrido" : "Iniciar recorrido"}</button>`;
  return `${sel}<button class="boton secundario" id="todo" ${ocupado || !rec.siguiente ? "disabled" : ""} style="align-self:flex-end">${icono("ruta")}Entregar todo</button>
    <button class="boton primario" id="siguiente" ${ocupado || !rec.siguiente ? "disabled" : ""} style="align-self:flex-end">${giro || icono("ubicacion")}Entregar siguiente</button>`;
}

function paradas() { //Recorrido/Parada para cada destino
  const items = [];
  rec.entregas.forEach((e) => items.push(`<div class="parada completada"><span class="orden">${icono("check", "p")}</span>
    <div><div class="destino">${esc(e.destino)}</div><div class="pequeno secundario">${e.pedidos.map(esc).join(", ")}</div><span class="estado">${icono("check", "p")}Entregado</span></div>
    <div class="tiempos"><strong>${minutos(e.tramo)}</strong><span class="secundario">Acumulado: ${minutos(e.acumulado)}</span></div></div>`));
  if (rec.siguiente) items.push(`<div class="parada siguiente"><span class="orden">${rec.entregas.length + 1}</span>
    <div><div class="destino">${esc(rec.siguiente.destino)}</div><div class="pequeno secundario">${rec.siguiente.pedidos.map(esc).join(", ")}</div>
      <div class="pequeno secundario">Camino: ${rec.siguiente.camino.map(esc).join(" → ")}</div><span class="estado">${icono("ubicacion", "p")}Siguiente entrega</span></div>
    <div class="tiempos"><strong>${minutos(rec.siguiente.tiempo)}</strong><span class="secundario">Acumulado: ${minutos(rec.tiempoTotal + rec.siguiente.tiempo)}</span></div></div>`);
  rec.pendientes.forEach((p) => items.push(p.tiempo === null
    ? `<div class="parada inalcanzable"><span class="orden">${icono("alerta-triangulo", "p")}</span><div><div class="destino">${esc(p.destino)}</div><div class="pequeno secundario">${p.pedidos.map(esc).join(", ")}</div><span class="estado">${icono("alerta-triangulo", "p")}Sin ruta desde ${esc(rec.ubicacionActual)}</span></div><div class="tiempos"><strong>—</strong></div></div>`
    : `<div class="parada pendiente"><span class="orden">·</span><div><div class="destino">${esc(p.destino)}</div><div class="pequeno secundario">${p.pedidos.map(esc).join(", ")}</div></div><div class="tiempos"><strong>${minutos(p.tiempo)}</strong><span class="secundario">desde ${esc(rec.ubicacionActual)}</span></div></div>`));
  return items.join("");
}

function dibujar() {
  if (!location.hash.startsWith("#/rutas")) return; //el usuario cambió de pantalla
  const pedidosEntregados = rec.entregas.reduce((s, e) => s + e.pedidos.length, 0);
  let aviso = "";
  if (rec.finalizado) {
    aviso = alerta("exito", `Recorrido de las ${SALIDAS.find(([v]) => v === rec.salida)?.[1] || ""} finalizado`, `Se entregaron ${numero(pedidosEntregados)} pedido(s) en ${numero(rec.entregas.length)} destino(s). Tiempo total: ${minutos(rec.tiempoTotal)}.`);
    const sinRuta = rec.pendientes.filter((p) => p.tiempo === null);
    if (sinRuta.length) aviso += alerta("advertencia", "Destinos sin ruta", `No se pudo llegar a: ${sinRuta.map((p) => p.destino).join(", ")}.`);
  } else if (rec.activo && rec.siguiente) {
    aviso = alerta("info", `Siguiente entrega: ${rec.siguiente.destino} (${minutos(rec.siguiente.tiempo)} desde ${rec.ubicacionActual})`, `Dijkstra desde la ubicación actual eligió el destino pendiente más cercano. Camino: ${rec.siguiente.camino.join(" → ")}.`);
  } else if (!rec.activo && rec.pendientes.length) {
    aviso = alerta("info", `${numero(rec.pendientes.reduce((s, p) => s + p.pedidos.length, 0))} pedido(s) listos para la salida`, `Inicie el recorrido para calcular las entregas desde ${origen}.`);
  }

  const lista = !rec.activo && !rec.pendientes.length
    ? vacio("camion", "No hay pedidos para esta salida", "Los pedidos pendientes de la sede aparecerán aquí.", '<a class="boton secundario" href="#/pedido">Realizar pedido</a>')
    : `${!rec.activo ? `<div class="pie-tabla">Tiempos desde la sede (${esc(origen)})</div>` : ""}${paradas()}`;

  raiz.innerHTML = encabezado("Rutas de entrega", "Rutas de entrega", "El vehículo sale de la sede y en cada paso entrega en el destino pendiente más cercano.", botones()) + aviso + `
    <div class="rejilla r-60-40">
      <section class="tarjeta"><div class="panel-cab"><h3>Red de distribución</h3><span class="pequeno secundario">${rec.activo ? `Vehículo en ${esc(rec.ubicacionActual)}` : `Sede: ${esc(origen)}`}</span></div>
        <div class="grafo-env">${dibujarGrafo(grafo, rec, origen, posiciones)}</div>${LEYENDA}</section>
      <div class="pila">
        <section class="tarjeta"><div class="panel-cab"><h3>Paradas del recorrido</h3>${rec.activo ? `<span class="pequeno secundario">Salida ${SALIDAS.find(([v]) => v === rec.salida)?.[1] || ""}</span>` : ""}</div>${lista}</section>
        ${metrica("Tiempo total del recorrido", minutos(rec.tiempoTotal), `${numero(rec.entregas.length)} destino(s) · ${numero(pedidosEntregados)} pedido(s) entregados`, "reloj", true)}
      </div>
    </div>`;
  raiz.querySelector("#salida")?.addEventListener("change", (e) => (salida = e.target.value));
  raiz.querySelector("#iniciar")?.addEventListener("click", iniciar);
  raiz.querySelector("#siguiente")?.addEventListener("click", () => entregar(false));
  raiz.querySelector("#todo")?.addEventListener("click", () => entregar(true));
}

async function iniciar() { //arranca el recorrido de la salida elegida
  ocupado = true; dibujar();
  try {
    rec = await api.iniciarRecorrido(salida);
    if (!rec.activo) toast("info", "No hay pedidos pendientes para esta salida.");
  } catch (e) { toast("error", e.message); }
  ocupado = false; dibujar();
}

async function entregar(todo) { //una entrega, o todas con una pausa para ver el avance
  ocupado = true; dibujar();
  try {
    do {
      const destino = rec.siguiente.destino, ids = rec.siguiente.pedidos, t = rec.siguiente.tiempo;
      rec = await api.siguienteEntrega();
      toast("exito", `Entregado en ${destino}: ${ids.join(", ")} (${minutos(t)})`);
      if (todo && rec.siguiente) { dibujar(); await new Promise((r) => setTimeout(r, 900)); }
    } while (todo && rec.siguiente && location.hash.startsWith("#/rutas"));
  } catch (e) { toast("error", e.message); }
  ocupado = false; dibujar();
}
