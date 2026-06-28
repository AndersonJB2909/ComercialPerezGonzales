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

        // Migración: agregar columna fecha_caducidad si no existe
        try
        {
            using var m = conn.CreateCommand();
            m.CommandText = "ALTER TABLE productos ADD COLUMN fecha_caducidad TEXT";
            m.ExecuteNonQuery();
        }
        catch { /* columna ya existe */ }

        // Migración: tabla de conversiones de productos
        try
        {
            using var m2 = conn.CreateCommand();
            m2.CommandText = @"
        CREATE TABLE IF NOT EXISTS producto_conversiones (
            id               INTEGER PRIMARY KEY AUTOINCREMENT,
            producto_id      INTEGER NOT NULL UNIQUE,
            producto_base_id INTEGER NOT NULL,
            factor           REAL    NOT NULL CHECK (factor > 0),
            FOREIGN KEY (producto_id)      REFERENCES productos(id),
            FOREIGN KEY (producto_base_id) REFERENCES productos(id),
            CHECK (producto_id != producto_base_id)
        );
        CREATE INDEX IF NOT EXISTS idx_conv_base ON producto_conversiones(producto_base_id);";
            m2.ExecuteNonQuery();
        }
        catch { /* tabla ya existe */ }

        // Migración: Limpieza de duplicados de Cliente General
        try
        {
            using var m3 = conn.CreateCommand();
            m3.CommandText = @"
            -- Reasignar ventas al primer Cliente General si estaban usando uno de los duplicados
            UPDATE ventas 
            SET cliente_id = (SELECT MIN(id) FROM clientes WHERE nombre = 'Cliente' AND apellido = 'General')
            WHERE cliente_id IN (
                SELECT id FROM clientes WHERE nombre = 'Cliente' AND apellido = 'General'
            );
            
            -- Eliminar los duplicados
            DELETE FROM clientes 
            WHERE nombre = 'Cliente' AND apellido = 'General' 
              AND id > (SELECT MIN(id) FROM clientes WHERE nombre = 'Cliente' AND apellido = 'General');
              
            -- Insertar si no existe
            INSERT INTO clientes (nombre, apellido) 
            SELECT 'Cliente', 'General'
            WHERE NOT EXISTS (SELECT 1 FROM clientes WHERE nombre = 'Cliente' AND apellido = 'General');
            ";
            m3.ExecuteNonQuery();
        }
        catch { /* error ignorado */ }

        // Migración: Actualizar check constraint de devoluciones para permitir TRANSFERENCIA
        try
        {
            string schemaSql = "";
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT sql FROM sqlite_schema WHERE name = 'devoluciones'";
                schemaSql = checkCmd.ExecuteScalar()?.ToString() ?? "";
            }

            if (!schemaSql.Contains("TRANSFERENCIA"))
            {
                using var mDev = conn.CreateCommand();
                mDev.CommandText = @"
                    PRAGMA foreign_keys = OFF;
                    
                    CREATE TABLE devoluciones_new (
                        id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                        venta_id            INTEGER NOT NULL REFERENCES ventas(id),
                        cierre_caja_id      INTEGER NOT NULL REFERENCES cierres_caja(id),
                        fecha_hora          TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                        motivo              TEXT    NOT NULL,
                        monto_subtotal      REAL    NOT NULL,
                        monto_descuento     REAL    NOT NULL DEFAULT 0,
                        monto_impuesto      REAL    NOT NULL DEFAULT 0,
                        monto_total         REAL    NOT NULL,
                        metodo_reembolso    TEXT    NOT NULL CHECK(metodo_reembolso IN ('EFECTIVO', 'NOTA_CREDITO', 'TARJETA', 'TRANSFERENCIA')),
                        supervisor_autorizo TEXT    NOT NULL,
                        cajero_solicito     TEXT    NOT NULL,
                        nota_credito_codigo TEXT
                    );

                    INSERT INTO devoluciones_new (id, venta_id, cierre_caja_id, fecha_hora, motivo, monto_subtotal, monto_descuento, monto_impuesto, monto_total, metodo_reembolso, supervisor_autorizo, cajero_solicito, nota_credito_codigo)
                    SELECT id, venta_id, cierre_caja_id, fecha_hora, motivo, monto_subtotal, monto_descuento, monto_impuesto, monto_total, metodo_reembolso, supervisor_autorizo, cajero_solicito, nota_credito_codigo FROM devoluciones;

                    DROP TABLE devoluciones;
                    ALTER TABLE devoluciones_new RENAME TO devoluciones;

                    CREATE INDEX IF NOT EXISTS idx_devoluciones_venta ON devoluciones(venta_id);
                    
                    PRAGMA foreign_keys = ON;
                ";
                mDev.ExecuteNonQuery();
            }
        }
        catch { /* error ignorado */ }

        // Limpieza de cotizaciones vencidas (mayores a 15 días)
        try
        {
            using var m4 = conn.CreateCommand();
            m4.CommandText = "DELETE FROM ventas WHERE estado = 'COTIZACION' AND datetime(created_at) < datetime('now', 'localtime', '-15 days');";
            m4.ExecuteNonQuery();
        }
        catch { /* error ignorado */ }

        // Migración: Módulo de Proveedores Avanzado
        try
        {
            using var m5 = conn.CreateCommand();
            m5.CommandText = @"
                ALTER TABLE proveedores ADD COLUMN documento_fiscal TEXT;
                ALTER TABLE proveedores ADD COLUMN contacto_nombre TEXT;
                ALTER TABLE proveedores ADD COLUMN contacto_telefono TEXT;
                ALTER TABLE proveedores ADD COLUMN contacto_email TEXT;
                ALTER TABLE proveedores ADD COLUMN limite_credito REAL NOT NULL DEFAULT 0;
                ALTER TABLE proveedores ADD COLUMN metodo_pago_preferido TEXT NOT NULL DEFAULT 'EFECTIVO';
            ";
            m5.ExecuteNonQuery();
        }
        catch { /* columnas ya existen */ }

        try
        {
            using var m6 = conn.CreateCommand();
            m6.CommandText = @"
                CREATE TABLE IF NOT EXISTS ordenes_compra (
                    id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    numero          TEXT    NOT NULL UNIQUE,
                    proveedor_id    INTEGER NOT NULL REFERENCES proveedores(id),
                    estado          TEXT    NOT NULL DEFAULT 'BORRADOR' CHECK(estado IN ('BORRADOR', 'ENVIADA', 'RECIBIDA_PARCIAL', 'RECIBIDA_COMPLETA', 'CANCELADA')),
                    fecha_emision   TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                    fecha_esperada  TEXT,
                    notas           TEXT,
                    created_at      TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
                );

                CREATE TABLE IF NOT EXISTS detalle_ordenes_compra (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    orden_compra_id     INTEGER NOT NULL REFERENCES ordenes_compra(id) ON DELETE CASCADE,
                    producto_id         INTEGER NOT NULL REFERENCES productos(id),
                    cantidad_solicitada REAL    NOT NULL CHECK(cantidad_solicitada > 0),
                    cantidad_recibida   REAL    NOT NULL DEFAULT 0,
                    costo_unitario      REAL    NOT NULL
                );

                CREATE TABLE IF NOT EXISTS facturas_compras (
                    id               INTEGER PRIMARY KEY AUTOINCREMENT,
                    orden_compra_id  INTEGER REFERENCES ordenes_compra(id),
                    proveedor_id     INTEGER NOT NULL REFERENCES proveedores(id),
                    numero_factura   TEXT    NOT NULL,
                    fecha_emision    TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                    subtotal         REAL    NOT NULL DEFAULT 0,
                    impuesto         REAL    NOT NULL DEFAULT 0,
                    total            REAL    NOT NULL DEFAULT 0,
                    saldo_pendiente  REAL    NOT NULL DEFAULT 0,
                    estado           TEXT    NOT NULL DEFAULT 'PENDIENTE' CHECK(estado IN ('PAGADA', 'PENDIENTE', 'VENCIDA')),
                    created_at       TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
                );

                CREATE TABLE IF NOT EXISTS pagos_proveedores (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    factura_compra_id   INTEGER NOT NULL REFERENCES facturas_compras(id),
                    cierre_caja_id      INTEGER REFERENCES cierres_caja(id),
                    fecha_pago          TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                    monto               REAL    NOT NULL CHECK(monto > 0),
                    metodo_pago         TEXT    NOT NULL,
                    referencia          TEXT,
                    usuario_nombre      TEXT
                );

                CREATE TABLE IF NOT EXISTS producto_proveedores (
                    producto_id             INTEGER NOT NULL REFERENCES productos(id),
                    proveedor_id            INTEGER NOT NULL REFERENCES proveedores(id),
                    codigo_barra_proveedor  TEXT,
                    PRIMARY KEY (producto_id, proveedor_id)
                );
            ";
            m6.ExecuteNonQuery();
        }
        catch { /* error ignorado */ }
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
            fecha_caducidad TEXT,
            activo        INTEGER NOT NULL DEFAULT 1,
            created_at    TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
            updated_at    TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
        );

        CREATE TABLE IF NOT EXISTS unidades_medida (
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            nombre     TEXT    NOT NULL UNIQUE
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

        CREATE TABLE IF NOT EXISTS proveedores (
            id                    INTEGER PRIMARY KEY AUTOINCREMENT,
            documento             TEXT    UNIQUE,
            documento_fiscal      TEXT,
            nombre                TEXT    NOT NULL,
            telefono              TEXT,
            email                 TEXT,
            direccion             TEXT,
            contacto_nombre       TEXT,
            contacto_telefono     TEXT,
            contacto_email        TEXT,
            dias_credito          INTEGER NOT NULL DEFAULT 0,
            limite_credito        REAL    NOT NULL DEFAULT 0,
            condiciones_pago      TEXT,
            metodo_pago_preferido TEXT    NOT NULL DEFAULT 'EFECTIVO',
            activo                INTEGER NOT NULL DEFAULT 1,
            created_at            TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
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

        CREATE TABLE IF NOT EXISTS devoluciones (
            id                  INTEGER PRIMARY KEY AUTOINCREMENT,
            venta_id            INTEGER NOT NULL REFERENCES ventas(id),
            cierre_caja_id      INTEGER NOT NULL REFERENCES cierres_caja(id),
            fecha_hora          TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
            motivo              TEXT    NOT NULL,
            monto_subtotal      REAL    NOT NULL,
            monto_descuento     REAL    NOT NULL DEFAULT 0,
            monto_impuesto      REAL    NOT NULL DEFAULT 0,
            monto_total         REAL    NOT NULL,
            metodo_reembolso    TEXT    NOT NULL CHECK(metodo_reembolso IN ('EFECTIVO', 'NOTA_CREDITO', 'TARJETA', 'TRANSFERENCIA')),
            supervisor_autorizo TEXT    NOT NULL,
            cajero_solicito     TEXT    NOT NULL,
            nota_credito_codigo TEXT
        );

        CREATE TABLE IF NOT EXISTS detalle_devoluciones (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            devolucion_id INTEGER NOT NULL REFERENCES devoluciones(id) ON DELETE CASCADE,
            producto_id   INTEGER NOT NULL REFERENCES productos(id),
            cantidad      REAL    NOT NULL,
            precio_unit   REAL    NOT NULL,
            subtotal      REAL    NOT NULL,
            estado_producto TEXT  NOT NULL CHECK(estado_producto IN ('STOCK', 'MERMA'))
        );

        CREATE TABLE IF NOT EXISTS notas_credito (
            id               INTEGER PRIMARY KEY AUTOINCREMENT,
            codigo           TEXT    NOT NULL UNIQUE,
            cliente_id       INTEGER REFERENCES clientes(id),
            monto_inicial    REAL    NOT NULL,
            monto_disponible REAL    NOT NULL,
            estado           TEXT    NOT NULL DEFAULT 'ACTIVA' CHECK(estado IN ('ACTIVA','USADA','VENCIDA')),
            fecha_emision    TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
            fecha_vencimiento TEXT   NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_devoluciones_venta ON devoluciones(venta_id);
        CREATE INDEX IF NOT EXISTS idx_devoluciones_cierre ON devoluciones(cierre_caja_id);
        CREATE INDEX IF NOT EXISTS idx_notas_credito_codigo ON notas_credito(codigo);

        CREATE INDEX IF NOT EXISTS idx_ordenes_compra_prov ON ordenes_compra(proveedor_id);
        CREATE INDEX IF NOT EXISTS idx_facturas_compras_prov ON facturas_compras(proveedor_id);
        CREATE INDEX IF NOT EXISTS idx_pagos_proveedores_factura ON pagos_proveedores(factura_compra_id);

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

        INSERT OR IGNORE INTO unidades_medida (nombre) VALUES 
            ('UND'), ('KG'), ('LT'), ('MT'), ('CAJA'), ('PAR'), ('PAQ');

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
        INSERT OR IGNORE INTO configuracion VALUES ('supervisor_pin',       '1234',                      'STRING',  'SEGURIDAD', 'PIN de supervisor para devoluciones');
        INSERT OR IGNORE INTO configuracion VALUES ('pos_password',         'admin123',                  'STRING',  'SEGURIDAD', 'Contraseña alfanumérica para iniciar el sistema');
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
