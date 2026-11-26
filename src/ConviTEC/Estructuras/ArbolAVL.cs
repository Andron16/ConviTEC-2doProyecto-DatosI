using System; //IComparable

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
    private NodoAVL<TClave, TValor> raiz; //nodo superior del árbol
    private int cantidad; //número de nodos
    private bool cambio; //lo marcan Insertar/Eliminar recursivos cuando realmente agregaron o quitaron un nodo

    public int Cantidad => cantidad; //número de nodos

    public bool Insertar(TClave clave, TValor valor) //false si la clave ya existe (no duplica)
    {
        cambio = false;
        raiz = Insertar(raiz, clave, valor);
        if (cambio) cantidad++;
        return cambio;
    }

    public TValor Buscar(TClave clave) //default si no existe
    {
        NodoAVL<TClave, TValor> nodo = BuscarNodo(clave);
        return nodo == null ? default : nodo.Valor;
    }

    public bool Contiene(TClave clave) => BuscarNodo(clave) != null; //true si la clave existe

    public bool Eliminar(TClave clave) //false si no existe; rebalancea
    {
        cambio = false;
        raiz = Eliminar(raiz, clave);
        if (cambio) cantidad--;
        return cambio;
    }

    public TValor[] EnOrden() //valores ordenados por clave
    {
        TValor[] valores = new TValor[cantidad];
        int indice = 0;
        EnOrden(raiz, valores, ref indice);
        return valores;
    }

    public int Altura() => AlturaDe(raiz); //altura del árbol (0 si está vacío)

    // Recursión
    private NodoAVL<TClave, TValor> Insertar(NodoAVL<TClave, TValor> nodo, TClave clave, TValor valor) //devuelve la nueva raíz del subárbol
    {
        if (nodo == null) { cambio = true; return new NodoAVL<TClave, TValor>(clave, valor); } //lugar encontrado: nueva hoja

        int comparacion = clave.CompareTo(nodo.Clave);
        if (comparacion < 0) nodo.Izquierdo = Insertar(nodo.Izquierdo, clave, valor);
        else if (comparacion > 0) nodo.Derecho = Insertar(nodo.Derecho, clave, valor);
        else return nodo; //duplicado: no se inserta ni se cambia el valor

        return Balancear(nodo);
    }

    private NodoAVL<TClave, TValor> Eliminar(NodoAVL<TClave, TValor> nodo, TClave clave) //devuelve la nueva raíz del subárbol
    {
        if (nodo == null) return null; //la clave no existe

        int comparacion = clave.CompareTo(nodo.Clave);
        if (comparacion < 0) nodo.Izquierdo = Eliminar(nodo.Izquierdo, clave);
        else if (comparacion > 0) nodo.Derecho = Eliminar(nodo.Derecho, clave);
        else
        {
            cambio = true;
            if (nodo.Izquierdo == null) return nodo.Derecho; //0 o 1 hijo: lo reemplaza su hijo
            if (nodo.Derecho == null) return nodo.Izquierdo;

            NodoAVL<TClave, TValor> sucesor = nodo.Derecho; //2 hijos: se copia el menor del subárbol derecho
            while (sucesor.Izquierdo != null) sucesor = sucesor.Izquierdo;
            nodo.Clave = sucesor.Clave;
            nodo.Valor = sucesor.Valor;
            nodo.Derecho = Eliminar(nodo.Derecho, sucesor.Clave); //y se borra el sucesor de su posición original
        }
        return Balancear(nodo);
    }

    private void EnOrden(NodoAVL<TClave, TValor> nodo, TValor[] valores, ref int indice) //izquierdo → nodo → derecho
    {
        if (nodo == null) return;
        EnOrden(nodo.Izquierdo, valores, ref indice);
        valores[indice++] = nodo.Valor;
        EnOrden(nodo.Derecho, valores, ref indice);
    }

    private NodoAVL<TClave, TValor> BuscarNodo(TClave clave) //búsqueda binaria iterativa; null si no existe
    {
        if (clave == null) return null;
        NodoAVL<TClave, TValor> actual = raiz;
        while (actual != null)
        {
            int comparacion = clave.CompareTo(actual.Clave);
            if (comparacion == 0) return actual;
            actual = comparacion < 0 ? actual.Izquierdo : actual.Derecho;
        }
        return null;
    }

    // Balanceo
    private static int AlturaDe(NodoAVL<TClave, TValor> nodo) => nodo == null ? 0 : nodo.Altura; //vacío = 0, hoja = 1

    private static void ActualizarAltura(NodoAVL<TClave, TValor> nodo) //1 + la altura del hijo más alto
    {
        nodo.Altura = 1 + Math.Max(AlturaDe(nodo.Izquierdo), AlturaDe(nodo.Derecho));
    }

    private static int FactorBalance(NodoAVL<TClave, TValor> nodo) => AlturaDe(nodo.Izquierdo) - AlturaDe(nodo.Derecho); //> 1 cargado a la izquierda, < -1 a la derecha

    private static NodoAVL<TClave, TValor> Balancear(NodoAVL<TClave, TValor> nodo) //actualiza la altura y aplica la rotación que haga falta
    {
        ActualizarAltura(nodo);
        int balance = FactorBalance(nodo);

        if (balance > 1) //pesado a la izquierda
        {
            if (FactorBalance(nodo.Izquierdo) < 0) nodo.Izquierdo = RotarIzquierda(nodo.Izquierdo); //caso izquierda-derecha: rotación doble
            return RotarDerecha(nodo); //caso izquierda-izquierda: rotación simple
        }
        if (balance < -1) //pesado a la derecha
        {
            if (FactorBalance(nodo.Derecho) > 0) nodo.Derecho = RotarDerecha(nodo.Derecho); //caso derecha-izquierda: rotación doble
            return RotarIzquierda(nodo); //caso derecha-derecha: rotación simple
        }
        return nodo; //ya está balanceado
    }

    private static NodoAVL<TClave, TValor> RotarDerecha(NodoAVL<TClave, TValor> y) //el hijo izquierdo sube y 'y' baja a la derecha
    {
        NodoAVL<TClave, TValor> x = y.Izquierdo;
        y.Izquierdo = x.Derecho;
        x.Derecho = y;
        ActualizarAltura(y); //primero 'y', que quedó abajo
        ActualizarAltura(x);
        return x;
    }

    private static NodoAVL<TClave, TValor> RotarIzquierda(NodoAVL<TClave, TValor> x) //el hijo derecho sube y 'x' baja a la izquierda
    {
        NodoAVL<TClave, TValor> y = x.Derecho;
        x.Derecho = y.Izquierdo;
        y.Izquierdo = x;
        ActualizarAltura(x); //primero 'x', que quedó abajo
        ActualizarAltura(y);
        return y;
    }
}