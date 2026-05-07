# DevNetControl - Guia de Despliegue a Produccion

**Ultima actualizacion**: 07 de mayo de 2026

---

## 1. Requisitos de Hardware

### Minimo (hasta 50 usuarios concurrentes)
| Recurso | Especificacion |
|---------|---------------|
| CPU | 1 core |
| RAM | 1 GB |
| Disco | 20 GB SSD |
| Red | 1 Gbps, transferencia ilimitada |
| SO | Ubuntu 22.04 LTS o superior |

### Recomendado (50-200 usuarios concurrentes)
| Recurso | Especificacion |
|---------|---------------|
| CPU | 2 cores |
| RAM | 2 GB |
| Disco | 40 GB SSD |
| Red | 1 Gbps, transferencia ilimitada |
| SO | Ubuntu 22.04 LTS o superior |

### Produccion Alta Carga (200+ usuarios)
| Recurso | Especificacion |
|---------|---------------|
| CPU | 4 cores |
| RAM | 4 GB |
| Disco | 80 GB NVMe |
| Red | 1 Gbps, transferencia ilimitada |
| SO | Ubuntu 22.04 LTS o superior |

### Analisis de Consumo de Recursos

| Componente | RAM Aproximada | CPU |
|------------|---------------|-----|
| .NET 10 Runtime (Kestrel) | 150-250 MB | Bajo (idle), medio bajo carga |
| PostgreSQL | 150-300 MB | Bajo (queries simples) |
| Sistema Operativo | 150-200 MB | Bajo |
| Nginx (reverse proxy) | 10-20 MB | Minimo |
| **Total estimado** | **~600-800 MB** | **Bajo** |

**Con 1 GB de RAM**: Funciona pero muy justo. El garbage collector de .NET puede causar pausas perceptibles bajo picos de carga. No recomendado para produccion real.

**Con 2 GB de RAM**: Holgado. Permite picos de carga sin degradacion. Recomendado como minimo para produccion.

### Nota sobre el Trafico SSH

El trafico SSH de los usuarios finales **NO pasa** por el servidor central de DevNetControl. Tu VPS central solo:
- Sirve la API REST y el frontend estatico
- Ejecuta comandos SSH puntuales hacia los nodos VPS de los tenants (crear usuario, verificar expiracion, etc.)
- Gestiona la base de datos

El consumo de ancho de banda del VPS central es **bajo**: solo trafico HTTP/API. El trafico pesado (VPN de usuarios finales) va directo a los nodos VPS de cada tenant.

---

## 2. Preparar el VPS

### 2.1 Instalar Docker

```bash
# Actualizar sistema
sudo apt update && sudo apt upgrade -y

# Instalar dependencias
sudo apt install -y ca-certificates curl gnupg

# Agregar clave oficial de Docker
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# Agregar repositorio
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Instalar Docker
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Verificar
docker --version
docker compose version

# Agregar usuario actual al grupo docker (opcional, para evitar sudo)
sudo usermod -aG docker $USER
newgrp docker
```

### 2.2 Instalar Nginx (Reverse Proxy)

```bash
sudo apt install -y nginx
sudo systemctl enable nginx
sudo systemctl start nginx
```

---

## 3. Configurar la Aplicacion

### 3.1 Subir el Proyecto

**Opcion A - Git (recomendado):**
```bash
# Clonar repositorio en el VPS
git clone https://github.com/tu-usuario/DevNetControl.git /opt/devnetcontrol
cd /opt/devnetcontrol
```

**Opcion B - Copia directa:**
```bash
# Desde tu maquina local, copiar al VPS
scp -r /ruta/local/DevNetControl/* usuario@TU_VPS_IP:/opt/devnetcontrol/
```

### 3.2 Configurar Variables de Entorno

```bash
cd /opt/devnetcontrol

# Crear archivo .env
cat > .env << 'EOF'
# JWT - CLAVE SECRETA (minimo 32 caracteres, cambiar por una aleatoria)
DEVNETCONTROL_JWT_KEY=TU_CLAVE_SECRETA_SUPER_LARGA_Y_ALEATORIA_2026

# Encriptacion AES para passwords de VPS (64 caracteres hex = 32 bytes)
DEVNETCONTROL_ENCRYPTION_KEY=0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef

# PostgreSQL
POSTGRES_PASSWORD=TU_PASSWORD_POSTGRES_SEGURA
EOF

# Generar claves aleatorias
openssl rand -base64 48 | tr -d '\n' && echo  # Para JWT_KEY
openssl rand -hex 32 && echo                     # Para ENCRYPTION_KEY
openssl rand -base64 32 | tr -d '\n' && echo     # Para POSTGRES_PASSWORD
```

