-- ============================================================
-- SEED DATA CORREGIDO: FichaCosto MVP
-- ============================================================
PRAGMA foreign_keys = ON;

-- 1. CLIENTES
INSERT INTO Clientes (Id, NombreEmpresa, CUIT, Direccion, ContactoNombre, ContactoEmail, ContactoTelefono, Activo, FechaAlta) VALUES 
(1, 'Mercado Central "El Portal" S.R.L.', '30123456789', 'Av. Corrientes 1234, CABA', 'Carlos Rodríguez', 'admin@elportal.com.ar', '011-4567-8900', 1, datetime('now', '-6 months')),
(2, 'Almacén y Café "La Esquina" S.A.', '30876543210', 'Av. Santa Fe 567, Palermo', 'María González', 'compras@laesquina.com', '011-5678-9012', 1, datetime('now', '-4 months')),
(3, 'MiniMarket "24 Horas" E.I.R.L.', '30234567890', 'Av. Libertador 8900, Belgrano', 'Roberto Silva', 'roberto@minimarket24.com', '011-6789-0123', 1, datetime('now', '-3 months')),
(4, 'Mercado Orgánico "Verde Vida" S.A.S.', '30456789012', 'Calle Thames 245, Villa Crespo', 'Lucía Martínez', 'lucia@verdevida.com', '011-7890-1234', 1, datetime('now', '-2 months')),
(5, 'Despensa Familiar "Don Pepe" S.R.L.', '30567890123', 'Av. Rivadavia 6700, Caballito', 'José López', 'jose@donpepe.com.ar', '011-8901-2345', 1, datetime('now', '-1 month'));

-- 2. PRODUCTOS
INSERT INTO Productos (Id, ClienteId, Codigo, Nombre, Descripcion, UnidadMedida, Activo, FechaCreacion) VALUES 
(1, 1, 'CAF-001', 'Café Espresso Doble', 'Café de especialidad, 60ml, doble extracción', 5, 1, datetime('now')),
(2, 1, 'SAN-001', 'Sándwich de Pollo Completo', 'Pechuga grillada, lechuga, tomate, queso cheddar, pan ciabatta', 5, 1, datetime('now')),
(3, 1, 'JUG-001', 'Jugo de Naranja Exprimido Natural', 'Naranjas frescas exprimidas al momento, 400ml', 3, 1, datetime('now')),
(4, 2, 'DES-001', 'Desayuno Ejecutivo Americano', 'Café + Tostadas con manteca y mermelada + Jugo', 5, 1, datetime('now')),
(5, 2, 'MED-001', 'Medialunas Rellenas (2 unidades)', 'Jamón y queso, horneadas', 1, 1, datetime('now')),
(6, 2, 'CAP-001', 'Cappuccino Italiano', 'Café espresso con leche vaporizada y espuma', 5, 1, datetime('now')),
(7, 3, 'ENS-001', 'Ensalada Caesar con Pollo', 'Mix de verdes, croutons, pollo grille, aderezo caesar', 5, 1, datetime('now')),
(8, 3, 'WAF-001', 'Waffles con Dulce de Leche', '2 unidades con DDL artesanal y crema', 5, 1, datetime('now')),
(9, 3, 'LIM-001', 'Limonada Natural Jarra', 'Jarra de 1 litro, limones frescos, azúcar orgánica', 3, 1, datetime('now')),
(10, 4, 'BOW-001', 'Bowl Orgánico Quinoa', 'Quinoa, palta, tomate cherry, pollo orgánico, mix de semillas', 5, 1, datetime('now')),
(11, 4, 'SMO-001', 'Smoothie Verde Detox', 'Espinaca, manzana verde, jengibre, limón, 500ml', 3, 1, datetime('now')),
(12, 4, 'TOS-001', 'Tostada de Palta', 'Pan integral, palta aplastada, huevo poché, semillas', 5, 1, datetime('now')),
(13, 5, 'EMP-001', 'Docena de Empanadas Criollas', 'Carne cortada a cuchillo, horno, 12 unidades', 7, 1, datetime('now')),
(14, 5, 'TAR-001', 'Tarta de Jamón y Queso (porción)', 'Masa casera, pascualina, queso tybo', 5, 1, datetime('now')),
(15, 5, 'LIC-001', 'Licuado de Banana y Dulce de Leche', 'Banana, leche, DDL, crema batida', 3, 1, datetime('now'));

