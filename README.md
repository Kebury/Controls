[README.md](https://github.com/user-attachments/files/25449322/README.md)
# Controls

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Controls — WPF-приложение для управления контрольными заданиями, отслеживания их исполнения и хранения документов организации.

## Возможности

- Контрольные задания с типами (разовое, ежедневное, еженедельное и др.), приоритетами важности и срочности
- Назначение исполнителей и отделов, отслеживание промежуточных ответов
- Архив выполненных заданий
- Календарный вид задач
- Уведомления о приближающихся и просроченных сроках через Windows Toast
- Хранение документов — шаблоны и образцы (PDF, Word, Excel)
- Многопользовательский режим через сетевую БД с мониторингом соединения
- Светлая и тёмная тема интерфейса
- Иконка в трее

## Требования

- Windows 10 (версия 1903+) или Windows 11
- .NET 10 SDK (для разработки)
- Visual Studio 2022 или Visual Studio Code

## Установка (для пользователей)

1. Скачайте `ControlsSetup.exe` из [Releases](../../releases).
2. Запустите установщик и следуйте инструкциям.

База данных создаётся автоматически при первом запуске.

## Сборка из исходников

```bash
git clone https://github.com/ваш-username/controls.git
cd controls
dotnet restore Controls/src/Controls/Controls/Controls.csproj
dotnet build Controls/src/Controls/Controls/Controls.csproj --configuration Release
dotnet run --project Controls/src/Controls/Controls/Controls.csproj
```

## Структура проекта

```
controls/
├── controls.sln
└── Controls/
    ├── src/
    │   ├── Controls/Controls/
    │   │   ├── Models/          # Модели данных и перечисления
    │   │   ├── ViewModels/      # MVVM ViewModels
    │   │   ├── Views/           # Окна и пользовательские элементы
    │   │   ├── Services/        # Уведомления, темы, трей, мониторинг БД
    │   │   ├── Data/            # EF Core контекст и конфигурация
    │   │   ├── Migrations/      # Миграции базы данных
    │   │   └── Helpers/         # Конвертеры, настройки, утилиты
    │   └── Controls.Installer/  # Скрипт Inno Setup
    ├── database/                # Директория базы данных по умолчанию
    └── documents/               # Хранилище документов
```

## Технологии

| Компонент | Технология |
|---|---|
| Framework | .NET 10.0 |
| UI | WPF |
| Архитектура | MVVM |
| ORM | Entity Framework Core 10 |
| База данных | SQLite |
| Уведомления | Microsoft.Toolkit.Uwp.Notifications |
| Установщик | Inno Setup 6 |

## Автор

Канатов М.Э. (Kebury), 2026
