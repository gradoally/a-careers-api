Deployment (using Ubuntu)
=============================


Prerequisites
----------------------------

* Install [**Microsoft .NET 6.0 (SDK)**](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) using [official manual](https://learn.microsoft.com/ru-ru/dotnet/core/install/linux-ubuntu).
    * .NET 8.0 is also possible, but current (Ferbruary 2023) version 8.0.2 has some memory leak problems. If used, app have to be restarted daily (depends on server hardware). 
* Install [NGINX](https://nginx.org/) and [certbot](https://www.nginx.com/blog/using-free-ssltls-certificates-from-lets-encrypt-with-nginx/) according to their manuals.


Installing application
-------------------------------------------

* Clone Git-repo to local folder (for example, to `/home/somebackend`).
* Self test:
    * Go to `/home/somebackend` folder
    * Runs `dotnet test`
    * In 1-2 minutes it should finish with green text like: `Passed!  - Failed: 0, Passed: 3, Skipped: 0, Total: 3, Duration: Х ms - backend.tests.dll`
* Go to `/home/somebackend/backoffice`
* Edit `appsettings.json` (put master contract address into MasterAddress, change other params if required)
* Build application using `dotnet publish -c Release -o /var/www/somebackend`, where folder after `-o` is target folder you want to run app from.
* Download [tonlib library](https://github.com/ton-blockchain/ton/releases) into app folder:
    ```
    wget https://github.com/ton-blockchain/ton/releases/download/v2024.01/tonlibjson-linux-x86_64.so -o /var/www/somebackend
    ```
    Make sure you choose correct architecture (`tonlibjson-linux-arm64.so` or `tonlibjson-linux-x86_64.so`) that matches your system.
* Create cache folder and set correct permissions: `mkdir /var/www/somebackend/cache & chmod 777 /var/www/somebackend/cache` (this folder will be used to store blockchain sync data, Merkle proofs etc);

Now, you can run the app. Execute `/var/www/somebackend/backend` - it will show different logs on screen, including detecting existing admin/user/order contracts. Screen should not have red (error) text.


Configuring app for auto-start and external access
---------------------------------------------------

### Autostart

You may use [systemd](https://learn.microsoft.com/ru-ru/troubleshoot/developer/webapps/aspnetcore/practice-troubleshoot-linux/2-3-configure-aspnet-core-application-start-automatically) for starting ap as a service, using this example service description file:
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

Please ensure values in `WorkingDirectory` и `ExecStart` matches parameter `-o` value during `dotnet publish`.

Start/stop service: `sudo systemctl start somebackend` and `sudo systemctl stop somebackend`.

View logs: `sudo journalctl -fu somebackend -n 100` (show last 100 log entries and wait for new ones).


### External access using Nginx

You may configure backend to run on port 80 and expose this port directly, but using Nginx as reverse proxy will give you additional benefits, like HTTPS/SSL certificate management, additional header configuration etc.

Use this sample config when creating new site in Nginx:
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

Please use real (external) domain name in `server_name` parameter (don't forget to update entry in tour DNS Zone).

Port number in `proxy_pass` parameter should math one in `Kestrel:Endpoints:Http:Url` in `appsettings.json` configuration file.

Use `certbot` to create free SSL-certificate and update it automatically.


### Installation complete

Done. Now open https://somebackend.example.com/swagger in your browser - you should see Swagger page.