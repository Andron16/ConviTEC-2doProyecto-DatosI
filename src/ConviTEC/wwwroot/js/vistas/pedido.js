// Pantalla 2 · Realizar pedido: catálogo global + detalle del pedido
import { api } from "../api.js";
import { encabezado, colones, numero, minutos, esc, icono, cargando, errorCarga, vacio, modal, toast, numerico, enlazarNumerico } from "../ui.js";

let datos = null; //{ sedes, productos } del catálogo global
let visibles = null; //productos mostrados (todos o el resultado de una búsqueda)
let destinos = [];
let carrito = []; //líneas { codigo, nombre, precio, cantidad, max }
let destino = "";
let enviando = false;
let sedeActual = "";

export async function mostrar(contenido, estado) {
  sedeActual = estado.sede?.codigo || "";
  contenido.innerHTML = encabezado("Realizar pedido", "Realizar pedido", "Seleccione productos del catálogo global, sus cantidades y el punto de entrega.") +
    `<div class="rejilla r-pedido"><section class="tarjeta" id="cat">${cargando("Cargando catálogo…")}</section><section class="tarjeta" id="det"></section></div>`;
  dibujarDetalle();
  try {
    [datos, destinos] = await Promise.all([api.catalogo(), api.destinos()]);
    visibles = datos.productos;
    carrito.forEach((l) => { const p = datos.productos.find((x) => x.codigo === l.codigo); l.max = p ? p.total : 0; }); //ajusta máximos si cambió el inventario
    dibujarCatalogo();
    dibujarDetalle();
  } catch (e) {
    document.getElementById("cat").innerHTML = `<div class="panel-cuerpo">${errorCarga(e, () => mostrar(contenido, estado))}</div>`;
  }
}

function dibujarCatalogo(mensaje = "") { //panel izquierdo
  const cat = document.getElementById("cat");
  if (!cat) return;
  const sedes = datos.sedes;
  cat.innerHTML = `
    <div class="panel-cab"><h3>Catálogo global</h3><span class="pequeno secundario">${numero(datos.productos.length)} productos</span></div>
    <div class="panel-cuerpo" style="padding-bottom:16px">
      <form class="buscador" id="f-buscar">
        <div class="campo"><label for="q">Buscar producto por código</label><div class="control">${icono("buscar")}<input id="q" placeholder="Ej. C001" autocomplete="off"></div></div>
        <button class="boton secundario">Buscar</button>
        ${visibles !== datos.productos ? '<button type="button" class="boton fantasma" id="todo">Mostrar todo</button>' : ""}
      </form>
    </div>
    ${mensaje}
    ${visibles.length ? `<div class="tabla-env"><table class="compacta">
      <thead><tr><th>Producto</th><th class="der">Precio</th>
        <th><div class="sedes-mini cab" style="--n:${sedes.length}">${sedes.map((s) => `<span>${esc(s.codigo)}</span>`).join("")}</div></th>
        <th class="der">Total</th><th><span class="sr">Acción</span></th></tr></thead>
      <tbody>${visibles.map((p, i) => {
        const enCarrito = carrito.find((l) => l.codigo === p.codigo);
        const lleno = p.total === 0 || (enCarrito && enCarrito.cantidad >= p.total);
        return `<tr class="${i % 2 ? "fila-par" : ""}${p.total === 0 ? " agotada" : ""}">
          <td><strong>${esc(p.nombre)}</strong><span class="sub">${esc(p.codigo)} · ${esc(p.categoria)}</span></td>
          <td class="der num">${colones(p.precio)}</td>
          <td><div class="sedes-mini" style="--n:${sedes.length}">${p.cantidades.map((c, j) => `<span class="${sedes[j].codigo === sedeActual ? "actual" : ""}${c === 0 ? " cero" : ""}">${numero(c)}</span>`).join("")}</div></td>
          <td class="der num">${p.total === 0 ? `<span class="agotado">${icono("alerta-triangulo", "p")}Agotado</span>` : `<strong>${numero(p.total)}</strong>`}</td>
          <td class="der"><button class="boton secundario s" data-agregar="${esc(p.codigo)}" ${lleno ? "disabled" : ""}>${icono("mas", "p")}Agregar</button></td></tr>`;
      }).join("")}</tbody></table></div>` : ""}`;
  cat.querySelector("#f-buscar").onsubmit = buscar;
  cat.querySelector("#todo")?.addEventListener("click", () => { visibles = datos.productos; dibujarCatalogo(); });
  cat.querySelectorAll("[data-agregar]").forEach((b) => (b.onclick = () => agregar(b.dataset.agregar)));
}

async function buscar(e) { //búsqueda por código en el backend
  e.preventDefault();
  const q = document.getElementById("q").value.trim();
  if (!q) { visibles = datos.productos; dibujarCatalogo(); return; }
  try {
    const r = await api.productoCatalogo(q);
    visibles = r.productos;
    dibujarCatalogo();
  } catch (err) {
    visibles = [];
    dibujarCatalogo(`<div class="panel-cuerpo" style="padding-top:0">${vacio("buscar", "Producto no encontrado", err.message)}</div>`);
  }
}

function agregar(codigo) { //suma una unidad o crea la línea
  const p = datos.productos.find((x) => x.codigo === codigo);
  const l = carrito.find((x) => x.codigo === codigo);
  if (l) l.cantidad = Math.min(l.cantidad + 1, p.total);
  else carrito.push({ codigo, nombre: p.nombre, precio: p.precio, cantidad: 1, max: p.total });
  dibujarCatalogo();
  dibujarDetalle();
}

