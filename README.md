Some DAO Backend
================


Требования к смартконтрактам
----------------------------

* Айтемы должны принадлежать строго одной коллекции (адрес коллекции задается в настройках);
* Коллекция должна поддерживать методы `get_collection_data` и `get_nft_address_by_index` из стандарта [TEP-62](https://github.com/ton-blockchain/TEPs/blob/master/text/0062-nft-standard.md);
* Индексация (нумерация) айтемов в коллекции должна быть последовательная;
* Айтемы должны поддерживать методы `get_nft_data` и `get_nft_content` из TEP-62;
* Айтемы должны возвращать результат `get_nft_content` вида **On-chain content layout** согласно [TEP-64](https://github.com/ton-blockchain/TEPs/blob/master/text/0064-token-data-standard.md)
    * Поддерживаются следующие поля метаданных:
        * `image`, `name`, `description`, `status`, `technical_assigment`, `category`, `customer_addr` - строковые, хранятся в формате **Snake format**; 
        * `amount` - целое число, хранится как строка в формате **Snake format**; 
        * `starting_unix_time`, `ending_unix_time`, `creation_unix_time` - целое число обозначающее Unix file time seconds, хранится как строка в формате **Snake format**; 


Процедура обновления данных
---------------------------

Используется одновременно несколько способов получения событий об изменённых айтемах:

1. Периодический (параметр `NewOrdersDetectorInterval`, по умолчанию 15 мин) вызов метода коллекции `get_collection_data` для получения информации о новых сминченных айтемах (путем проверки next item index). 
2. Периодический (параметр `CollectionTxTrackingInterval`, по умолчанию 10 секунд(!)) просмотр новых транзакций на смартконтракте коллекции: 
      * при обнаружении транзакций от новых (неизвестных) адресов вне очереди запускается метод (1) для проверки новых сминченных айтемов;
      * при обнаружении транзакций от известных адресов (айтемов) запускается задача по их обновлению;


Настройки системы
-----------------

Для настройки используется файл `appsettings.json` в корне приложения. Каждая настройка имеет описание прямо внутри файла.

**После изменения** файла настроек необходимо **перезапустить** приложение, чтобы измененный файл считался.

Самым главным является параметр `CollectionAddress`, где указывается адрес коллекции.

⚠ Важно! Если **меняется адрес** коллекции, то необходимо **удалить файл базы данных** (`backend.sqlite`)!


Требования к серверу, развертывание и запуск системы
----------------------

Требования к серверу:

* Для работы необходим [**Microsoft .NET 6.0**](https://dotnet.microsoft.com/en-us/download/dotnet/6.0), который работает на Linux, Windows и macOS.
* **NGINX** рекомендуется в качестве реверс-прокси и для управления SSL сертификатом (через certbot);
* В качестве базы данных используется **SQLite**, который не требует какой-либо предварительной установки.

Порядок установки (на примере Ubuntu):

1. Установить [**Microsoft .NET 6.0**](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) согласно инструкции на сайте. Более старшие версии (7.0 и 8.0 должны поддерживаться, но не работать: 7.0 не имеет long-term support, а 8.0 ещё не релиз).
2. Установить [NGINX](https://nginx.org/) и [certbot](https://www.nginx.com/blog/using-free-ssltls-certificates-from-lets-encrypt-with-nginx/) согласно их инструкциям.
3. Клонировать Git-репо в какую-либо папку на диске (например `/home/somebackend`).
4. Самотестирование:
    1. Запустить консоль (командную строку), перейти в папку `/home/somebackend`
    2. Выполнить команду `dotnet test`
    3. Через 1-2 мин работа должна закончиться зеленой строкой вида: `Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: Х ms - backend.tests.dll (net6.0)`
5. Перейти в папку `/home/somebackend/backoffice`
6. Отредактировать `appsettings.json` (указать адрес коллекции, при необходимости исправить пути или другие параметры)
7. Собрать приложение командой `dotnet publish -c Release -o /var/www/somebackend`, где после `-o` нужно указать целевую папку, откуда будет впоследствии производиться запуск.
8. Скачать библиотеку tonlib:
    ```
    cd /var/www/backend
    wget https://github.com/ton-blockchain/ton/releases/download/v2023.06/tonlibjson-linux-x86_64.so
    ```
    При этом следует выбрать правильный файл для текущей архитектуры процессора (tonlibjson-linux-arm64.so или tonlibjson-linux-x86_64.so).
8. Добавить приложение в автозапуск, [например через systemd](https://learn.microsoft.com/ru-ru/troubleshoot/developer/webapps/aspnetcore/practice-troubleshoot-linux/2-3-configure-aspnet-core-application-start-automatically) используя такой файл описания сервиса:
    ```
    [Unit]
    Description=Some Backend

    [Service]
    WorkingDirectory=/var/www/somebackend
    ExecStart=/var/www/somebackend/backend
    Restart=always
    RestartSec=10
    TimeoutStopSec=30
    KillMode=process
    KillSignal=SIGTERM
    SyslogIdentifier=somebackend
    User=www-data
    Environment=ASPNETCORE_ENVIRONMENT=Production
    Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

    [Install]
    WantedBy=multi-user.target
    ```
    Путь в строках `WorkingDirectory` и `ExecStart` должен соответствовать параметру `-o` с шага 7.
9. Добавить сайт в Nginx используя такой файл описания сайта:
    ```
    server {
        listen 80;
        listen [::]:80;
        server_name somebackend.example.com;

        location / {
            proxy_pass         http://localhost:5000;
            proxy_http_version 1.1;
            proxy_set_header   Upgrade $http_upgrade;
            proxy_set_header   Connection keep-alive;
            proxy_set_header   HOST $host;
            proxy_cache_bypass $http_upgrade;
            proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header   X-Forwarded-Proto $scheme;
            proxy_set_header   X-Real-IP $remote_addr;
        }
    }
    ```
    При этом в `server_name` нужно вписать ваше публичное DNS имя под которым будет работать система (не забудьте внести соответствующую A-запись в DNS).
10. Запустить `certbot` и попросить его сделать бесплатный SSL для системы.
11. Открыть https://somebackend.example.com/swagger в браузере - должна отобразиться страница Swagger-а.