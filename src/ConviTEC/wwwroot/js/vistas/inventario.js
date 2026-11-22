// Pantalla 3 · Inventario local: productos de la sede y entrada de unidades
import { api } from "../api.js";
import { encabezado, colones, numero, esc, icono, esqueleto, errorCarga, vacio, modal, toast, numerico, enlazarNumerico } from "../ui.js";

let productos = []; //inventario local completo
let visibles = null; //lista mostrada (todos o resultado de búsqueda)
let raiz = null;

const acciones = `<button class="boton primario" id="agregar">${icono("agregar-caja")}Agregar unidades</button>`;

export async function mostrar(contenido) {
  raiz = contenido;
  contenido.innerHTML = encabezado("Inventario local", "Inventario local", "Productos almacenados en esta sede, con sus precios y existencias.", acciones) + `<section class="tarjeta" id="inv"><div class="panel-cuerpo">${esqueleto(6)}</div></section>`;
  contenido.querySelector("#agregar").onclick = abrirAgregar;
  await cargar();
}

async function cargar() { //trae el inventario completo
  try {
    productos = await api.inventario();
    visibles = productos;
    dibujar();
  } catch (e) {
    document.getElementById("inv").innerHTML = `<div class="panel-cuerpo">${errorCarga(e, cargar)}</div>`;
  }
}

function dibujar(mensaje = "") {
  const inv = document.getElementById("inv");
  if (!inv) return;
  if (!productos.length) { inv.innerHTML = vacio("caja", "Esta sede aún no tiene productos registrados", "Use «Agregar unidades» para incorporar existencias al inventario local."); return; }
  inv.innerHTML = `
    <div class="panel-cuerpo" style="padding-bottom:16px">
      <form class="buscador" id="f-buscar">
        <div class="campo"><label for="q">Buscar por código</label><div class="control">${icono("buscar")}<input id="q" placeholder="Ej. C001" autocomplete="off"></div></div>
        <button class="boton secundario">Buscar</button>
        ${visibles !== productos ? '<button type="button" class="boton fantasma" id="todo">Mostrar todo</button>' : ""}
      </form>
    </div>
    ${mensaje}
    ${visibles.length ? `<div class="tabla-env"><table>
      <thead><tr><th>Código</th><th>Nombre</th><th>Descripción</th><th>Categoría</th><th class="der">Precio</th><th class="der">Disponible</th><th class="der">Reservado</th></tr></thead>
      <tbody>${visibles.map((p, i) => `<tr class="${i % 2 ? "fila-par" : ""}${p.disponible === 0 ? " agotada" : ""}">
        <td><strong>${esc(p.codigo)}</strong></td><td>${esc(p.nombre)}</td><td>${esc(p.descripcion)}</td><td>${esc(p.categoria)}</td>
        <td class="der num">${colones(p.precio)}</td>
        <td class="der num">${p.disponible === 0 ? `<span class="agotado">${icono("alerta-triangulo", "p")}Agotado</span>` : `<strong>${numero(p.disponible)}</strong>`}</td>
        <td class="der num">${numero(p.reservado)}</td></tr>`).join("")}</tbody></table></div>
      <div class="pie-tabla">Mostrando ${numero(visibles.length)} de ${numero(productos.length)} productos</div>` : ""}`;
  inv.querySelector("#f-buscar").onsubmit = buscar;
  inv.querySelector("#todo")?.addEventListener("click", () => { visibles = productos; dibujar(); });
}

async function buscar(e) { //consulta el árbol del catálogo local en el backend
  e.preventDefault();
  const q = document.getElementById("q").value.trim();
  if (!q) { visibles = productos; dibujar(); return; }
  try {
    visibles = [await api.productoLocal(q)];
    dibujar();
  } catch (err) {
    visibles = [];
    dibujar(`<div class="panel-cuerpo" style="padding-top:0">${vacio("buscar", "Producto no encontrado", err.message)}</div>`);
  }
}

async function abrirAgregar() { //modal para incorporar unidades
  let catalogo;
  try { catalogo = (await api.catalogo()).productos; } catch { catalogo = productos; }
  let codigo = "", cantidad = 1;
  const cuerpo = `
    <div class="campo"><label for="m-prod">Producto</label><div class="control"><select id="m-prod"><option value="">Seleccione un producto</option>${catalogo.map((p) => `<option value="${esc(p.codigo)}">${esc(p.codigo)} · ${esc(p.nombre)}</option>`).join("")}</select></div><div class="msj-error" id="m-err" hidden>${icono("error-circulo", "p")}Seleccione un producto.</div></div>
    <div class="campo" style="margin-top:16px"><label>Cantidad a incorporar</label><div>${numerico("cant", 1, 1, 9999, "cantidad a incorporar")}</div></div>`;
  const promesa = modal({ titulo: "Agregar unidades", cuerpo, acciones: [{ texto: "Cancelar", clase: "secundario" }, { texto: "Guardar", clase: "primario" }] });
  const caja = document.querySelector(".fondo-modal:last-child");
  enlazarNumerico(caja, (_, n) => (cantidad = n));
  caja.querySelector("#m-prod").onchange = (e) => { codigo = e.target.value; caja.querySelector("#m-err").hidden = true; caja.querySelector(".campo").classList.remove("error"); };
  const guardar = caja.querySelector('.modal-pie [data-i="1"]');
  guardar.addEventListener("click", (e) => { //valida antes de cerrar
    if (!codigo) { e.stopPropagation(); caja.querySelector("#m-err").hidden = false; caja.querySelector(".campo").classList.add("error"); }
  }, true);
  if ((await promesa) !== 1) return;
  try {
    const p = await api.agregarUnidades(codigo, cantidad);
    toast("exito", `Se agregaron ${numero(cantidad)} unidades de ${p.nombre}. Disponible: ${numero(p.disponible)}.`);
    await cargar();
  } catch (e) {
    toast("error", e.message);
  }
}
