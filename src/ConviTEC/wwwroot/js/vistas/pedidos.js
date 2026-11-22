// Pantalla 5 · Pedidos: pendientes de entrega e historial completo
import { api } from "../api.js";
import { encabezado, colones, numero, esc, icono, esqueleto, errorCarga, vacio, insignia, fecha, insigniaSede } from "../ui.js";

let pestana = "pendientes"; //"pendientes" | "historial"
let filtro = "todos"; //estado del historial
let lista = [];
let abiertos = new Set(); //ids de pedidos expandidos
let conteoPendientes = null;
let sedeActual = "";

export async function mostrar(contenido, estado) {
  sedeActual = estado.sede?.codigo || "";
  abiertos = new Set();
  contenido.innerHTML = encabezado("Pedidos", "Pedidos", "Pedidos registrados en esta sede.", `<a class="boton primario" href="#/pedido">${icono("carrito")}Realizar pedido</a>`) +
    `<div class="pestanas" role="tablist" id="tabs"></div><section class="tarjeta" id="lista"></section>`;
  await cargar();
}

function dibujarPestanas() {
  const tabs = document.getElementById("tabs");
  if (!tabs) return;
  tabs.innerHTML = [["pendientes", "Pendientes de entrega"], ["historial", "Historial"]].map(([id, texto]) =>
    `<button role="tab" aria-selected="${pestana === id}" class="${pestana === id ? "activa" : ""}" data-tab="${id}">${texto}${id === "pendientes" && conteoPendientes !== null ? `<span class="contador">${numero(conteoPendientes)}</span>` : ""}</button>`).join("");
  tabs.querySelectorAll("[data-tab]").forEach((b) => (b.onclick = () => { pestana = b.dataset.tab; abiertos = new Set(); cargar(); }));
}

async function cargar() {
  dibujarPestanas();
  const cont = document.getElementById("lista");
  cont.innerHTML = `<div class="panel-cuerpo">${esqueleto(5)}</div>`;
  try {
    lista = await api.pedidos(pestana === "pendientes" ? "pendiente" : filtro);
    if (pestana === "pendientes") conteoPendientes = lista.length;
    else if (conteoPendientes === null) conteoPendientes = (await api.pedidos("pendiente")).length;
    dibujarPestanas();
    dibujar();
  } catch (e) {
    cont.innerHTML = `<div class="panel-cuerpo">${errorCarga(e, cargar)}</div>`;
  }
}

function resumen(lineas) { //"Café ×3, Agua ×2"
  return lineas.map((l) => `${l.nombre} ×${numero(l.cantidad)}`).join(", ");
}

function dibujar() {
  const cont = document.getElementById("lista");
  if (!cont) return;
  const filtroHtml = pestana === "historial" ? `<div class="panel-cuerpo" style="padding-bottom:16px"><div class="campo" style="max-width:280px"><label for="filtro">Filtrar por estado</label><div class="control"><select id="filtro">
      ${[["todos", "Todos"], ["pendiente", "Pendiente"], ["entregado", "Entregado"], ["cancelado", "Cancelado"], ["rechazado", "Rechazado"]].map(([v, t]) => `<option value="${v}" ${filtro === v ? "selected" : ""}>${t}</option>`).join("")}</select></div></div></div>` : "";
  if (!lista.length) {
    cont.innerHTML = filtroHtml + (pestana === "pendientes"
      ? vacio("lista", "No hay pedidos pendientes de entrega", "Los pedidos registrados en esta sede aparecerán aquí.", '<a class="boton primario" href="#/pedido">Realizar pedido</a>')
      : vacio("lista", "No hay pedidos con este estado", "Pruebe con otro filtro."));
    enlazarFiltro(cont);
    return;
  }
  const columnas = pestana === "pendientes" ? 7 : 8;
  cont.innerHTML = filtroHtml + `<div class="tabla-env"><table>
    <thead><tr><th><span class="sr">Expandir</span></th><th>Pedido</th><th>Fecha de creación</th><th>Destino</th><th>Productos</th><th class="der">Monto</th><th>Sedes participantes</th>${pestana === "historial" ? "<th>Estado</th>" : ""}</tr></thead>
    <tbody>${lista.map((p, i) => {
      const abierto = abiertos.has(p.id);
      return `<tr class="${i % 2 ? "fila-par" : ""}${abierto ? " expandida" : ""}">
        <td style="width:52px"><button class="boton-icono" data-abrir="${esc(p.id)}" aria-expanded="${abierto}" aria-label="${abierto ? "Ocultar" : "Ver"} detalle de ${esc(p.id)}">${icono(abierto ? "chevron-abajo" : "chevron-derecha")}</button></td>
        <td><strong>${esc(p.id)}</strong></td><td>${esc(fecha(p.fecha))}</td><td>${esc(p.destino)}</td>
        <td style="max-width:280px">${esc(resumen(p.lineas))}</td><td class="der num">${colones(p.total)}</td>
        <td><div style="display:flex;gap:4px;flex-wrap:wrap">${p.sedesParticipantes.map((s) => insigniaSede(s, s === sedeActual)).join("")}</div></td>
        ${pestana === "historial" ? `<td>${insignia(p.estado)}</td>` : ""}</tr>
        ${abierto ? `<tr class="detalle"><td colspan="${columnas}">
          ${p.motivo ? `<div class="motivo">${icono("alerta-triangulo")}<span><strong>Motivo:</strong> ${esc(p.motivo)}</span></div>` : ""}
          <table><thead><tr><th>Producto</th><th class="der">Cantidad</th><th class="der">Precio unitario</th><th class="der">Subtotal</th></tr></thead>
          <tbody>${p.lineas.map((l) => `<tr><td>${esc(l.nombre)} <span class="secundario">· ${esc(l.codigo)}</span></td><td class="der num">${numero(l.cantidad)}</td><td class="der num">${colones(l.precioUnitario)}</td><td class="der num">${colones(l.cantidad * l.precioUnitario)}</td></tr>`).join("")}
          <tr><td colspan="3" class="der"><strong>Total</strong></td><td class="der num"><strong>${colones(p.total)}</strong></td></tr></tbody></table></td></tr>` : ""}`;
    }).join("")}</tbody></table></div>
    <div class="pie-tabla">${numero(lista.length)} pedido(s)</div>`;
  cont.querySelectorAll("[data-abrir]").forEach((b) => (b.onclick = () => { const id = b.dataset.abrir; abiertos.has(id) ? abiertos.delete(id) : abiertos.add(id); dibujar(); }));
  enlazarFiltro(cont);
}

function enlazarFiltro(cont) { //cambia el filtro del historial
  cont.querySelector("#filtro")?.addEventListener("change", (e) => { filtro = e.target.value; abiertos = new Set(); cargar(); });
}