function dibujarDetalle() { //panel derecho: líneas, destino, total
  const det = document.getElementById("det");
  if (!det) return;
  const total = carrito.reduce((s, l) => s + l.precio * l.cantidad, 0);
  const listo = carrito.length > 0 && destino && carrito.every((l) => l.cantidad <= l.max);
  const motivo = !carrito.length ? "Agregue al menos un producto." : !destino ? "Seleccione un punto de entrega." : "";
  det.innerHTML = `
    <div class="panel-cab"><h3>Detalle del pedido</h3>${carrito.length ? `<button class="boton fantasma s" id="vaciar">${icono("basurero", "p")}Vaciar</button>` : ""}</div>
    <div class="panel-cuerpo">
      ${carrito.length ? carrito.map((l) => `
        <div class="linea-pedido${l.cantidad > l.max ? " sin" : ""}">
          <div class="nombre"><strong>${esc(l.nombre)}</strong><div class="pequeno secundario">${esc(l.codigo)} · ${colones(l.precio)} c/u</div></div>
          ${numerico(l.codigo, l.cantidad, 1, Math.max(1, l.max), `cantidad de ${l.nombre}`)}
          <div class="precio"><span class="secundario">Subtotal</span><strong class="num">${colones(l.precio * l.cantidad)}</strong></div>
          <button class="boton-icono" data-quitar="${esc(l.codigo)}" aria-label="Quitar ${esc(l.nombre)}">${icono("basurero")}</button>
          ${l.cantidad > l.max ? `<div class="aviso">${icono("alerta-triangulo", "p")}Solo hay ${numero(l.max)} disponibles en la red de sedes</div>` : ""}
        </div>`).join("") : vacio("carrito", "El pedido está vacío", "Agregue productos desde el catálogo global.")}
      <div class="campo" style="margin-top:24px">
        <label for="destino">Punto de entrega</label>
        <div class="control"><select id="destino"><option value="">Seleccione un destino</option>${destinos.map((d) => `<option ${d === destino ? "selected" : ""}>${esc(d)}</option>`).join("")}</select></div>
      </div>
      <div class="total-pedido"><span class="secundario">Total del pedido</span><strong>${colones(total)}</strong></div>
      <button class="boton primario" id="registrar" style="width:100%" ${!listo || enviando ? "disabled" : ""}>
        ${enviando ? `${icono("recargar", "girando")}Procesando…` : `${icono("check")}Validar y registrar pedido`}</button>
      ${motivo && !enviando ? `<p class="pequeno secundario" style="margin-top:8px;text-align:center">${motivo}</p>` : ""}
    </div>`;
  enlazarNumerico(det, (codigo, n) => { carrito.find((l) => l.codigo === codigo).cantidad = n; dibujarDetalle(); dibujarCatalogo(); });
  det.querySelectorAll("[data-quitar]").forEach((b) => (b.onclick = () => { carrito = carrito.filter((l) => l.codigo !== b.dataset.quitar); dibujarDetalle(); dibujarCatalogo(); }));
  det.querySelector("#vaciar")?.addEventListener("click", () => { carrito = []; dibujarDetalle(); dibujarCatalogo(); });
  det.querySelector("#destino").onchange = (e) => { destino = e.target.value; dibujarDetalle(); };
  det.querySelector("#registrar").onclick = registrar;
}

async function registrar() { //envía el pedido y muestra aceptación o rechazo
  enviando = true;
  dibujarDetalle();
  try {
    const r = await api.registrarPedido(destino, carrito.map((l) => ({ codigo: l.codigo, cantidad: l.cantidad })));
    if (r.aceptado) {
      const porSede = {}; //agrupa los aportes por sede
      r.aportes.forEach((a) => (porSede[a.sede] ||= []).push(`${numero(a.cantidad)} × ${esc(a.nombre)}`));
      await modal({ tipo: "exito", titulo: `Pedido ${r.pedido.id} registrado`, cuerpo: `
        <p><strong>Total:</strong> ${colones(r.pedido.total)} · <strong>Destino:</strong> ${esc(r.pedido.destino)}</p>
        <p style="margin-top:12px"><strong>Sedes que aportan productos:</strong></p>
        <ul class="lista-simple">${Object.entries(porSede).map(([s, items]) => `<li><strong>${esc(s)}:</strong> ${items.join(", ")}</li>`).join("")}</ul>
        <p style="margin-top:12px"><strong>Tiempo de traslado desde la sede:</strong> ${minutos(r.tiempoEstimado)}</p>
        <p class="pequeno secundario" style="margin-top:12px">El pedido quedó pendiente de entrega.</p>` });
      carrito = []; destino = "";
      toast("exito", `Pedido ${r.pedido.id} registrado como pendiente de entrega`);
    } else {
      await modal({ tipo: "error", titulo: "Pedido rechazado", cuerpo: `<p>${esc(r.motivo)}</p>${r.reservasLiberadas?.length ? `<p class="pequeno secundario" style="margin-top:12px">Se liberaron las reservas en: ${r.reservasLiberadas.map(esc).join(", ")}.</p>` : ""}` });
    }
  } catch (e) {
    await modal({ tipo: "error", titulo: "No se pudo registrar el pedido", cuerpo: `<p>${esc(e.message)}</p>` });
  } finally {
    enviando = false;
    try { datos = await api.catalogo(); visibles = datos.productos; carrito.forEach((l) => { const p = datos.productos.find((x) => x.codigo === l.codigo); l.max = p ? p.total : 0; }); dibujarCatalogo(); } catch { /* se mantiene la vista anterior */ }
    dibujarDetalle();
  }
}

export function salir() { enviando = false; } //el carrito se conserva al cambiar de pantalla
