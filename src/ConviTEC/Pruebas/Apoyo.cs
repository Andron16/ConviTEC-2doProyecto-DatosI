using System; //Action, Exception, Console, Guid
using System.IO; //Path, Directory, File
using ConviTEC.Modelos; //Sede

namespace ConviTEC.Pruebas;

public class FallaPrueba : Exception { public FallaPrueba(string mensaje) : base(mensaje) { } } //una comprobación no se cumplió
public class PruebaOmitida : Exception { public PruebaOmitida(string motivo) : base(motivo) { } } //la prueba no se puede ejecutar en este equipo

public static class Verificar //comprobaciones que usan las pruebas; si no se cumplen lanzan FallaPrueba
{
    public static void Igual<T>(T esperado, T obtenido, string que) //los dos valores deben ser iguales
    {
        if (!Equals(esperado, obtenido)) throw new FallaPrueba($"{que}: se esperaba {Texto(esperado)} y se obtuvo {Texto(obtenido)}");
    }

    public static void Verdadero(bool condicion, string que) //la condición debe cumplirse
    {
        if (!condicion) throw new FallaPrueba(que);
    }

    public static void Nulo(object valor, string que) //el valor debe ser null
    {
        if (valor != null) throw new FallaPrueba($"{que}: se esperaba null");
    }

    public static void NoNulo(object valor, string que) //el valor no debe ser null
    {
        if (valor == null) throw new FallaPrueba($"{que}: se obtuvo null");
    }

    public static void Arreglo<T>(T[] esperado, T[] obtenido, string que) //mismo tamaño y mismos elementos en el mismo orden
    {
        if (obtenido == null) throw new FallaPrueba($"{que}: se obtuvo null");
        bool iguales = esperado.Length == obtenido.Length;
        for (int i = 0; iguales && i < esperado.Length; i++)
            if (!Equals(esperado[i], obtenido[i])) iguales = false;
        if (!iguales) throw new FallaPrueba($"{que}: se esperaba {Texto(esperado)} y se obtuvo {Texto(obtenido)}");
    }

    public static void Lanza<TExcepcion>(Action accion, string que) where TExcepcion : Exception //la acción debe lanzar esa excepción
    {
        try { accion(); }
        catch (Exception e)
        {
            if (e is NotImplementedException) throw; //si falta implementar, la prueba queda "pendiente"
            if (e is TExcepcion) return;
            throw new FallaPrueba($"{que}: se esperaba {typeof(TExcepcion).Name} y se lanzó {e.GetType().Name}");
        }
        throw new FallaPrueba($"{que}: se esperaba {typeof(TExcepcion).Name} y no se lanzó ninguna excepción");
    }

    private static string Texto(object valor) //texto legible de un valor o de un arreglo
    {
        if (valor == null) return "null";
        if (valor is Array arreglo)
        {
            string texto = "[";
            for (int i = 0; i < arreglo.Length; i++)
                texto += (i > 0 ? ", " : "") + Texto(arreglo.GetValue(i));
            return texto + "]";
        }
        return valor.ToString();
    }
}

public static class EjecutorPruebas //ejecuta las pruebas de cada rol y muestra el resumen
{
    private static int _correctas, _fallidas, _pendientes, _omitidas; //contadores del resumen

    public static int Ejecutar(string rol, string carpetaConfig, string hostRabbit, string usuario, string clave) //rol: "A", "B", "C", "D" o cualquier otro valor para todos; devuelve el código de salida
    {
        rol = rol == null ? "" : rol.ToUpper();
        bool todos = rol != "A" && rol != "B" && rol != "C" && rol != "D";
        Console.WriteLine("Pruebas de ConviTEC");

        if (todos || rol == "A") { Titulo("Rol A: estructuras, persistencia y ventas (Celeste)"); PruebasRolA.Ejecutar(); }
        if (todos || rol == "B") { Titulo("Rol B: grafo y rutas (Palma)"); PruebasRolB.Ejecutar(carpetaConfig); }
        if (todos || rol == "C") { Titulo("Rol C: mensajería (Abigail)"); PruebasRolC.Ejecutar(hostRabbit, usuario, clave); }
        if (todos || rol == "D") { Titulo("Rol D: servicios de inventario y pedidos (Andron)"); PruebasRolD.Ejecutar(carpetaConfig); }

        Console.WriteLine();
        Console.WriteLine($"Resumen: {_correctas} correctas, {_fallidas} fallidas, {_pendientes} pendientes, {_omitidas} omitidas");
        return _fallidas == 0 ? 0 : 1; //1 si alguna falló
    }

