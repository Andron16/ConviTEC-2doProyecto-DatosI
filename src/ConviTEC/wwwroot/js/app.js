// ConviTEC · arranque de la aplicación: barra lateral, conexión y navegación entre pantallas
import { api } from "./api.js";
import { icono, esc, insigniaSede } from "./ui.js";
import * as inicio from "./vistas/inicio.js";
import * as pedido from "./vistas/pedido.js";
import * as inventario from "./vistas/inventario.js";
import * as catalogo from "./vistas/catalogo.js";
import * as pedidos from "./vistas/pedidos.js";
import * as rutas from "./vistas/rutas.js";
import * as ventas from "./vistas/ventas.js";

const PANTALLAS = [ //ruta (#/...), texto del menú, ícono, módulo de la vista
  ["inicio", "Inicio", "inicio", inicio],
  ["pedido", "Realizar pedido", "carrito", pedido],
  ["inventario", "Inventario local", "caja", inventario],
  ["catalogo", "Catálogo global", "globo", catalogo],
  ["pedidos", "Pedidos", "lista", pedidos],
  ["rutas", "Rutas de entrega", "camion", rutas],
  ["ventas", "Ventas y estadísticas", "grafica", ventas],
];

export const estado = { sede: null }; //datos de la sede compartidos con las vistas
let vistaActual = null; //para que la vista libere temporizadores al salir

function dibujarMenu() { //Navegacion/Item por pantalla
  document.getElementById("menu").innerHTML = PANTALLAS.map(([ruta, texto, ic]) =>
    `<a href="#/${ruta}" data-ruta="${ruta}">${icono(ic)}<span>${esc(texto)}</span></a>`).join("");
}

function dibujarConexion(conectado) { //Indicador/Conexion
  const el = document.getElementById("conexion");
  el.className = `conexion ${conectado ? "ok" : "mal"}`;
  el.innerHTML = conectado ? `${icono("conectado")}<span>Conectado a la red de sedes</span>` : `${icono("desconectado")}<span>Sin conexión con otras sedes</span>`;
}

async function actualizarSede() { //consulta /api/sede y refresca la barra lateral
  try {
    const sede = await api.sede();
    estado.sede = sede;
    document.getElementById("nombre-sede").textContent = sede.nombre;
    document.getElementById("codigo-sede").innerHTML = insigniaSede(sede.codigo, true);
    document.title = `ConviTEC · ${sede.nombre}`;
    dibujarConexion(sede.conectado);
  } catch {
    dibujarConexion(false);
  }
}

async function navegar() { //muestra la pantalla según el #/ruta de la URL
  const ruta = location.hash.replace("#/", "").split("?")[0] || "inicio";
  const pantalla = PANTALLAS.find(([r]) => r === ruta) || PANTALLAS[0];
  document.querySelectorAll("#menu a").forEach((a) => {
    const activo = a.dataset.ruta === pantalla[0];
    a.classList.toggle("activo", activo);
    if (activo) a.setAttribute("aria-current", "page"); else a.removeAttribute("aria-current");
  });
  vistaActual?.salir?.();
  vistaActual = pantalla[3];
  const contenido = document.getElementById("contenido");
  contenido.innerHTML = "";
  contenido.scrollTop = 0;
  window.scrollTo(0, 0);
  await vistaActual.mostrar(contenido, estado);
}

async function iniciar() { //punto de entrada
  dibujarMenu();
  document.getElementById("conexion").innerHTML = `${icono("recargar", "girando")}<span>Verificando conexión…</span>`;
  await actualizarSede();
  setInterval(actualizarSede, 10000); //revisa la conexión cada 10 s
  window.addEventListener("hashchange", navegar);
  navegar();
}

iniciar();
