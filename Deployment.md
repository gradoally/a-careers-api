Установка (на примере Ubuntu)
=============================


Необходимые пакеты
----------------------------

* Установить [**Microsoft .NET 6.0**](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) согласно [инструкции на сайте](https://learn.microsoft.com/ru-ru/dotnet/core/install/linux-ubuntu). Более старшие версии (7.0 и 8.0 должны работать, но не проверялись: 7.0 не имеет long-term support, а 8.0 ещё не релизнута).
* Установить [NGINX](https://nginx.org/) и [certbot](https://www.nginx.com/blog/using-free-ssltls-certificates-from-lets-encrypt-with-nginx/) согласно их инструкциям.


Развертывание приложения
-------------------------------------------

* Клонировать Git-репо в какую-либо папку на диске (например `/home/somebackend`).
* Самотестирование:
    * Перейти в папку `/home/somebackend`
    * Запустить (выполнить) команду `dotnet test`
    * Через 1-2 мин работа должна закончиться зеленой строкой вида: `Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: Х ms - backend.tests.dll (net6.0)`
* Перейти в папку `/home/somebackend/backoffice`
* Отредактировать `appsettings.json` (указать адрес коллекции и адрес мастер-контракта, при необходимости исправить пути или другие параметры)
* Собрать приложение командой `dotnet publish -c Release -o /var/www/somebackend`, где после `-o` нужно указать целевую папку, откуда будет впоследствии производиться запуск.
* Скачать [библиотеку tonlib](https://github.com/ton-blockchain/ton/releases) в папку запуска:
    ```
    wget https://github.com/ton-blockchain/ton/releases/download/v2023.06/tonlibjson-linux-x86_64.so -o /var/www/somebackend
    ```
    При этом следует выбрать правильный файл для текущей архитектуры процессора (`tonlibjson-linux-arm64.so` или `tonlibjson-linux-x86_64.so`).
* Создать служебный каталог и дать на него права на запись: `mkdir /var/www/somebackend/cache & chmod 777 /var/www/somebackend/cache` (данный каталог служит для хранения данных о синхронизации с блокчейном, о Merkle-проверенных данных с лайтсервера);

На данном этапе приложение можно запустить выполнив `/var/www/somebackend/backend` - на экране появится много текста, можно будет различить сообщения о найденных NFT и периодическим опросе tonapi. Не должно быть красного текста, свидетельствующего об ошибках.


Добавление в автозапуск и настройка доступа снаружи
---------------------------------------------------

### Автозапуск

Добавить приложение в автозапуск можно например [через systemd](https://learn.microsoft.com/ru-ru/troubleshoot/developer/webapps/aspnetcore/practice-troubleshoot-linux/2-3-configure-aspnet-core-application-start-automatically) используя такой файл описания сервиса:
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

Пути в строках `WorkingDirectory` и `ExecStart` должен соответствовать параметру `-o` при выполнении шага `dotnet publish`.

Запуск/остановка сервиса: `sudo systemctl start somebackend` и `sudo systemctl stop somebackend`.

Просмотр логов: `sudo journalctl -fu somebackend -n 100` (показать последние 100 записей и выводить новые по мере поступления).


### Доступ через Nginx

Добавить сайт в Nginx можно используя такой файл описания сайта:
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

Номер порта в строке `proxy_pass` должен соответствовать номеру порта в параметре `Kestrel:Endpoints:Http:Url` в файле `appsettings.json`

С помощью команды `certbot` нужно сделать бесплатный SSL-сертификат (не забудьте разрешить ему внести изменения в описание сайта Nginx, он добавит туда информацию об SSL).


### Установка завершена

Установка завершена. Теперь можно открыть https://somebackend.example.com/swagger в браузере - должна отобразиться страница Swagger-а.