-- 3. MATERIAS PRIMAS
INSERT INTO MateriasPrimas (Id, ProductoId, Nombre, CodigoInterno, Cantidad, CostoUnitario, Observaciones, Orden, Activo) VALUES 
(1, 1, 'Café en Grano Especialidad (18g)', 'MP-CAF-001', 18, 0.35, 'Origen Colombia, tostado medio', 1, 1),
(2, 1, 'Agua purificada', 'MP-AGU-001', 0.1, 0.50, 'Litro desmineralizado', 2, 1),
(3, 1, 'Azúcar blanca (servicio)', 'MP-AZU-001', 10, 0.04, 'Sobres individuales', 3, 1),
(4, 2, 'Pechuga de pollo (filete)', 'MP-POL-001', 0.15, 8.50, 'Pollo fresco, peso neto', 1, 1),
(5, 2, 'Pan ciabatta individual', 'MP-PAN-001', 1, 1.20, 'Panadería artesanal', 2, 1),
(6, 2, 'Queso cheddar laminado', 'MP-QUE-001', 0.04, 4.50, '40 gramos', 3, 1),
(7, 2, 'Lechuga fresca', 'MP-VER-001', 0.05, 2.00, 'Hojas seleccionadas', 4, 1),
(8, 2, 'Tomate redondo', 'MP-TOM-001', 0.08, 1.80, '80 gramos', 5, 1),
(9, 2, 'Mayonesa y aderezos', 'MP-SAL-001', 0.02, 3.00, 'Porción', 6, 1),
(10, 3, 'Naranjas para jugo (unidad)', 'MP-NAR-001', 4, 0.75, 'Naranjas grandes, exprimibles', 1, 1),
(11, 3, 'Azúcar (opcional)', 'MP-AZU-001', 0.02, 0.04, '20 gramos', 2, 1),
(12, 4, 'Café en grano', 'MP-CAF-001', 15, 0.35, 'Porción café', 1, 1),
(13, 4, 'Pan de miga (tostadas)', 'MP-PAN-002', 0.1, 2.50, '4 rodajas', 2, 1),
(14, 4, 'Mermelada artesanal', 'MP-MER-001', 0.04, 6.00, '40 gramos', 3, 1),
(15, 4, 'Manteca', 'MP-MAN-001', 0.03, 3.50, '30 gramos', 4, 1),
(16, 4, 'Naranjas para jugo', 'MP-NAR-001', 3, 0.75, '3 naranjas', 5, 1),
(17, 10, 'Quinoa orgánica cocida', 'MP-QUI-001', 0.15, 5.00, '150 gramos cocidos', 1, 1),
(18, 10, 'Palta Hass', 'MP-PAL-001', 0.5, 3.50, 'Media unidad', 2, 1),
(19, 10, 'Tomate cherry orgánico', 'MP-TOM-002', 0.1, 6.00, '100 gramos', 3, 1),
(20, 10, 'Pechuga orgánica', 'MP-POL-002', 0.12, 12.00, '120 gramos, pollo libre', 4, 1),
(21, 10, 'Mix de semillas (chía, girasol)', 'MP-SEM-001', 0.02, 8.00, '20 gramos', 5, 1),
(22, 10, 'Aceite de oliva virgen', 'MP-ACE-001', 0.01, 15.00, '10 ml', 6, 1),
(23, 13, 'Tapas de empanadas (docena)', 'MP-TAP-001', 1, 3.50, '12 tapas caseras', 1, 1),
(24, 13, 'Carne picada especial', 'MP-CAR-001', 0.6, 6.50, '600 gramos', 2, 1),
(25, 13, 'Cebolla, morrón, huevo', 'MP-VER-002', 0.4, 2.50, 'Mix de verdues', 3, 1),
(26, 13, 'Especias y condimentos', 'MP-ESP-001', 0.05, 5.00, 'Comino, pimentón, sal', 4, 1);

-- 4. MANO DE OBRA DIRECTA
INSERT INTO ManoObraDirecta (Id, ProductoId, Horas, SalarioHora, PorcentajeCargasSociales, DescripcionTarea) VALUES 
(1, 1, 0.08, 22.00, 56.0, 'Molienda, colocación en máquina, extracción, servicio (5 min)'),
(2, 2, 0.25, 22.00, 56.0, 'Cocción pechuga, armado sándwich, emplatado (15 min)'),
(3, 3, 0.10, 20.00, 56.0, 'Exprimido manual, servicio (6 min)'),
(4, 4, 0.20, 22.00, 56.0, 'Preparación integrada desayuno (12 min)'),
(5, 5, 0.05, 18.00, 56.0, 'Calentamiento y emplado (3 min)'),
(6, 10, 0.30, 25.00, 56.0, 'Cocción quinoa, corte vegetales, armado artesanal (18 min)'),
(7, 13, 1.50, 20.00, 56.0, 'Repulgue de 12 empanadas, horneado (90 min)');

-- 5. FICHAS DE COSTO - CORREGIDAS: Exactamente 17 valores por fila
-- Columnas: Id, ProductoId, FechaCalculo, CostoMateriasPrimas, CostoManoObra, CostosDirectosTotales, MargenUtilidad, PrecioVentaCalculado, EstadoValidacion, ObservacionesValidacion, NumeroResolucionAplicada, GeneradoPor, VersionCalculo, CostoTotal, PrecioVentaSugerido, Observaciones, CalculadoPor

INSERT INTO FichasCosto VALUES 
(1, 1, datetime('now'), 6.39, 1.73, 8.12, 65.00, 23.20, 1, 'Costos validados según lista de precios proveedores vigente', '209/2024', 'Sistema', '1.0.0-MVP', 8.12, 23.20, 'Café de alta rotación, margen sugerido 65% por valor percibido', 'Admin'),
(2, 2, datetime('now'), 14.80, 8.58, 23.38, 60.00, 58.45, 1, 'Producto estrella, validado', '209/2024', 'Sistema', '1.0.0-MVP', 23.38, 58.45, 'Sugerencia: ofrecer combo con bebida', 'Admin'),
(3, 10, datetime('now'), 29.80, 11.70, 41.50, 64.00, 115.28, 1, 'Producto premium, margen justificado por calidad orgánica', '209/2024', 'Sistema', '1.0.0-MVP', 41.50, 115.28, 'Cliente objetivo: segmento ABC1 conciente de salud', 'Admin'),
(4, 13, datetime('now'), 8.65, 46.80, 55.45, 58.00, 132.02, 1, 'Producto por volumen, rotación alta los fines de semana', '209/2024', 'Sistema', '1.0.0-MVP', 55.45, 132.02, 'Sugerencia: promo 2 docenas $220', 'Admin'),
(5, 3, datetime('now'), 3.08, 1.25, 4.33, 67.00, 13.12, 1, 'Margen excelente, baja complejidad', '209/2024', 'Sistema', '1.0.0-MVP', 4.33, 13.12, 'Ideal para combos desayuno', 'Admin');