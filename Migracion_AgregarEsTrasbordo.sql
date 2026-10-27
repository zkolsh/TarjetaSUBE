-- Migración para agregar la columna EsTrasbordo a la tabla Boleto
-- Esta migración permite registrar los trasbordos de manera explícita en la base de datos.
ALTER TABLE "Boleto" ADD COLUMN "EsTrasbordo" INTEGER NOT NULL DEFAULT 0;
