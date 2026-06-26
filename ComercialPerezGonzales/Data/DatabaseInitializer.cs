using Microsoft.Data.Sqlite;

namespace ComercialPerezGonzales.Data;

public class DatabaseInitializer
{
    private readonly DatabaseContext _context;

    public DatabaseInitializer(DatabaseContext context)
    {
        _context = context;
    }

    public void Initialize()
    {
        using var conn = _context.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = GetSchema();
        cmd.ExecuteNonQuery();

        // Migración: agregar columna imagen_data si no existe
        try
        {
            using var m = conn.CreateCommand();
            m.CommandText = "ALTER TABLE productos ADD COLUMN imagen_data BLOB";
            m.ExecuteNonQuery();
        }
        catch { /* columna ya existe */ }
    }

    private static string GetSchema() => @"
        PRAGMA foreign_keys = ON;
        PRAGMA journal_mode = WAL;

        CREATE TABLE IF NOT EXISTS categorias (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            nombre      TEXT    NOT NULL UNIQUE,
            descripcion TEXT,
            activo      INTEGER NOT NULL DEFAULT 1,
            created_at  TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
        );

        CREATE TABLE IF NOT EXISTS productos (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            codigo        TEXT    NOT NULL UNIQUE,
            nombre        TEXT    NOT NULL,
            descripcion   TEXT,
            precio_venta  REAL    NOT NULL DEFAULT 0,
            precio_costo  REAL    NOT NULL DEFAULT 0,
            stock         REAL    NOT NULL DEFAULT 0,
            stock_minimo  REAL    NOT NULL DEFAULT 0,
            categoria_id  INTEGER REFERENCES categorias(id),
            unidad_medida TEXT    NOT NULL DEFAULT 'UND',
            imagen_path   TEXT,
            activo        INTEGER NOT NULL DEFAULT 1,
            created_at    TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
            updated_at    TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
        );

        CREATE TABLE IF NOT EXISTS clientes (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            codigo     TEXT    UNIQUE,
            nombre     TEXT    NOT NULL,
            apellido   TEXT,
            documento  TEXT    UNIQUE,
            telefono   TEXT,
            email      TEXT,
            direccion  TEXT,
            activo     INTEGER NOT NULL DEFAULT 1,
            created_at TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
        );

        CREATE TABLE IF NOT EXISTS ventas (
            id              INTEGER PRIMARY KEY AUTOINCREMENT,
            numero          TEXT    NOT NULL UNIQUE,
            cliente_id      INTEGER REFERENCES clientes(id),
            subtotal        REAL    NOT NULL DEFAULT 0,
            descuento       REAL    NOT NULL DEFAULT 0,
            impuesto        REAL    NOT NULL DEFAULT 0,
            total           REAL    NOT NULL DEFAULT 0,
            metodo_pago     TEXT    NOT NULL DEFAULT 'EFECTIVO',
            monto_recibido  REAL    NOT NULL DEFAULT 0,
            cambio          REAL    NOT NULL DEFAULT 0,
            estado          TEXT    NOT NULL DEFAULT 'COMPLETADA',
            notas           TEXT,
            created_at      TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
        );

