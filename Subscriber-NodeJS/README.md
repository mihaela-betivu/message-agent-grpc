# Message Agent Subscriber (Node.js)

Un subscriber simplu în Node.js pentru sistemul de mesagerie gRPC Message Agent.

## Instalare

```bash
cd Subscriber-NodeJS
npm install
```

## Utilizare

### 1. Pornește broker-ul .NET
```bash
cd ../Broker
dotnet run
```

### 2. Pornește subscriber-ul Node.js
```bash
cd ../Subscriber-NodeJS
npm start
```

### 3. Specifică topicul (opțional)
```bash
npm start sports
```

## Funcționalități

- ✅ Se conectează la broker-ul .NET prin gRPC
- ✅ Se abonează la topicuri specificate
- ✅ Primește notificări prin gRPC (Notifier service)
- ✅ Gestionarea erorilor
- ✅ Shutdown graceful

## Servicii gRPC

- `Notifier.Notify` - Primește notificări de la broker prin gRPC

## Exemplu de utilizare

1. Pornește broker-ul .NET pe portul 5001
2. Pornește subscriber-ul Node.js pe portul 3001
3. Subscriber-ul se va abona la topicul specificat
4. Când broker-ul trimite mesaje, subscriber-ul le va afișa în consolă

## Structura

```
Subscriber-NodeJS/
├── subscriber.js          # Subscriber principal
├── protos/               # Fișiere protobuf
│   ├── subscribe.proto   # Serviciul de abonare
│   └── notify.proto      # Serviciul de notificare
├── package.json          # Dependențe
└── README.md            # Documentație
```

## Configurare

Modifică adresele în `subscriber.js`:
- `BROKER_ADDRESS` - adresa broker-ului .NET
- `SUBSCRIBER_PORT` - portul subscriber-ului Node.js
