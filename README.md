# ReportGen API

**ReportGen API** — это WebAPI-приложение на ASP.NET Core, предназначенное для загрузки, хранения и анализа CSV-данных.  
Сервис позволяет загружать данные, получать агрегированные результаты и выполнять фильтрацию.

---

## Стек технологий

```txt
- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL 
- Swagger (Swashbuckle)
- XUnit + Moq (юнит-тесты)
- SQL (custom функции, median)
- Docker / Docker Compose 

```


## Реализованные возможности

    Загрузка CSV-файла, его валидация и сохранение в базу данных

    Подсчёт интегральных показателей (медиана, среднее, минимум/максимум и др.)

    Перезапись результата при повторной загрузке файла

    Фильтрация по имени файла, датам, значениям и времени выполнения

    Получение последних 10 значений по имени файла

    Обработка ошибок через middleware

    SQL-скрипт для ускорения вычислений медианы

    Использование репозитория для доступа к данным

    Swagger-документация

    Покрытие кода юнит-тестами


## Структура проекта
```txt
ReportGen/
├── src
│   ├── Controllers/              # ReportController.cs — основные маршруты API
│   ├── Data/                     # EF Core контекст и миграции
│   ├── Dto/                      # Запросы и ответы
│   ├── Interfaces/               # Контракт репозитория
│   ├── Middleware/               # Глобальная обработка ошибок
│   ├── Models/                   # Сущности и ошибки
│   ├── Repositories/             # Реализация репозитория
│   ├── Scripts/Functions/        # SQL-функции (медиана)
│   ├── Services/                 # CSV-парсинг и логика
│   └── Program.cs, ReportGen.csproj
├── tests/
│   ├── Data/                     # Примеры CSV и JSON
│   ├── HttpTests/                # Примеры команд для HTTP-тестов (curl)
│   └── ReportGen.UnitTests/      # Юнит-тесты контроллера
├── timescaleAPI.sln
└── README.md

```
## Эндпоинты API

```http
POST   /api/report/upload-csv
        Загружает CSV-файл, валидирует и сохраняет данные

POST   /api/report/search_results
        Возвращает список агрегированных результатов с фильтрацией, принимает json с параметрами

GET    /api/report/<filename>
        Возвращает 10 последних значений по дате для указанного файла

GET    /swagger           
        Документация swagger
```

## Правила валидации CSV

    Наличие строки с заголовками столбцов перед данными Date;ExecutionTime;Value 
    
    Дата (Date) — от 01.01.2000 до текущей даты

    Время выполнения (ExecutionTime) ≥ 0

    Значение (Value) ≥ 0

    Кол-во строк: от 1 до 10 000

    Отсутствие или неверный тип — ошибка

    При ошибке — откат транзакции, возврат подробной ошибки

## Планы по развитию
```txt
[+] Загрузка и валидация CSV-файлов
[+] Хранение и пересчёт агрегированных результатов
[+] Гибкая фильтрация данных
[+] Получение последних значений
[+] Обработка ошибок через middleware
[+] SQL-ускорение медианы
[+] Swagger-документация
[+] Покрытие основных компонентов тестами

[ ] Docker Compose + CI/CD

```