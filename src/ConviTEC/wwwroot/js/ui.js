// ConviTEC · utilidades de interfaz compartidas por todas las vistas

const fmtNumero = new Intl.NumberFormat("es-CR", { maximumFractionDigits: 0, useGrouping: "always" });
const fmtDecimal = new Intl.NumberFormat("es-CR", { minimumFractionDigits: 2, maximumFractionDigits: 2, useGrouping: "always" });

export const colones = (n) => `₡${fmtNumero.format(n ?? 0)}`; //₡12 500
export const numero = (n) => fmtNumero.format(n ?? 0);
export const rendimiento = (n) => `₡${fmtDecimal.format(n ?? 0)}/min`; //₡142,86/min
export const minutos = (n) => `${fmtNumero.format(n ?? 0)} min`;

export function fecha(iso) { //21/11/2026 · 10:42 a. m.
  if (!iso) return "";
  const d = new Date(iso);
  const dia = d.toLocaleDateString("es-CR", { day: "2-digit", month: "2-digit", year: "numeric" });
  const hora = d.toLocaleTimeString("es-CR", { hour: "numeric", minute: "2-digit" });
  return `${dia} · ${hora}`;
}

export function horaActual() { //10:42 a. m.
  return new Date().toLocaleTimeString("es-CR", { hour: "numeric", minute: "2-digit" });
}

export const esc = (t) => String(t ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]); //evita inyectar HTML

export const icono = (nombre, clase = "") => `<svg class="icono ${clase}" aria-hidden="true"><use href="img/iconos.svg#${nombre}"></use></svg>`;

const ESTADOS = { //insignia de estado: siempre ícono + texto
  Pendiente: ["pendiente", "reloj"], Entregado: ["entregado", "check"], Cancelado: ["cancelado", "cerrar"],
  Rechazado: ["rechazado", "alerta-triangulo"], Reservado: ["reservado", "caja"],
};
export function insignia(estado) {
  const [clase, ic] = ESTADOS[estado] || ["reservado", "info-circulo"];
  return `<span class="insignia ${clase}">${icono(ic, "p")}${esc(estado)}</span>`;
}

export const insigniaSede = (codigo, actual) => `<span class="insignia-sede${actual ? " actual" : ""}">${esc(codigo)}</span>`;

export function encabezado(miga, titulo, descripcion = "", acciones = "") { //Encabezado/Pagina
  return `<header class="encabezado"><div><div class="migas">ConviTEC / ${esc(miga)}</div><h1>${esc(titulo)}</h1>${descripcion ? `<p class="desc">${esc(descripcion)}</p>` : ""}</div><div class="acciones">${acciones}</div></header>`;
}

export function metrica(titulo, cifra, sub = "", ic = "", destacada = false) { //Tarjeta/Metrica
  return `<div class="tarjeta metrica${destacada ? " destacada" : ""}"><div class="titulo">${ic ? icono(ic) : ""}${esc(titulo)}</div><div class="cifra">${esc(cifra)}</div>${sub ? `<div class="sub">${esc(sub)}</div>` : ""}</div>`;
}

export function vacio(ic, titulo, texto, accionHtml = "") { //EstadoVacio
  return `<div class="vacio">${icono(ic, "g")}<h3>${esc(titulo)}</h3><p>${esc(texto)}</p>${accionHtml}</div>`;
}

export const cargando = (texto = "Cargando…") => `<div class="cargando" role="status">${icono("recargar", "girando")}<span>${esc(texto)}</span></div>`;
export const esqueleto = (filas = 4) => `<div aria-hidden="true">${'<div class="esqueleto"></div>'.repeat(filas)}</div>`;

export function alerta(tipo, titulo, texto = "", accionHtml = "") { //Alerta en la página
  const ic = { info: "info-circulo", exito: "check", advertencia: "alerta-triangulo", error: "error-circulo" }[tipo];
  return `<div class="alerta ${tipo}" role="${tipo === "error" ? "alert" : "status"}">${icono(ic)}<div class="cuerpo"><strong>${esc(titulo)}</strong>${texto ? `<span class="pequeno">${esc(texto)}</span>` : ""}</div>${accionHtml}</div>`;
}