        CREATE TABLE IF NOT EXISTS detalle_ventas (
            id           INTEGER PRIMARY KEY AUTOINCREMENT,
            venta_id     INTEGER NOT NULL REFERENCES ventas(id) ON DELETE CASCADE,
            producto_id  INTEGER NOT NULL REFERENCES productos(id),
            cantidad     REAL    NOT NULL,
            precio_unit  REAL    NOT NULL,
            descuento    REAL    NOT NULL DEFAULT 0,
            subtotal     REAL    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS configuracion (
            clave       TEXT PRIMARY KEY,
            valor       TEXT,
            tipo        TEXT NOT NULL DEFAULT 'STRING',
            grupo       TEXT NOT NULL DEFAULT 'GENERAL',
            descripcion TEXT
        );

        CREATE INDEX IF NOT EXISTS idx_productos_codigo    ON productos(codigo);
        CREATE INDEX IF NOT EXISTS idx_productos_categoria ON productos(categoria_id);
        CREATE INDEX IF NOT EXISTS idx_ventas_fecha        ON ventas(created_at);
        CREATE INDEX IF NOT EXISTS idx_ventas_cliente      ON ventas(cliente_id);
        CREATE INDEX IF NOT EXISTS idx_detalle_venta_id    ON detalle_ventas(venta_id);

        CREATE TABLE IF NOT EXISTS cierres_caja (
            id                   INTEGER PRIMARY KEY AUTOINCREMENT,
            fecha_jornada        TEXT    NOT NULL UNIQUE,
            fecha_apertura       TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
            fecha_cierre         TEXT,
            fondo_inicial        REAL    NOT NULL DEFAULT 0,
            total_efectivo       REAL    NOT NULL DEFAULT 0,
            total_tarjetas       REAL    NOT NULL DEFAULT 0,
            total_transferencias REAL    NOT NULL DEFAULT 0,
            total_bruto          REAL    NOT NULL DEFAULT 0,
            total_descuentos     REAL    NOT NULL DEFAULT 0,
            total_impuesto       REAL    NOT NULL DEFAULT 0,
            total_neto           REAL    NOT NULL DEFAULT 0,
            salidas_efectivo     REAL    NOT NULL DEFAULT 0,
            entradas_extra       REAL    NOT NULL DEFAULT 0,
            efectivo_esperado    REAL,
            efectivo_real        REAL,
            diferencia           REAL,
            cantidad_ventas      INTEGER NOT NULL DEFAULT 0,
            estado               TEXT    NOT NULL DEFAULT 'ABIERTO' CHECK(estado IN ('ABIERTO','CERRADO')),
            observaciones        TEXT,
            usuario_apertura     TEXT,
            usuario_cierre       TEXT
        );

        CREATE TABLE IF NOT EXISTS movimientos_caja (
            id             INTEGER PRIMARY KEY AUTOINCREMENT,
            cierre_caja_id INTEGER NOT NULL REFERENCES cierres_caja(id),
            fecha_hora     TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
            tipo           TEXT    NOT NULL CHECK(tipo IN ('ENTRADA','SALIDA')),
            concepto       TEXT    NOT NULL,
            monto          REAL    NOT NULL CHECK(monto > 0),
            referencia     TEXT,
            usuario_nombre TEXT
        );

        CREATE TABLE IF NOT EXISTS kardex (
            id               INTEGER PRIMARY KEY AUTOINCREMENT,
            producto_id      INTEGER NOT NULL REFERENCES productos(id),
            fecha_hora       TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
            tipo_movimiento  TEXT    NOT NULL CHECK(tipo_movimiento IN ('ENTRADA_COMPRA','SALIDA_VENTA','AJUSTE_POS','AJUSTE_NEG','DEVOLUCION')),
            cantidad         REAL    NOT NULL CHECK(cantidad > 0),
            costo_unitario   REAL,
            stock_resultante REAL    NOT NULL,
            referencia_id    INTEGER,
            referencia_tipo  TEXT,
            notas            TEXT
        );

        CREATE UNIQUE INDEX IF NOT EXISTS idx_kardex_ref
            ON kardex(referencia_id, referencia_tipo) WHERE referencia_id IS NOT NULL;
        CREATE INDEX IF NOT EXISTS idx_kardex_producto
            ON kardex(producto_id, fecha_hora);
        CREATE INDEX IF NOT EXISTS idx_movimientos_cierre
            ON movimientos_caja(cierre_caja_id);

        CREATE TRIGGER IF NOT EXISTS trg_bloquear_venta_dia_cerrado
        BEFORE INSERT ON ventas
        BEGIN
            SELECT CASE WHEN (
                SELECT COUNT(*) FROM cierres_caja
                WHERE fecha_jornada = date(NEW.created_at)
                  AND estado = 'CERRADO'
            ) > 0
            THEN RAISE(ABORT, 'Jornada cerrada: no se permiten ventas en esta fecha')
            END;
        END;

        INSERT OR IGNORE INTO categorias (nombre) VALUES ('General');
        INSERT OR IGNORE INTO categorias (nombre) VALUES ('Bebidas');
        INSERT OR IGNORE INTO categorias (nombre) VALUES ('Snacks');
        INSERT OR IGNORE INTO categorias (nombre) VALUES ('Limpieza');

        INSERT OR IGNORE INTO clientes (nombre, apellido) VALUES ('Cliente', 'General');

        INSERT OR IGNORE INTO productos (codigo, nombre, precio_venta, precio_costo, stock, stock_minimo, unidad_medida, categoria_id)
        VALUES
            ('001', 'Agua mineral 500ml',        1200,  600,  50, 10, 'UND', (SELECT id FROM categorias WHERE nombre='Bebidas')),
            ('002', 'Coca-Cola 350ml',            1500,  900,  40, 10, 'UND', (SELECT id FROM categorias WHERE nombre='Bebidas')),
            ('003', 'Jugo de naranja 1L',         2200, 1400,  30,  5, 'UND', (SELECT id FROM categorias WHERE nombre='Bebidas')),
            ('004', 'Papas fritas 100g',          1800, 1000,  60, 15, 'UND', (SELECT id FROM categorias WHERE nombre='Snacks')),
            ('005', 'Galletas chocolate 200g',    2500, 1500,  45, 10, 'UND', (SELECT id FROM categorias WHERE nombre='Snacks')),
            ('006', 'Maní salado 150g',           1600,  900,  35, 10, 'UND', (SELECT id FROM categorias WHERE nombre='Snacks')),
            ('007', 'Detergente líquido 1L',      4500, 2800,  20,  5, 'UND', (SELECT id FROM categorias WHERE nombre='Limpieza')),
            ('008', 'Jabón de manos 250ml',       2800, 1600,  25,  5, 'UND', (SELECT id FROM categorias WHERE nombre='Limpieza')),
            ('009', 'Papel higiénico x4',         3200, 2000,  30,  8, 'PAQ', (SELECT id FROM categorias WHERE nombre='Limpieza')),
            ('010', 'Azúcar 1kg',                 1900, 1200,  40, 10, 'KG',  (SELECT id FROM categorias WHERE nombre='General'));

        UPDATE productos SET precio_venta = 1200, precio_costo =  600, stock = 50, stock_minimo = 10 WHERE codigo = '001' AND precio_venta = 0;
        UPDATE productos SET precio_venta = 1500, precio_costo =  900, stock = 40, stock_minimo = 10 WHERE codigo = '002' AND precio_venta = 0;
        UPDATE productos SET precio_venta = 2200, precio_costo = 1400, stock = 30, stock_minimo =  5 WHERE codigo = '003' AND precio_venta = 0;
        UPDATE productos SET precio_venta = 1800, precio_costo = 1000, stock = 60, stock_minimo = 15 WHERE codigo = '004' AND precio_venta = 0;
        UPDATE productos SET precio_venta = 2500, precio_costo = 1500, stock = 45, stock_minimo = 10 WHERE codigo = '005' AND precio_venta = 0;
        UPDATE productos SET precio_venta = 1600, precio_costo =  900, stock = 35, stock_minimo = 10 WHERE codigo = '006' AND precio_venta = 0;
        UPDATE productos SET precio_venta = 4500, precio_costo = 2800, stock = 20, stock_minimo =  5 WHERE codigo = '007' AND precio_venta = 0;
        UPDATE productos SET precio_venta = 2800, precio_costo = 1600, stock = 25, stock_minimo =  5 WHERE codigo = '008' AND precio_venta = 0;
        UPDATE productos SET precio_venta = 3200, precio_costo = 2000, stock = 30, stock_minimo =  8 WHERE codigo = '009' AND precio_venta = 0;
        UPDATE productos SET precio_venta = 1900, precio_costo = 1200, stock = 40, stock_minimo = 10 WHERE codigo = '010' AND precio_venta = 0;

        INSERT OR IGNORE INTO configuracion VALUES ('negocio_nombre',       'Comercial Perez Gonzales', 'STRING',  'NEGOCIO',   'Nombre del negocio');
        INSERT OR IGNORE INTO configuracion VALUES ('negocio_rut',          '',                          'STRING',  'NEGOCIO',   'RUT o identificacion fiscal');
        INSERT OR IGNORE INTO configuracion VALUES ('negocio_direccion',    '',                          'STRING',  'NEGOCIO',   'Direccion del negocio');
        INSERT OR IGNORE INTO configuracion VALUES ('negocio_telefono',     '',                          'STRING',  'NEGOCIO',   'Telefono');
        INSERT OR IGNORE INTO configuracion VALUES ('moneda_simbolo',       '$',                         'STRING',  'FACTURA',   'Simbolo de moneda');
        INSERT OR IGNORE INTO configuracion VALUES ('impuesto_porcentaje',  '0',                         'DECIMAL', 'FACTURA',   'Porcentaje de impuesto');
        INSERT OR IGNORE INTO configuracion VALUES ('imprimir_ticket',      'false',  'BOOL',    'IMPRESION', 'Imprimir ticket al completar venta');
        INSERT OR IGNORE INTO configuracion VALUES ('usuario_nombre',       '',       'STRING',  'NEGOCIO',   'Nombre del cajero/usuario que aparece en el recibo');
        INSERT OR IGNORE INTO configuracion VALUES ('caja_fondo_inicial',  '0',      'DECIMAL', 'CAJA',      'Fondo de caja al inicio del día');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_impresora',        '',       'STRING',  'IMPRESION', 'Nombre de la impresora seleccionada');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_papel',            '58mm',   'STRING',  'IMPRESION', 'Tamaño de papel: 58mm, 80mm, A4, Carta');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_copias',           '1',      'INT',     'IMPRESION', 'Número de copias por venta');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_margen_arriba',    '0',      'INT',     'IMPRESION', 'Margen superior en mm');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_margen_abajo',     '0',      'INT',     'IMPRESION', 'Margen inferior en mm');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_margen_izquierda', '0',      'INT',     'IMPRESION', 'Margen izquierdo en mm');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_margen_derecha',   '0',      'INT',     'IMPRESION', 'Margen derecho en mm');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_encabezado',       '',       'TEXT',    'IMPRESION', 'Texto de encabezado del recibo');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_pie',              '',       'TEXT',    'IMPRESION', 'Texto de pie de página del recibo');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_fuente_familia',   '(Predeterminada)', 'STRING', 'IMPRESION', 'Familia tipográfica del recibo');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_fuente_tamano',    '100',    'INT',     'IMPRESION', 'Tamaño de fuente en porcentaje (50-150)');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_codigo_barras',    'false',  'BOOL',    'IMPRESION', 'Imprimir código de barras');
        INSERT OR IGNORE INTO configuracion VALUES ('imp_logo_ancho',       'false',  'BOOL',    'IMPRESION', 'Imprimir logo a ancho completo');
    ";
}
