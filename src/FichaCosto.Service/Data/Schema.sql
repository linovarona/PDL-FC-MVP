-- =============================================
-- Schema MVP FichaCosto Service
-- Resoluciones 148/2023 y 209/2024
-- SQLite 3
-- =============================================;

PRAGMA foreign_keys = ON;

-- =============================================
-- 1. TABLA: Clientes
-- =============================================;
CREATE TABLE IF NOT EXISTS Clientes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    NombreEmpresa TEXT NOT NULL,
    CUIT TEXT NOT NULL UNIQUE CHECK(length(CUIT) = 11),
    Direccion TEXT,
    ContactoNombre TEXT,
    ContactoEmail TEXT,
    ContactoTelefono TEXT,
    Activo INTEGER NOT NULL DEFAULT 1 CHECK(Activo IN (0, 1)),
    FechaAlta TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS IX_Clientes_CUIT ON Clientes(CUIT);
CREATE INDEX IF NOT EXISTS IX_Clientes_Activo ON Clientes(Activo);

-- =============================================
-- 2. TABLA: Productos
-- =============================================;
CREATE TABLE IF NOT EXISTS Productos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ClienteId INTEGER NOT NULL,
    Codigo TEXT NOT NULL,
    Nombre TEXT NOT NULL,
    Descripcion TEXT,
    UnidadMedida INTEGER NOT NULL DEFAULT 5 CHECK(UnidadMedida BETWEEN 1 AND 9),
    Activo INTEGER NOT NULL DEFAULT 1 CHECK(Activo IN (0, 1)),
    FechaCreacion TEXT NOT NULL DEFAULT (datetime('now')),
    FechaModificacion TEXT,
    
    FOREIGN KEY (ClienteId) REFERENCES Clientes(Id) ON DELETE CASCADE,
    UNIQUE(ClienteId, Codigo)
);

CREATE INDEX IF NOT EXISTS IX_Productos_ClienteId ON Productos(ClienteId);
CREATE INDEX IF NOT EXISTS IX_Productos_Activo ON Productos(Activo);

-- =============================================
-- 3. TABLA: MateriasPrimas
-- =============================================;
CREATE TABLE IF NOT EXISTS MateriasPrimas (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductoId INTEGER NOT NULL,
    Nombre TEXT NOT NULL,
    CodigoInterno TEXT,
    Cantidad DECIMAL(18,4) NOT NULL CHECK(Cantidad > 0),
    CostoUnitario DECIMAL(18,4) NOT NULL CHECK(CostoUnitario >= 0),
    Observaciones TEXT,
    Orden INTEGER NOT NULL DEFAULT 0,
    Activo INTEGER NOT NULL DEFAULT 1 CHECK(Activo IN (0, 1)),
    
    FOREIGN KEY (ProductoId) REFERENCES Productos(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_MateriasPrimas_ProductoId ON MateriasPrimas(ProductoId);
CREATE INDEX IF NOT EXISTS IX_MateriasPrimas_Activo ON MateriasPrimas(Activo);

-- =============================================
-- 4. TABLA: ManoObraDirecta
-- =============================================;
CREATE TABLE IF NOT EXISTS ManoObraDirecta (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductoId INTEGER NOT NULL UNIQUE,
    Horas DECIMAL(18,2) NOT NULL CHECK(Horas > 0),
    SalarioHora DECIMAL(18,4) NOT NULL CHECK(SalarioHora > 0),
    PorcentajeCargasSociales DECIMAL(18,2) NOT NULL DEFAULT 35.5 CHECK(PorcentajeCargasSociales >= 0),
    DescripcionTarea TEXT,
    
    FOREIGN KEY (ProductoId) REFERENCES Productos(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_ManoObraDirecta_ProductoId ON ManoObraDirecta(ProductoId);

-- =============================================
-- 5. TABLA: FichasCosto
-- =============================================;
CREATE TABLE IF NOT EXISTS FichasCosto (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductoId INTEGER NOT NULL,
    FechaCalculo TEXT NOT NULL DEFAULT (datetime('now')),
    CostoMateriasPrimas DECIMAL(18,4) NOT NULL CHECK(CostoMateriasPrimas >= 0),
    CostoManoObra DECIMAL(18,4) NOT NULL CHECK(CostoManoObra >= 0),
    CostosDirectosTotales DECIMAL(18,4) NOT NULL CHECK(CostosDirectosTotales >= 0),
    MargenUtilidad DECIMAL(18,2) NOT NULL CHECK(MargenUtilidad >= 0),
    PrecioVentaCalculado DECIMAL(18,4) NOT NULL CHECK(PrecioVentaCalculado >= 0),
    EstadoValidacion INTEGER NOT NULL DEFAULT 1 CHECK(EstadoValidacion BETWEEN 1 AND 4),
    ObservacionesValidacion TEXT,
    NumeroResolucionAplicada TEXT DEFAULT '209/2024',
    GeneradoPor TEXT DEFAULT 'Sistema',
    VersionCalculo TEXT DEFAULT '1.0.0-MVP',
    CostoTotal DECIMAL(18,4) NOT NULL CHECK(CostoTotal >= 0),
	PrecioVentaSugerido DECIMAL(18,4) NOT NULL CHECK(PrecioVentaSugerido >= 0),
	Observaciones TEXT,
	CalculadoPor TEXT,

	

    
    FOREIGN KEY (ProductoId) REFERENCES Productos(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_FichasCosto_ProductoId ON FichasCosto(ProductoId);
CREATE INDEX IF NOT EXISTS IX_FichasCosto_FechaCalculo ON FichasCosto(FechaCalculo);
CREATE INDEX IF NOT EXISTS IX_FichasCosto_EstadoValidacion ON FichasCosto(EstadoValidacion);

-- =============================================
-- VISTAS DE AYUDA
-- =============================================;

-- Vista de productos con costos calculados más recientes;
CREATE VIEW IF NOT EXISTS vw_ProductosUltimoCosto AS
SELECT 
    p.Id AS ProductoId,
    p.Codigo,
    p.Nombre,
    c.NombreEmpresa AS Cliente,
    p.UnidadMedida,
    (
        SELECT COALESCE(SUM(mp.Cantidad * mp.CostoUnitario), 0)
        FROM MateriasPrimas mp
        WHERE mp.ProductoId = p.Id AND mp.Activo = 1
    ) AS CostoMateriasPrimasActual,
    (
        SELECT COALESCE(mo.Horas * mo.SalarioHora * (1 + mo.PorcentajeCargasSociales/100), 0)
        FROM ManoObraDirecta mo
        WHERE mo.ProductoId = p.Id
    ) AS CostoManoObraActual,
    p.Activo
FROM Productos p
JOIN Clientes c ON p.ClienteId = c.Id
WHERE p.Activo = 1;