    public static void Prueba(string nombre, Action prueba) //ejecuta una prueba y muestra su resultado
    {
        try
        {
            prueba();
            _correctas++;
            Escribir(ConsoleColor.Green, "OK       ", nombre, null);
        }
        catch (NotImplementedException e) { _pendientes++; Escribir(ConsoleColor.Yellow, "PENDIENTE", nombre, "falta " + Metodo(e)); }
        catch (PruebaOmitida e) { _omitidas++; Escribir(ConsoleColor.DarkGray, "OMITIDA  ", nombre, e.Message); }
        catch (FallaPrueba e) { _fallidas++; Escribir(ConsoleColor.Red, "FALLA    ", nombre, e.Message); }
        catch (Exception e) { _fallidas++; Escribir(ConsoleColor.Red, "ERROR    ", nombre, $"{e.GetType().Name} en {Metodo(e)}: {e.Message}"); }
    }

    public static void Omitir(string motivo) { throw new PruebaOmitida(motivo); } //marca la prueba como omitida (no cuenta como falla)

    private static void Titulo(string texto) //encabezado de cada rol
    {
        Console.WriteLine();
        Console.WriteLine(texto);
    }

    private static void Escribir(ConsoleColor color, string estado, string nombre, string detalle) //una línea de resultado con color
    {
        Console.Write("  ");
        Console.ForegroundColor = color;
        Console.Write(estado);
        Console.ResetColor();
        Console.WriteLine(detalle == null ? $" {nombre}" : $" {nombre}  ({detalle})");
    }

    private static string Metodo(Exception e) //"ArbolAVL.Insertar": clase y método donde se lanzó la excepción
    {
        if (e.TargetSite == null || e.TargetSite.DeclaringType == null) return "?";
        string clase = e.TargetSite.DeclaringType.Name;
        int tilde = clase.IndexOf('`'); //los genéricos se llaman "ArbolAVL`2"
        if (tilde != -1) clase = clase.Substring(0, tilde);
        return clase + "." + e.TargetSite.Name;
    }
}

public static class DatosPrueba //datos pequeños y conocidos para que las pruebas no dependan de config/ ni toquen datos/
{
    public static string CarpetaTemporal() //carpeta nueva y vacía dentro de la carpeta temporal del sistema
    {
        string ruta = Path.Combine(Path.GetTempPath(), "convitec-pruebas", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ruta);
        return ruta;
    }

    public static Sede[] Sedes() //las 4 sedes en el mismo orden que config/sedes.json
    {
        return new Sede[]
        {
            new Sede { Codigo = "LIB", Nombre = "Liberia", Ubicacion = "Liberia" },
            new Sede { Codigo = "ALA", Nombre = "Alajuela", Ubicacion = "Alajuela" },
            new Sede { Codigo = "SJO", Nombre = "San José", Ubicacion = "San José" },
            new Sede { Codigo = "LIM", Nombre = "Limón", Ubicacion = "Limón" }
        };
    }

    public static string CrearConfig() //config/ de prueba: 4 sedes, 3 productos y un inventario inicial conocido
    {
        string carpeta = CarpetaTemporal();
        File.WriteAllText(Path.Combine(carpeta, "sedes.json"), """
            { "sedes": [
              { "codigo": "LIB", "nombre": "Liberia",  "ubicacion": "Liberia" },
              { "codigo": "ALA", "nombre": "Alajuela", "ubicacion": "Alajuela" },
              { "codigo": "SJO", "nombre": "San José", "ubicacion": "San José" },
              { "codigo": "LIM", "nombre": "Limón",    "ubicacion": "Limón" } ] }
            """);
        File.WriteAllText(Path.Combine(carpeta, "productos.json"), """
            { "productos": [
              { "codigo": "C001", "nombre": "Café molido 250 g",    "descripcion": "Café de tueste medio",  "categoria": "Bebidas calientes", "precio": 2800 },
              { "codigo": "G014", "nombre": "Galletas de vainilla", "descripcion": "Paquete de 6 unidades", "categoria": "Snacks",            "precio": 950 },
              { "codigo": "A003", "nombre": "Agua 600 ml",          "descripcion": "Agua purificada",       "categoria": "Bebidas",           "precio": 600 } ] }
            """);
        File.WriteAllText(Path.Combine(carpeta, "inventario-inicial.json"), """
            { "inventarios": [
              { "sede": "LIB", "existencias": [ { "codigo": "C001", "cantidad": 6 },  { "codigo": "G014", "cantidad": 3 }, { "codigo": "A003", "cantidad": 0 } ] },
              { "sede": "ALA", "existencias": [ { "codigo": "C001", "cantidad": 10 }, { "codigo": "G014", "cantidad": 5 }, { "codigo": "A003", "cantidad": 0 } ] },
              { "sede": "SJO", "existencias": [ { "codigo": "C001", "cantidad": 7 },  { "codigo": "G014", "cantidad": 2 }, { "codigo": "A003", "cantidad": 20 } ] },
              { "sede": "LIM", "existencias": [ { "codigo": "C001", "cantidad": 0 },  { "codigo": "G014", "cantidad": 8 }, { "codigo": "A003", "cantidad": 4 } ] } ] }
            """);
        return carpeta;
    }
}
