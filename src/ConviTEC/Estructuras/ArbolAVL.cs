using System; //NotImplementedException, IComparable

namespace ConviTEC.Estructuras;

public class NodoAVL<TClave, TValor> //nodo del árbol AVL
{
    public TClave Clave { get; set; }
    public TValor Valor { get; set; }
    public int Altura { get; set; } = 1; //altura del subárbol con raíz en este nodo
    public NodoAVL<TClave, TValor> Izquierdo { get; set; }
    public NodoAVL<TClave, TValor> Derecho { get; set; }
    public NodoAVL(TClave clave, TValor valor) { Clave = clave; Valor = valor; } //crea una hoja
}

public class ArbolAVL<TClave, TValor> where TClave : IComparable<TClave> //catálogo local indexado por código de producto
{
    public int Cantidad => throw new NotImplementedException(); //número de nodos

    public bool Insertar(TClave clave, TValor valor) => throw new NotImplementedException(); //false si la clave ya existe (no duplica)
    public TValor Buscar(TClave clave) => throw new NotImplementedException(); //default si no existe
    public bool Contiene(TClave clave) => throw new NotImplementedException(); //true si la clave existe
    public bool Eliminar(TClave clave) => throw new NotImplementedException(); //false si no existe; rebalancea
    public TValor[] EnOrden() => throw new NotImplementedException(); //valores ordenados por clave
    public int Altura() => throw new NotImplementedException(); //altura del árbol (0 si está vacío)
}