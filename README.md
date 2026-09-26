# Kanban Board — Web API на ASP.NET Core

Серверная часть канбан-доски: пользователи, задачи со статусами, аутентификация на JWT
(access + refresh токены), интерактивная документация Swagger и хранение в SQLite.
Проект построен по Onion (Clean) Architecture и упакован в Docker.

---

## Содержание

- [О проекте](#о-проекте)
- [Технологии](#технологии)
- [Архитектура](#архитектура)
- [Структура репозитория](#структура-репозитория)
- [Возможности](#возможности)
- [Быстрый старт](#быстрый-старт)
- [Конфигурация](#конфигурация)
- [Аутентификация](#аутентификация)
- [API: основные эндпоинты](#api-основные-эндпоинты)
- [Пример использования (curl)](#пример-использования-curl)
- [База данных и миграции](#база-данных-и-миграции)
- [Docker](#docker)
- [Troubleshooting](#troubleshooting)
- [Правила разработки](#правила-разработки)

---

## О проекте

Kanban Board — это REST API для управления задачами в стиле канбан:
пользователь регистрируется, входит, создаёт задачи, переводит их по статусам
и удаляет. Каждый пользователь видит и изменяет только свои задачи.

Проект решает три учебно-практические задачи:

1. **Чистая архитектура** — бизнес-логика отделена от фреймворков и базы данных.
2. **Безопасная аутентификация** — короткоживущие access-токены + долгоживущие refresh-токены.
3. **Повторяемое развертывание** — один и тот же образ работает локально и на сервере.

## Технологии

| Технология            | Зачем                                              |
|-----------------------|----------------------------------------------------|
| ASP.NET Core (net9/10)| Веб-фреймворк, контроллеры, DI, middleware         |
| Entity Framework Core | ORM: модели, миграции, запросы к БД                |
| SQLite                | Лёгкая файловая БД (без отдельного СУБД-сервера)   |
| JWT Bearer            | Аутентификация: access + refresh токены            |
| Swashbuckle (Swagger) | Автогенерация интерактивной документации API       |
| Docker + Compose      | Контейнеризация и простое развертывание            |

## Архитектура

Проект следует **Onion Architecture**: зависимости всегда направлены внутрь,
к домену. Внешние слои знают о внутренних, но не наоборот.

```
        ┌───────────────────────────────┐
        │  Api (Presentation)           │  контроллеры, Swagger, Program.cs
        ├───────────────────────────────┤
        │  Infrastructure               │  EF Core DbContext, миграции, DI
        ├───────────────────────────────┤
        │  Application                  │  интерфейсы, сервисы приложений
        ├───────────────────────────────┤
        │  Domain (ядро)                │  сущности: Account, Task, RefreshToken
        └───────────────────────────────┘
```

| Слой           | Ответственность                                        | Зависит от            |
|----------------|--------------------------------------------------------|-----------------------|
| **Domain**     | Сущности и правила предметной области                  | ни от чего            |
| **Application**| Контракты (`IApplicationDbContext`, `ITokenService`),  | Domain                |
|                | прикладные сервисы (генерация JWT)                     |                       |
| **Infrastructure** | Реализации контрактов: DbContext, провайдер БД     | Application, Domain   |
| **Api**        | HTTP-слой: контроллеры, модели запросов/ответов,       | Application,          |
|                | настройка auth и Swagger, точка входа                  | Infrastructure        |

**Зачем так:** домен можно тестировать без БД и веба; SQLite можно заменить на
PostgreSQL, поменяв только Infrastructure; контроллеры не знают про EF Core напрямую.

## Структура репозитория

```
KanBanBoard/
├── Domain/                  # Ядро: сущности Account, Task, RefreshToken
├── Application/
│   ├── Interfaces/          # IApplicationDbContext, ITokenService
│   └── Services/            # JwtTokenService
├── Infrastructure/
│   ├── Persistence/         # ApplicationDbContext
│   ├── Migrations/          # Миграции EF Core
│   └── DependencyInjection.cs
├── Api/
│   ├── MVC/
│   │   ├── Controllers/     # AccountController, TaskController и др.
│   │   ├── Requests/        # Модели запросов (LoginRequest, CreateTaskRequest...)
│   │   └── Responces/       # Модели ответов (LoginResult, GetUserResponce...)
│   ├── Properties/launchSettings.json
│   ├── Program.cs           # Composition root: DI, auth, Swagger, миграции
│   └── appsettings.json
├── Dockerfile               # Multi-stage сборка (sdk → runtime)
├── docker-compose.yml       # Контейнер + volume для БД
└── KanBanBoard.sln
```

## Возможности

- Регистрация и вход пользователей, хеширование паролей.
- Access-токен (короткий срок) + refresh-токен (для продления сессии).
- CRUD задач: создание, смена статуса, удаление; проверка принадлежности владельцу.
- Автоматическое применение миграций при старте приложения.
- Swagger UI для просмотра и проверки всех эндпоинтов.
- Запуск локально (`dotnet run`) или в Docker (`docker compose up`).

## Быстрый старт

### Требования

- .NET SDK 9.0+ (версия зависит от `TargetFramework` в `.csproj`);
- Docker и Docker Compose — для контейнерного запуска.

### Вариант 1: локально, без Docker

```bash
cd KanBanBoard
dotnet run --project Api
```

Swagger: `http://localhost:5238/swagger` (порт берётся из `launchSettings.json`).

### Вариант 2: в Docker

```bash
cd KanBanBoard
docker compose up -d --build
```

Swagger: `http://localhost:8000/swagger`.
База данных хранится в именованном volume `kanban-data` и переживает пересоздание контейнера.

## Конфигурация

Основные настройки — в `Api/appsettings.json`:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=kanban.db"   // в Docker: /data/kanban.db
  },
  "Jwt": {
    "Key": "<секретный ключ подписи>",
    "Issuer": "...",
    "Audience": "...",
    "AccessTokenLifetimeMinutes": 15
  }
}
```

Любой параметр можно переопределить переменными окружения
(удобно в Docker и на сервере), например:

```bash
ConnectionStrings__DefaultConnection="Data Source=/data/kanban.db"
ASPNETCORE_URLS="http://0.0.0.0:8000"
ASPNETCORE_ENVIRONMENT=Production
```

> Двойное подчёркивание `__` заменяет разделитель секций конфига.

## Аутентификация

1. `POST /api/Account/CreateAccount` — регистрация пользователя.
2. `POST /api/Account/Login` — вход; сервер выдаёт **access-токен** (короткий срок)
   и **refresh-токен** (длинный срок); токены отдаются в cookie.
3. Запросы к защищённым эндпоинтам идут с cookie / заголовком `Authorization`.
4. Когда access-токен истёк — пара токенов обновляется через refresh-эндпоинт.
5. Все защищённые действия помечены атрибутом `[Authorize]`.

## API: основные эндпоинты

| Метод | Маршрут                        | Описание                    | Доступ     |
|-------|--------------------------------|-----------------------------|------------|
| POST  | `/api/Account/CreateAccount`   | Регистрация                 | открытый   |
| POST  | `/api/Account/Login`           | Вход, выдача токенов        | открытый   |
| POST  | `/api/Account/Refresh`         | Продление пары токенов      | refresh    |
| POST  | `/api/Task/CreateTask`         | Создать задачу              | `[Authorize]` |
| POST  | `/api/Task/ChangeStatusTask`   | Сменить статус задачи       | `[Authorize]` |
| POST  | `/api/Task/DeleteTask`         | Удалить свою задачу         | `[Authorize]` |

> Актуальный и полный список маршрутов и моделей — всегда в Swagger (`/swagger`):
> документация генерируется из кода и не устаревает.

## Пример использования (curl)

```bash
BASE=http://localhost:8000

# 1. Регистрация
curl -X POST $BASE/api/Account/CreateAccount \
  -H "Content-Type: application/json" \
  -d '{"login":"user","password":"secret123"}'

# 2. Вход (сохраняем cookie с токенами)
curl -c cookies.txt -X POST $BASE/api/Account/Login \
  -H "Content-Type: application/json" \
  -d '{"login":"user","password":"secret123"}'

# 3. Создать задачу
curl -b cookies.txt -X POST $BASE/api/Task/CreateTask \
  -H "Content-Type: application/json" \
  -d '{"name":"Сделать README","shortDescription":"docs","description":"...","status":"Todo"}'

# 4. Сменить статус
curl -b cookies.txt -X POST $BASE/api/Task/ChangeStatusTask \
  -H "Content-Type: application/json" \
  -d '{"taskId":"<guid-задачи>","newStatus":"Done"}'

# 5. Удалить задачу
curl -b cookies.txt -X POST $BASE/api/Task/DeleteTask \
  -H "Content-Type: application/json" \
  -d '{"taskId":"<guid-задачи>"}'
```

## База данных и миграции

- При старте приложения миграции применяются автоматически (`Database.Migrate()`).
- После изменения моделей (Domain) создавайте миграцию:

```bash
dotnet ef migrations add <ИмяМиграции> \
  --project Infrastructure --startup-project Api

dotnet ef database update \
  --project Infrastructure --startup-project Api
```

- CLI-инструмент EF (однократно): `dotnet tool install --global dotnet-ef`.

## Docker

- `Dockerfile` — multi-stage: сборка в образе `sdk`, запуск в лёгком `aspnet`.
- `docker-compose.yml` — порт `8000:8000`, volume `kanban-data` для файла БД.

```bash
docker compose up -d --build   # собрать и запустить
docker compose logs -f         # логи
docker compose restart         # перезапуск
docker compose down            # остановить (данные сохранятся)
docker compose down -v         # остановить и УДАЛИТЬ базу (осторожно!)
```

Для доступа из внешней сети на роутере настроен проброс порта `80 → 8000`
на адрес сервера; приложение внутри контейнера слушает `0.0.0.0:8000`.

## Troubleshooting

| Симптом | Причина | Решение |
|---------|---------|---------|
| `Address already in use` | Порт занят старым процессом | `pkill -f dotnet` или сменить порт |
| `no such table: Accounts` | Миграции не применены | `dotnet ef database update ...` |
| Swagger отдаёт 404 | Swagger включён только для Development | Вынести `UseSwagger()` из `if (IsDevelopment())` |
| `Could not load type ... Microsoft.OpenApi` | Несовместимые версии Swashbuckle/OpenApi | Swashbuckle 6.9.0 для net9 |
| `Permission denied` при bind | Порт < 1024 или запуск под sudo | Использовать порт > 1024 без sudo |
| Локально работает, извне нет | NAT/файрвол/провайдер | Проверить `ss -tlnp`, iptables, проброс портов, CGNAT |

## Правила разработки

Куда класть новый код:

- **Новая сущность / бизнес-правило** → `Domain`.
- **Новый контракт или прикладной сценарий** → `Application`.
- **Работа с БД, внешние сервисы, реализации контрактов** → `Infrastructure`.
- **Новый HTTP-эндпоинт, модели запросов/ответов** → `Api`.

Порядок добавления фичи: модель в Domain → DbSet/контракт в Application →
реализация и миграция в Infrastructure → контроллер в Api → тест через Swagger.

---

## Лицензия

Учебный проект. Распространяется свободно.