export function errorCarga(error, reintentar) { //alerta de error con botón de reintento
  const id = `reintentar-${Math.random().toString(36).slice(2)}`;
  setTimeout(() => document.getElementById(id)?.addEventListener("click", reintentar));
  return alerta("error", "No se pudo cargar la información", error.message, `<button class="boton secundario s" id="${id}">${icono("recargar")}Reintentar</button>`);
}

export function toast(tipo, mensaje) { //aviso flotante que desaparece solo
  const ic = { exito: "check", error: "error-circulo", info: "info-circulo" }[tipo];
  const el = document.createElement("div");
  el.className = `toast ${tipo}`;
  el.setAttribute("role", "status");
  el.innerHTML = `${icono(ic)}<div class="cuerpo">${esc(mensaje)}</div><button class="boton-icono" aria-label="Cerrar aviso">${icono("cerrar", "p")}</button>`;
  el.querySelector("button").onclick = () => el.remove();
  document.getElementById("toasts").appendChild(el);
  setTimeout(() => el.remove(), 6000);
}

export function modal({ tipo = "informativo", titulo, cuerpo, acciones = [{ texto: "Aceptar", clase: "primario" }] }) { //Modal con promesa
  return new Promise((resolver) => {
    const ic = { exito: "check", error: "error-circulo", confirmacion: "alerta-triangulo" }[tipo];
    const fondo = document.createElement("div");
    fondo.className = "fondo-modal";
    fondo.innerHTML = `<div class="modal ${tipo}" role="dialog" aria-modal="true" aria-labelledby="titulo-modal">
      <div class="modal-cab">${ic ? icono(ic) : ""}<h2 id="titulo-modal">${esc(titulo)}</h2><button class="boton-icono" data-i="-1" aria-label="Cerrar">${icono("cerrar")}</button></div>
      <div class="modal-cuerpo">${cuerpo}</div>
      <div class="modal-pie">${acciones.map((a, i) => `<button class="boton ${a.clase || "secundario"}" data-i="${i}">${esc(a.texto)}</button>`).join("")}</div></div>`;
    const cerrar = (i) => { fondo.remove(); document.removeEventListener("keydown", teclas); resolver(i); };
    const teclas = (e) => { if (e.key === "Escape") cerrar(-1); };
    fondo.addEventListener("click", (e) => { const b = e.target.closest("[data-i]"); if (b) cerrar(Number(b.dataset.i)); else if (e.target === fondo) cerrar(-1); });
    document.addEventListener("keydown", teclas);
    document.body.appendChild(fondo);
    (fondo.querySelector(".modal-pie .primario, .modal-pie .peligro") || fondo.querySelector(".modal-pie button"))?.focus();
  });
}

export function numerico(id, valor, min, max, etiqueta) { //Campo/Numerico (− valor +)
  return `<div class="numerico" data-numerico="${id}"><button type="button" class="boton-icono" data-paso="-1" aria-label="Disminuir ${esc(etiqueta)}" ${valor <= min ? "disabled" : ""}>${icono("menos")}</button><input type="number" inputmode="numeric" aria-label="${esc(etiqueta)}" value="${valor}" min="${min}" max="${max}"><button type="button" class="boton-icono" data-paso="1" aria-label="Aumentar ${esc(etiqueta)}" ${valor >= max ? "disabled" : ""}>${icono("mas")}</button></div>`;
}

export function enlazarNumerico(raiz, alCambiar) { //conecta los botones − / + con su campo
  raiz.querySelectorAll("[data-numerico]").forEach((caja) => {
    const input = caja.querySelector("input");
    const min = Number(input.min), max = Number(input.max);
    const fijar = (v) => {
      const n = Math.min(max, Math.max(min, Number.isFinite(v) ? Math.round(v) : min));
      input.value = n;
      caja.querySelector('[data-paso="-1"]').disabled = n <= min;
      caja.querySelector('[data-paso="1"]').disabled = n >= max;
      alCambiar?.(caja.dataset.numerico, n);
    };
    caja.querySelectorAll("[data-paso]").forEach((b) => (b.onclick = () => fijar(Number(input.value) + Number(b.dataset.paso))));
    input.onchange = () => fijar(Number(input.value));
  });
}
