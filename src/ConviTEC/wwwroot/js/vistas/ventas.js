// Pantalla 7 · Ventas y estadísticas: historial, producto estrella y peores pedidos
import { api } from "../api.js";
import { encabezado, colones, numero, minutos, rendimiento, esc, icono, cargando, errorCarga, vacio, metrica, fecha } from "../ui.js";

let abiertos = new Set(); //ventas expandidas
let datos = null;

export async function mostrar(contenido) {
  abiertos = new Set();
  contenido.innerHTML = encabezado("Ventas y estadísticas", "Ventas y estadísticas", "Pedidos entregados por esta sede y sus indicadores.") + cargando();
  try {
    const [ventas, est] = await Promise.all([api.ventas(), api.estadisticas()]);
    datos = { ventas, est };
    dibujar(contenido);
  } catch (e) {
    contenido.innerHTML = encabezado("Ventas y estadísticas", "Ventas y estadísticas") + errorCarga(e, () => mostrar(contenido));
  }
}

function dibujar(contenido) {
  const { ventas, est } = datos;
  const estrella = est.productoEstrella;
  const cab = encabezado("Ventas y estadísticas", "Ventas y estadísticas", "Pedidos entregados por esta sede y sus indicadores.");
  if (!ventas.length) { contenido.innerHTML = cab + `<section class="tarjeta">${vacio("grafica", "Esta sede todavía no tiene ventas registradas", "Las ventas se registran cuando el vehículo entrega un pedido.", '<a class="boton secundario" href="#/rutas">Ir a rutas de entrega</a>')}</section>`; return; }
  contenido.innerHTML = cab + `
    <div class="rejilla r-3">
      ${metrica("Ingresos totales", colones(est.ingresosTotales), "Suma de todas las ventas de la sede", "grafica")}
      ${metrica("Producto estrella", estrella ? estrella.nombre : "—", estrella ? `${numero(estrella.unidades)} unidades × ${colones(estrella.precioUnitario)} = ${colones(estrella.ingresos)}` : "", "estrella", true)}
      ${metrica("Pedidos entregados", numero(est.pedidosEntregados), "Ventas registradas", "check")}
    </div>
    <section class="tarjeta" style="margin-top:24px">
      <div class="panel-cab" style="flex-wrap:wrap"><h3>Peores pedidos (menor rendimiento)</h3>
        <span class="formula">Rendimiento = <span class="frac"><span>Monto de venta (₡)</span><span>Tiempo de traslado (min)</span></span></span></div>
      ${est.peoresPedidos.length ? `<div class="tabla-env"><table><thead><tr><th>#</th><th>Pedido</th><th class="der">Monto</th><th class="der">Tiempo de traslado</th><th class="der">Rendimiento</th></tr></thead>
        <tbody>${est.peoresPedidos.map((p, i) => `<tr class="${i % 2 ? "fila-par" : ""}"><td>${i + 1}</td><td><strong>${esc(p.id)}</strong></td><td class="der num">${colones(p.monto)}</td><td class="der num">${minutos(p.tiempo)}</td><td class="der num"><strong>${rendimiento(p.rendimiento)}</strong></td></tr>`).join("")}</tbody></table></div>` : `<div class="panel-cuerpo secundario">Sin datos.</div>`}
    </section>
    <section class="tarjeta" style="margin-top:24px">
      <div class="panel-cab"><h3>Historial de ventas</h3><span class="pequeno secundario">${numero(ventas.length)} venta(s)</span></div>
      <div class="tabla-env"><table><thead><tr><th><span class="sr">Expandir</span></th><th>Fecha de entrega</th><th>Pedido</th><th>Destino</th><th>Productos</th><th class="der">Unidades</th><th class="der">Monto total</th></tr></thead>
      <tbody>${ventas.map((v, i) => {
        const abierto = abiertos.has(v.pedidoId);
        return `<tr class="${i % 2 ? "fila-par" : ""}${abierto ? " expandida" : ""}">
          <td style="width:52px"><button class="boton-icono" data-abrir="${esc(v.pedidoId)}" aria-expanded="${abierto}" aria-label="${abierto ? "Ocultar" : "Ver"} detalle de ${esc(v.pedidoId)}">${icono(abierto ? "chevron-abajo" : "chevron-derecha")}</button></td>
          <td>${esc(fecha(v.fecha))}</td><td><strong>${esc(v.pedidoId)}</strong></td><td>${esc(v.destino)}</td>
          <td style="max-width:280px">${esc(v.lineas.map((l) => l.nombre).join(", "))}</td>
          <td class="der num">${numero(v.lineas.reduce((s, l) => s + l.cantidad, 0))}</td><td class="der num"><strong>${colones(v.total)}</strong></td></tr>
          ${abierto ? `<tr class="detalle"><td colspan="7"><table><thead><tr><th>Producto</th><th class="der">Cantidad</th><th class="der">Precio unitario</th><th class="der">Subtotal</th></tr></thead>
            <tbody>${v.lineas.map((l) => `<tr><td>${esc(l.nombre)} <span class="secundario">· ${esc(l.codigo)}</span></td><td class="der num">${numero(l.cantidad)}</td><td class="der num">${colones(l.precioUnitario)}</td><td class="der num">${colones(l.cantidad * l.precioUnitario)}</td></tr>`).join("")}
            <tr><td colspan="3" class="der secundario">Tiempo de traslado: ${minutos(v.tiempoTraslado)}</td><td class="der num"><strong>${colones(v.total)}</strong></td></tr></tbody></table></td></tr>` : ""}`;
      }).join("")}</tbody></table></div>
    </section>`;
  contenido.querySelectorAll("[data-abrir]").forEach((b) => (b.onclick = () => { const id = b.dataset.abrir; abiertos.has(id) ? abiertos.delete(id) : abiertos.add(id); dibujar(contenido); }));
}
