# Test directo sin swagger
Invoke-WebRequest "http://localhost:5000/api/admin/health" -ErrorAction SilentlyContinue | Select-Object StatusCode, Content

# Si devuelve 200, el controller existe pero swagger no lo muestra
# Si devuelve 404, el controller no se cargó