### 3.3 Verificar docker-compose.yml

El archivo ya esta configurado. Verificar que el puerto expuesto sea correcto:

```yaml
services:
  api:
    ports:
      - "5000:8080"    # Cambiar si necesitas otro puerto externo
```

---

## 4. Configurar SSL con Let's Encrypt

### 4.1 Instalar Certbot

```bash
sudo apt install -y certbot python3-certbot-nginx
```

### 4.2 Configurar Dominio

Asumiendo que tu dominio es `devnetcontrol.tudominio.com`:

```bash
# Configuracion inicial de Nginx para el dominio
sudo nano /etc/nginx/sites-available/devnetcontrol
```

Contenido:
```nginx
server {
    listen 80;
    server_name devnetcontrol.tudominio.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

```bash
# Activar el sitio
sudo ln -s /etc/nginx/sites-available/devnetcontrol /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx

# Obtener certificado SSL
sudo certbot --nginx -d devnetcontrol.tudominio.com
```

Certbot actualizara automaticamente la configuracion de Nginx con SSL.

### 4.3 Auto-renovacion de SSL

```bash
# Verificar que el timer de renovacion esta activo
sudo systemctl status certbot.timer

# Probar renovacion
sudo certbot renew --dry-run
```

---

## 5. Desplegar con Docker

### 5.1 Construir y Levantar

```bash
cd /opt/devnetcontrol

# Construir imagenes
docker compose build

# Levantar servicios
docker compose up -d

# Verificar que estan corriendo
docker compose ps

# Ver logs
docker compose logs -f api
docker compose logs -f db
```

### 5.2 Verificar Funcionamiento

```bash
# Test de la API
curl -k https://devnetcontrol.tudominio.com/api/auth/test-db

# Deberia devolver algo como:
# {"message":"Conexion a base de datos exitosa"}
```

### 5.3 Aplicar Migraciones

Las migraciones se aplican automaticamente al iniciar (ver `Program.cs`), pero si necesitas aplicarlas manualmente:

```bash
docker compose exec api dotnet ef database update
```

---

## 6. Servir el Frontend

### 6.1 Opcion A - Frontend separado (recomendado)

Construir el frontend y servirlo con Nginx:

```bash
# En tu maquina de desarrollo
cd DevNetControl.Web
npm install
npm run build

# Subir la carpeta dist/ al VPS
scp -r dist/ usuario@TU_VPS_IP:/opt/devnetcontrol/web/

