export class ErrorApi extends Error { //error con mensaje listo para mostrar al usuario
  constructor(mensaje, estado) { super(mensaje); this.estado = estado; }
}
