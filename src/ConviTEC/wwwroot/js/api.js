// =====================================================================
// ConviTEC · Capa de comunicación con el backend de la sede
// ---------------------------------------------------------------------
// CONTRATO DE LA API (lo que la web espera del backend C#)
// Todas las rutas son relativas al mismo servidor que sirve esta página.
// Formato: JSON. Las listas siempre son arreglos JSON ([...]).
// Errores: código HTTP 4xx/5xx con cuerpo { "error": "mensaje para el usuario" }.
// Fechas: texto ISO 8601, por ejemplo "2026-11-21T10:42:00".
//
// GET  /api/sede
//      -> { codigo, nombre, ubicacion, conectado }
//         conectado = true si la sede tiene conexión con el intermediario de mensajes.
//
// GET  /api/inventario
//      -> [ Producto ]   (inventario local de la sede)
// GET  /api/inventario/{codigo}
//      -> Producto | 404 { error }      (búsqueda en el árbol del catálogo local)
// POST /api/inventario/unidades   cuerpo { codigo, cantidad }
//      -> Producto actualizado | 400/404 { error }
//      Producto = { codigo, nombre, descripcion, categoria, precio, disponible, reservado }
//
// GET  /api/catalogo
//      -> { sedes: [ { codigo, nombre } ],
//           productos: [ { codigo, nombre, descripcion, categoria, precio,
//                          cantidades: [ enteros en el mismo orden que "sedes" ], total } ] }
// GET  /api/catalogo/{codigo}
//      -> misma forma, con un solo producto en "productos" | 404 { error }
//
// GET  /api/destinos
//      -> [ "Heredia", "Cartago", ... ]   (ubicaciones del grafo que se pueden elegir como destino)
//
// POST /api/pedidos   cuerpo { destino, lineas: [ { codigo, cantidad } ] }
//      -> aceptado:  { aceptado: true, pedido: Pedido,
//                      aportes: [ { sede, codigo, nombre, cantidad } ], tiempoEstimado }
//      -> rechazado: { aceptado: false, motivo, reservasLiberadas: [ codigos de sede ] }
// GET  /api/pedidos?estado=todos|pendiente|entregado|cancelado|rechazado
//      -> [ Pedido ]  ordenados del más reciente al más antiguo
//      Pedido = { id, fecha, estado ("Pendiente"|"Entregado"|"Cancelado"|"Rechazado"),
//                 destino, lineas: [ { codigo, nombre, cantidad, precioUnitario } ],
//                 total, sedesParticipantes: [ codigos ], motivo (texto o null) }
//
// GET  /api/grafo
//      -> { nodos: [ { nombre, sede (código o null) } ],
//           aristas: [ { id, origen, destino, tiempo } ] }
//
// GET  /api/recorrido
// POST /api/recorrido/iniciar     cuerpo { salida: "11:00" | "15:00" }
// POST /api/recorrido/siguiente   (entrega en el destino más cercano y recalcula con Dijkstra)
//      -> Recorrido = {
//           activo, finalizado, salida, origen, ubicacionActual, tiempoTotal,
//           entregas:  [ { orden, destino, pedidos: [ids], tramo, acumulado, camino: [nombres] } ],
//           siguiente: { destino, tiempo, camino: [nombres], pedidos: [ids] } | null,
//           pendientes:[ { destino, pedidos: [ids], tiempo } ]   (tiempo = null si es inalcanzable)
//         }
//      Si no hay recorrido activo: activo = false y "pendientes" trae los pedidos pendientes de la sede.
//
// GET  /api/ventas
//      -> [ { pedidoId, fecha, destino, lineas: [ ... ], total, tiempoTraslado } ]
// GET  /api/estadisticas
//      -> { ingresosTotales, pedidosEntregados,
//           productoEstrella: { codigo, nombre, unidades, precioUnitario, ingresos } | null,
//           peoresPedidos: [ { id, monto, tiempo, rendimiento } ] }   (de menor a mayor rendimiento)
// =====================================================================

import * as simulacion from "./simulacion.js"; //datos de prueba mientras no existe el backend
import { ErrorApi } from "./errores.js";

export { ErrorApi };
export const USAR_SIMULACION = true; //cambiar a false cuando el backend tenga los endpoints

async function pedir(metodo, ruta, cuerpo) { //hace la petición HTTP y devuelve el JSON
  if (USAR_SIMULACION) return simulacion.responder(metodo, ruta, cuerpo);
  let respuesta;
  try {
    respuesta = await fetch(ruta, {
      method: metodo,
      headers: cuerpo ? { "Content-Type": "application/json" } : undefined,
      body: cuerpo ? JSON.stringify(cuerpo) : undefined,
    });
  } catch {
    throw new ErrorApi("No se pudo comunicar con el servidor de la sede. Verifique que esté en ejecución.", 0);
  }
  const texto = await respuesta.text(); //puede venir vacío
  const datos = texto ? JSON.parse(texto) : null;
  if (!respuesta.ok) throw new ErrorApi(datos?.error || `Error ${respuesta.status} en ${ruta}`, respuesta.status);
  return datos;
}

const cod = (texto) => encodeURIComponent(texto.trim()); //protege códigos en la URL

export const api = { //una función por endpoint del contrato
  sede: () => pedir("GET", "/api/sede"),
  inventario: () => pedir("GET", "/api/inventario"),
  productoLocal: (codigo) => pedir("GET", `/api/inventario/${cod(codigo)}`),
  agregarUnidades: (codigo, cantidad) => pedir("POST", "/api/inventario/unidades", { codigo, cantidad }),
  catalogo: () => pedir("GET", "/api/catalogo"),
  productoCatalogo: (codigo) => pedir("GET", `/api/catalogo/${cod(codigo)}`),
  destinos: () => pedir("GET", "/api/destinos"),
  registrarPedido: (destino, lineas) => pedir("POST", "/api/pedidos", { destino, lineas }),
  pedidos: (estado = "todos") => pedir("GET", `/api/pedidos?estado=${estado}`),
  grafo: () => pedir("GET", "/api/grafo"),
  recorrido: () => pedir("GET", "/api/recorrido"),
  iniciarRecorrido: (salida) => pedir("POST", "/api/recorrido/iniciar", { salida }),
  siguienteEntrega: () => pedir("POST", "/api/recorrido/siguiente"),
  ventas: () => pedir("GET", "/api/ventas"),
  estadisticas: () => pedir("GET", "/api/estadisticas"),
};
