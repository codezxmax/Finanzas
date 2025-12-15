# 💰 Finanzas

Aplicación de gestión financiera personal desarrollada en .NET 8.0 con Windows Forms.

## 📋 Descripción

Finanzas es una aplicación de escritorio para Windows que te permite gestionar tus finanzas personales de manera eficiente y sencilla.

## 🚀 Instalación

### Opción 1: Instalador (Recomendado)
1. Descarga `Finanzas-Setup.exe` desde [Releases](https://github.com/codezxmax/Finanzas/releases)
2. Ejecuta el instalador
3. Sigue las instrucciones en pantalla

### Opción 2: Versión Portable
Descarga la versión correspondiente a tu sistema:
- **Windows 64 bits**: `Finanzas-win-x64.zip`
- **Windows 32 bits**: `Finanzas-win-x86.zip`

Extrae el archivo ZIP y ejecuta `Finanzas.exe`

## 🛠️ Requisitos del Sistema

- **Sistema Operativo**: Windows 10/11
- **.NET Runtime**: 8.0 o superior
- **Arquitectura**: x64 o x86

## 💻 Desarrollo

### Tecnologías Utilizadas
- .NET 8.0
- Windows Forms
- C#

### Compilar desde el código fuente

```bash
# Clonar el repositorio
git clone https://github.com/codezxmax/Finanzas.git
cd Finanzas

# Restaurar dependencias y compilar
dotnet restore
dotnet build

# Ejecutar la aplicación
dotnet run
```

### Crear una versión publicable

```bash
# Para Windows x64
dotnet publish -c Release -r win-x64 --self-contained

# Para Windows x86
dotnet publish -c Release -r win-x86 --self-contained
```

## 📦 Estructura del Proyecto

```
Finanzas/
├── assets/          # Recursos e iconos
├── dist/            # Distribuciones compiladas
├── MainForm.cs      # Formulario principal
├── Program.cs       # Punto de entrada
├── Finanzas.csproj  # Archivo de proyecto
└── README.md        # Este archivo
```

## 📄 Licencia

Este proyecto es de código abierto.

## 👤 Autor

**codezxmax**

## 🤝 Contribuciones

Las contribuciones son bienvenidas. Por favor:
1. Haz fork del proyecto
2. Crea una rama para tu característica (`git checkout -b feature/NuevaCaracteristica`)
3. Commit tus cambios (`git commit -m 'Agregar nueva característica'`)
4. Push a la rama (`git push origin feature/NuevaCaracteristica`)
5. Abre un Pull Request

---

⭐ Si te gusta este proyecto, dale una estrella en GitHub
