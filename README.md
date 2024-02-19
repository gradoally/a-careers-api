Some DAO Backend
================

Backend service for web3 Freelance application based on TON blockchain.

Parses information from smart-contracts, stores data in local DB, and provides this data to frontend in various forms. Local DB updates automatically shortly after blockchain data changes.

Public LiteServers are used, no REST/HTTP API is involved.


Smart contract requirements
----------------------------

Please see [SmartContractRequirements.md](SmartContractRequirements.md).


Local DB updates
-----------------

Master smart contract is checked (every 10 seconds by default) for two reasons:
* To detect new child contracts (Admin/User/Order) by comparing indexes of next contracts inside master data with saved in local DB. When mismatch is detected - addresses of new contracts are calculated via get_methods (using their index), stored in local DB and syncronized.
* To detect changes in (child) Order contracts: every update of Order contract is followed by notification message to Master contract, so new transaction from known Order contract starts an urgent order update.
 
Also, every child contract is force-synced every 24 hours (by default).

In case of LiteServer failure (timeout, out-of-sync etc) update gets retried at increasing interval, from 5 seconds to several hours.
      
Deployment
-----------

Server prerequisites:

* [Microsoft **.NET 8.0**](https://dotnet.microsoft.com/en-us/download/dotnet/8.0), which is available Linux, Windows and macOS.
* [**NGINX**](https://nginx.org/) is recommended for reverse-proxy role, for rate limiting (if needed) and to manage SSL certificate (free one from Let's Encrypt using [certbot](https://certbot.eff.org/) is Ok);
* **SQLite** is used for local DB, it does not require any packages/install.

Deployment steps are described in [Deployment.md](Deployment.md).


Configuration
--------------

All settings are stored in `appsettings.json` file in app root. File contains description for every setting.

After you **update** settings file - application **must be restarted** for new settings to become active!

The main and most important setting is **`MasterAddress`** - it stores TON address (EQ... or UQ...) of Master smart-contract.

⚠ Important! When you change **master address** - you also need to **delete old database** file (backend.sqlite by default)! Application compares new and stored addresses during startup, and will refuse to start if mismatch is detected.

Second important setting is **`DeeplToken`** - token for Deepl.com translation service. With this token, some Order fields (name, description, tecknicalTask) and User fields (about) will be translated to all languages, set in master contract. In case this token is not set or empty - *translation feature will be disabled*. 

ℹ To ensure smooth Git updates in the future, it's recommended to not change existing `appsettings.json` file. Instead, copy it to `appsettings.Production.json` (attention to uppercase **P**!) in the same folder and change settings in it. You may safely remove unchanged (unneeded) settings from new file - application reads both files during startup and takes value from `appsettings.json` if it's not exist in `appsettings.Production.json`. But please pay attention to Json hierarchy inside settings file!


Monitoring
-----------

There is a special self-diagnosis page: `/health`.

It contains some values, like app and host versions, local time, number of contracts and their min/max sync(update) time, last sync masterchain seqno, last timestamps of internal recurrent tasks etc.

And this page contains last value `Healty` with ("yes" or "no" value), which is calculated dynamically depending of values of some previous parameters. For example, healty turns to "no" when internal task(s) fails for too long time.

And when `healty` value is `yes`, a special code (`CVWFHB9EUTDMVDACGZUD`) follows. So you may use this code as a keyword for your monitoring software (e.g. Zabbix or HetrixTools). 