# En el VPS, configurar Nginx para servir el frontend
sudo nano /etc/nginx/sites-available/devnetcontrol
```

Configuracion completa de Nginx:
```nginx
server {
    listen 443 ssl http2;
    server_name devnetcontrol.tudominio.com;

    ssl_certificate /etc/letsencrypt/live/devnetcontrol.tudominio.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/devnetcontrol.tudominio.com/privkey.pem;

    # Frontend estatico
    root /opt/devnetcontrol/web/dist;
    index index.html;

    # SPA routing - todas las rutas van a index.html
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy
    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # Swagger
    location /swagger/ {
        proxy_pass http://localhost:5000/swagger/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}

# Redirect HTTP -> HTTPS
server {
    listen 80;
    server_name devnetcontrol.tudominio.com;
    return 301 https://$server_name$request_uri;
}
```

```bash
sudo ln -sf /etc/nginx/sites-available/devnetcontrol /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 6.2 Opcion B - Frontend dentro del container Docker

Modificar el Dockerfile para incluir el frontend:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Build frontend
FROM node:20-alpine AS frontend
WORKDIR /frontend
COPY DevNetControl.Web/package*.json ./
RUN npm ci
COPY DevNetControl.Web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY DevNetControl.Api/DevNetControl.Api.csproj DevNetControl.Api/
RUN dotnet restore DevNetControl.Api/DevNetControl.Api.csproj
COPY DevNetControl.Api/ DevNetControl.Api/
WORKDIR /src/DevNetControl.Api
RUN dotnet build DevNetControl.Api.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish DevNetControl.Api.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=frontend /frontend/dist wwwroot/
USER app
ENTRYPOINT ["dotnet", "DevNetControl.Api.dll"]
```

Con esta opcion, el frontend se sirve automaticamente desde `wwwroot/` por Kestrel.

---

## 7. Configurar Firewall

```bash
# Activar UFW
sudo ufw enable

# Permitir SSH
sudo ufw allow 22/tcp

# Permitir HTTP/HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Permitir SSH para nodos VPS (si el servidor central tambien es un nodo)
sudo ufw allow 22/tcp

# Verificar
sudo ufw status
```

---

## 8. Backup de PostgreSQL

### 8.1 Backup Manual

```bash
# Crear backup
docker compose exec db pg_dump -U postgres devnetcontrol > backup_$(date +%Y%m%d_%H%M%S).sql

# Restaurar desde backup
cat backup_20260506_120000.sql | docker compose exec -T db psql -U postgres devnetcontrol
```

### 8.2 Backup Automatico

```bash
# Crear script de backup
sudo nano /opt/devnetcontrol/backup.sh
```

```bash
#!/bin/bash
BACKUP_DIR="/opt/devnetcontrol/backups"
mkdir -p $BACKUP_DIR
DATE=$(date +%Y%m%d_%H%M%S)

# Backup de PostgreSQL
docker compose exec -T db pg_dump -U postgres devnetcontrol | gzip > $BACKUP_DIR/db_$DATE.sql.gz

# Mantener solo los ultimos 7 backups
ls -t $BACKUP_DIR/db_*.sql.gz | tail -n +8 | xargs -r rm

echo "Backup completado: db_$DATE.sql.gz"
```

```bash
chmod +x /opt/devnetcontrol/backup.sh

# Agregar al cron (backup diario a las 3 AM)
crontab -e
# Agregar linea:
0 3 * * * /opt/devnetcontrol/backup.sh >> /opt/devnetcontrol/backups/backup.log 2>&1
```

---

## 9. Monitoreo Basico

### 9.1 Verificar Estado de Servicios

```bash
# Docker containers
docker compose ps

# Logs en tiempo real
docker compose logs -f api

# Uso de recursos
docker stats

# PostgreSQL
docker compose exec db psql -U postgres -c "SELECT version();"
```

### 9.2 Health Check

Agregar un healthcheck al docker-compose:

```yaml
services:
  api:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/api/auth/test-db"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

---

## 10. Actualizar la Aplicacion

```bash
cd /opt/devnetcontrol

# Actualizar codigo
git pull

# Reconstruir y reiniciar
docker compose down
docker compose build
docker compose up -d

# Verificar
docker compose ps
docker compose logs -f api
```

---

## 11. Troubleshooting

### API no inicia
```bash
# Ver logs
docker compose logs api

# Problema comun: puerto ocupado
sudo lsof -i :5000
sudo kill -9 <PID>

# Problema comun: variables de entorno faltantes
docker compose config  # Ver configuracion renderizada
```

### PostgreSQL no conecta
```bash
# Verificar que esta corriendo
docker compose ps db

# Ver logs
docker compose logs db

# Intentar conexion manual
docker compose exec db psql -U postgres -c "\l"
```

### SSL no funciona
```bash
# Verificar certificado
sudo certbot certificates

# Renovar manualmente
sudo certbot renew --force-renewal

# Verificar config de Nginx
sudo nginx -t
sudo systemctl status nginx
```

### Memoria insuficiente
```bash
# Ver uso de memoria
free -h
docker stats

# Si 1 GB es insuficiente, considerar:
# 1. Reducir pool de conexiones de PostgreSQL
# 2. Agregar swap (temporal)
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

---

## 12. Checklist Pre-Produccion

- [ ] Variables de entorno configuradas con valores seguros
- [ ] JWT_KEY cambiada (no usar la del appsettings.json)
- [ ] ENCRYPTION_KEY generada aleatoriamente
- [ ] POSTGRES_PASSWORD configurada
- [ ] Credenciales de seed cambiadas (superadmin123, admin123)
- [ ] SSL configurado con Let's Encrypt
- [ ] Firewall activo (solo puertos 22, 80, 443)
- [ ] Backup automatico configurado
- [ ] Docker compose funcionando
- [ ] Frontend accesible desde el navegador
- [ ] Login funciona con HTTPS
- [ ] Swagger deshabilitado en produccion (ya lo esta por `app.Environment.IsDevelopment()`)
- [ ] Rate limiting activo
- [ ] HSTS activo

---

*Esta guia asume un VPS con Ubuntu 22.04 LTS. Para otros sistemas operativos, adaptar los comandos de instalacion.*
