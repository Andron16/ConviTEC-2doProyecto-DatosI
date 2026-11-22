// Pantalla 4 · Catálogo global: disponibilidad de cada producto en todas las sedes
import { api } from "../api.js";
import { encabezado, colones, numero, esc, icono, esqueleto, errorCarga, vacio, horaActual } from "../ui.js";

let datos = null;
let visibles = null;
let temporizador = null;
let sedeActual = "";
let busqueda = ""; //código buscado (vacío = todos)

export async function mostrar(contenido, estado) {
  sedeActual = estado.sede?.codigo || "";
  busqueda = "";
  contenido.innerHTML = encabezado("Catálogo global", "Catálogo global", "Productos de ConviTEC y su disponibilidad en cada sede.", `<button class="boton secundario" id="refrescar">${icono("recargar")}Actualizar</button>`) +
    `<section class="tarjeta" id="glob"><div class="panel-cuerpo">${esqueleto(6)}</div></section>`;
  contenido.querySelector("#refrescar").onclick = () => cargar();
  await cargar();
  temporizador = setInterval(() => cargar(true), 10000); //la copia global se actualiza con los mensajes de las sedes
}

async function cargar(silencioso = false) {
  if (silencioso && document.activeElement?.id === "q") return; //no interrumpe si el usuario está escribiendo
  try {
    datos = busqueda ? await api.productoCatalogo(busqueda) : await api.catalogo();
    visibles = datos.productos;
    dibujar();
  } catch (e) {
    if (silencioso) return;
    if (busqueda && e.estado === 404) { visibles = []; dibujar(`<div class="panel-cuerpo" style="padding-top:0">${vacio("buscar", "Producto no encontrado", e.message)}</div>`); return; }
    const g = document.getElementById("glob");
    if (g) g.innerHTML = `<div class="panel-cuerpo">${errorCarga(e, () => cargar())}</div>`;
  }
}

function dibujar(mensaje = "") {
  const g = document.getElementById("glob");
  if (!g) return;
  const valorBusqueda = document.getElementById("q")?.value ?? busqueda;
  const sedes = datos?.sedes || [];
  g.innerHTML = `
    <div class="panel-cuerpo" style="padding-bottom:16px;display:flex;justify-content:space-between;align-items:flex-end;gap:16px;flex-wrap:wrap">
      <form class="buscador" id="f-buscar" style="flex:1">
        <div class="campo"><label for="q">Buscar por código</label><div class="control">${icono("buscar")}<input id="q" placeholder="Ej. C001" autocomplete="off" value="${esc(valorBusqueda)}"></div></div>
        <button class="boton secundario">Buscar</button>
        ${busqueda ? '<button type="button" class="boton fantasma" id="todo">Mostrar todo</button>' : ""}
      </form>
      <span class="pequeno secundario">Actualizado a las ${horaActual()} · se actualiza cada 10 s</span>
    </div>
    ${mensaje}
    ${visibles.length ? `<div class="tabla-env"><table>
      <thead><tr><th>Código</th><th>Producto</th><th>Categoría</th><th class="der">Precio</th>
        ${sedes.map((s) => `<th class="der${s.codigo === sedeActual ? " col-actual" : ""}">${esc(s.nombre)}${s.codigo === sedeActual ? " (esta sede)" : ""}</th>`).join("")}
        <th class="der">Total</th></tr></thead>
      <tbody>${visibles.map((p, i) => `<tr class="${i % 2 ? "fila-par" : ""}">
        <td><strong>${esc(p.codigo)}</strong></td><td>${esc(p.nombre)}</td><td>${esc(p.categoria)}</td><td class="der num">${colones(p.precio)}</td>
        ${p.cantidades.map((c, j) => `<td class="der num${sedes[j]?.codigo === sedeActual ? " col-actual" : ""}${c === 0 && sedes[j]?.codigo !== sedeActual ? " cero" : ""}">${numero(c)}</td>`).join("")}
        <td class="der num">${p.total === 0 ? `<span class="agotado">${icono("alerta-triangulo", "p")}Agotado</span>` : `<strong>${numero(p.total)}</strong>`}</td></tr>`).join("")}</tbody></table></div>
      <div class="pie-tabla">${numero(visibles.length)} productos</div>` : ""}`;
  g.querySelector("#f-buscar").onsubmit = (e) => { e.preventDefault(); busqueda = document.getElementById("q").value.trim(); cargar(); };
  g.querySelector("#todo")?.addEventListener("click", () => { busqueda = ""; document.getElementById("q").value = ""; cargar(); });
}

export function salir() { clearInterval(temporizador); } //detiene la actualización automática
