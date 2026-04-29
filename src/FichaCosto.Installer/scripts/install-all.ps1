# 0. Preparar entorno (una vez)
cd D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Installer\scripts
.\config-extensions.ps1

# 1. Verificar todo está listo
.\verify-environment.ps1

# 2. Compilar el servicio
.\publisher.ps1

# 3. Crear MSI (aquí se corregirá lo de las DLLs)
.\compailer-msi.ps1

# 4. Crear Bundle
.\compailer-bundle.ps1

# 5. Pre-instalación (verificar destino)
.\pre-install.ps1

# 6. Instalar
.\install.ps1

# 7. Verificar instalación (detectará si BD está vacía)
#.\post-install.ps1

# 8. Si post-install dice "BD VACÍA", ejecutar:
.\seed-repair.ps1 -ForceReset

# 9. Diagnóstico final
.\diagnose.ps1