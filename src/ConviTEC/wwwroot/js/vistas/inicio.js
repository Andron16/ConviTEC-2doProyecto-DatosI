// Pantalla 1 · Inicio: resumen de la sede
import { api } from "../api.js";
import { encabezado, metrica, colones, numero, esc, insignia, icono, cargando, errorCarga, vacio, fecha } from "../ui.js";

function proximaSalida() { //11:00 a. m. o 3:00 p. m. según la hora actual
  const h = new Date().getHours();
  if (h < 11) return "11:00 a. m.";
  if (h < 15) return "3:00 p. m.";
  return "11:00 a. m. (mañana)";
}

export async function mostrar(contenido, estado) {
  const nombre = estado.sede?.nombre || "";
  contenido.innerHTML = encabezado("Inicio", `Sede ${nombre}`, "Resumen de la operación de la sede.", `<a class="boton primario" href="#/pedido">${icono("carrito")}Realizar pedido</a>`) + cargando();
  try {
    const [pend, todos, est] = await Promise.all([api.pedidos("pendiente"), api.pedidos("todos"), api.estadisticas()]);
    const estrella = est.productoEstrella;
    const ultimos = todos.slice(0, 5);
    contenido.innerHTML = encabezado("Inicio", `Sede ${nombre}`, "Resumen de la operación de la sede.", `<a class="boton primario" href="#/pedido">${icono("carrito")}Realizar pedido</a>`) + `
      <div class="rejilla r-4">
        ${metrica("Pedidos pendientes", numero(pend.length), "Por entregar desde la sede", "reloj")}
        ${metrica("Pedidos entregados", numero(est.pedidosEntregados), "Ventas registradas", "check")}
        ${metrica("Ingresos totales", colones(est.ingresosTotales), "Suma de las ventas", "grafica")}
        ${metrica("Producto estrella", estrella ? colones(estrella.ingresos) : "—", estrella ? `${estrella.nombre} · ${numero(estrella.unidades)} unidades` : "Aún no hay ventas", "estrella", true)}
      </div>
      <div class="rejilla r-60-40" style="margin-top:24px">
        <section class="tarjeta">
          <div class="panel-cab"><h3>Últimos pedidos</h3><a class="boton fantasma s" href="#/pedidos">Ver todos</a></div>
          ${ultimos.length ? `<div class="tabla-env"><table><thead><tr><th>Pedido</th><th>Fecha</th><th>Destino</th><th class="der">Monto</th><th>Estado</th></tr></thead><tbody>
            ${ultimos.map((p, i) => `<tr class="${i % 2 ? "fila-par" : ""}"><td><strong>${esc(p.id)}</strong></td><td>${esc(fecha(p.fecha))}</td><td>${esc(p.destino)}</td><td class="der num">${colones(p.total)}</td><td>${insignia(p.estado)}</td></tr>`).join("")}
          </tbody></table></div>` : vacio("lista", "Todavía no hay pedidos", "Los pedidos registrados en esta sede aparecerán aquí.")}
        </section>
        <section class="tarjeta">
          <div class="panel-cab"><h3>Próxima salida</h3></div>
          <div class="panel-cuerpo pila" style="gap:16px">
            <div class="metrica" style="padding:0">
              <div class="titulo">${icono("camion")}Salida de las ${proximaSalida()}</div>
              <div class="cifra">${numero(pend.length)}</div>
              <div class="sub">${pend.length === 1 ? "pedido listo para entregar" : "pedidos listos para entregar"}${pend.length ? ` en ${new Set(pend.map((p) => p.destino)).size} destino(s)` : ""}</div>
            </div>
            <p class="pequeno secundario">El vehículo sale de la sede dos veces al día: 11:00 a. m. y 3:00 p. m.</p>
            <a class="boton secundario" href="#/rutas">${icono("ruta")}Planificar recorrido</a>
          </div>
        </section>
      </div>`;
  } catch (e) {
    contenido.innerHTML = encabezado("Inicio", `Sede ${nombre}`) + errorCarga(e, () => mostrar(contenido, estado));
  }
}
