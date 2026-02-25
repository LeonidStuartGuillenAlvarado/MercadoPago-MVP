# MercadoPago-MVP

Producto Mínimo Viable (MVP) para la implementación de pagos mediante **QR dinámico y QR estático** utilizando la API de Mercado Pago.

---

## 🎯 Objetivo

Optimizar y acelerar el proceso de cobro incorporando pagos con código QR, reduciendo fricción en el checkout y simplificando la experiencia del usuario tanto en tienda online como en tienda física.

---

## 🧩 Escenarios Implementados

### 1️⃣ Tienda Web — QR Dinámico

**Antes**
El usuario debía ingresar manualmente los datos de su tarjeta bancaria (método tradicional de checkout).

**Después**
La tienda genera un **QR dinámico** a través de Mercado Pago.
El usuario escanea el código desde su aplicación y confirma el pago.

**Resultado:**

* Reducción del tiempo de pago
* Eliminación de ingreso manual de datos
* Mayor seguridad y comodidad

---

### 2️⃣ Tienda Física — QR Estático

**Antes**
El cliente abonaba mediante efectivo, alias o tarjeta física.

**Después**
El comercio habilita un **QR estático** que el cliente escanea para realizar el pago desde su dispositivo móvil.

**Resultado:**

* Flujo de cobro más ágil
* Menor manipulación de efectivo
* Integración digital del punto de venta

---

## 🏗 Arquitectura y Tecnologías

### Backend

* Web API
* ASP.NET 8
* API oficial de Mercado Pago
* SQL Server
* Webhooks HTTP
* SignalR

### Frontend

* Angular 21+
* TailwindCSS

### Externo

* Ngrok (para exponer el webhook en entorno local)

---

## 🧑‍💻 Entorno de Desarrollo

* VS Code (Frontend)
* Visual Studio Insiders (Backend)

---

## 🗄 Base de Datos

**Base de datos:** `MpQrDb`

---

### Tabla: Payments

```sql
CREATE TABLE [dbo].[Payments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	 NULL,
	 NULL,
	 NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[CreatedAt] [datetime] NOT NULL DEFAULT (GETDATE()),
	[UpdatedAt] [datetime] NULL,
PRIMARY KEY CLUSTERED ([Id] ASC)
)
```

---

### Tabla: StorePayments

```sql
CREATE TABLE [dbo].[StorePayments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	 NULL,
	 NOT NULL,
	 NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[IsEnabled] [bit] NOT NULL DEFAULT (0),
	 NOT NULL,
	[CreatedAt] [datetime] NOT NULL DEFAULT (GETDATE()),
	[UpdatedAt] [datetime] NULL,
PRIMARY KEY CLUSTERED ([Id] ASC)
)
```

---

## ⚙ Configuración del Proyecto

---

### 🔹 Backend

1. Solicitar los **Access Tokens** al responsable del proyecto.
2. Configurarlos en `appsettings.json`.
3. Crear la base de datos `MpQrDb` y sus tablas.
4. Configurar la cadena de conexión en `appsettings.json`.
5. Revisar la documentación en Swagger UI.
6. Crear una cuenta en Ngrok y descargar el ejecutable.
7. Ejecutar:

```bash
ngrok http 5251
```

(Verificar el puerto real del localhost)

8. Copiar el dominio generado por Ngrok en:

   * `appsettings.json` → `BaseUrl`
   * Panel de Mercado Pago → sección **Webhooks**

Webhook configurado como:

```
https://tu-dominio-ngrok/api/payments/webhook
```

9. Iniciar la API en modo HTTP.

---

### 🔹 Frontend

1. Configurar el dominio de Ngrok en:

   * `environment.ts`
   * `signalr.service.ts` (agregar `/paymentHub`)
2. Ejecutar:

```bash
ng serve
```

---

## 🚀 Ejecución Paso a Paso

1. Iniciar la API (HTTP).
2. Ejecutar Ngrok:

```bash
ngrok http 5251
```

3. Iniciar el proyecto Angular.
4. Seleccionar escenario:

---

### 🛒 Flujo Tienda Web

1. Seleccionar productos
2. Continuar al pago
3. Generar QR dinámico
4. Escanear desde la app de Mercado Pago
5. Confirmación automática vía Webhook + SignalR

---

### 🏬 Flujo Tienda Física

1. Seleccionar productos
2. Presionar “Cobrar”
3. Habilitación de QR estático
4. Escaneo por parte del cliente
5. Confirmación en tiempo real

---

## 📌 Consideraciones Técnicas

* Los pagos son confirmados mediante **Webhooks**.
* La actualización en tiempo real se realiza mediante **SignalR**.
* Ngrok es obligatorio en entorno local para recibir notificaciones de pago.
* El estado del pago se persiste en SQL Server.