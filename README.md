# Amega Test Case

This project is an ASP.NET Core (.NET 8, C# 12) Web API that provides:
- REST endpoints to retrieve forex instrument information and top-of-book prices from the Tiingo API.
- A scalable WebSocket endpoint that broadcasts live EURUSD price updates to connected clients using Tiingo's WebSocket feed.

## Features

- **REST API** for instrument list and top-of-book prices.
- **WebSocket API** for real-time EURUSD price updates (fan-out to many clients).
- **Logging** for all key operations.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A [Tiingo API key](https://api.tiingo.com/) (already present in the code, but you should use your own for production)
- Internet access (to reach Tiingo's API)

---

## Getting Started

### 1. Clone the Repository

git clone <your-repo-url> cd <repo-folder>

### 2. Configure the API Key

The Tiingo API key is currently hardcoded in the source.  
**For production,** set it in `appsettings.json` or as an environment variable and update the code to read from configuration.

### 3. Restore and Build

dotnet restore dotnet build

### 4. Run the Application

dotnet run


The API will be available at `https://localhost:5001` (or as configured).

---

## API Endpoints

### REST Endpoints

#### Get List of Supported Instruments

**Response:**

["EURUSD", "EURGBP", "GBPUSD"]

#### Get Top of Book Price for a Ticker

GET /GetInstruments/{ticker}

**Example:**

GET /GetInstruments/EURUSD

**Response:**

{ "ticker": "EURUSD", "bidPrice": 1.2345, "askPrice": 1.2347, "bidSize": 1000000, "askSize": 1000000, "quoteTimestamp": "2024-06-14T12:34:56.789Z" }

### WebSocket Endpoint

#### Subscribe to Live EURUSD Price Updates

Connect to: ws://localhost:5000/ws

- The server will push real-time EURUSD price updates to all connected clients.
- The server maintains a single connection to Tiingo and fans out updates to all subscribers.

---

## Logging

- All key actions (requests, responses, errors, client connects/disconnects, Tiingo events) are logged to stdout.

---

## Notes

- For production, secure your API key and handle configuration securely.
- The project is designed for high concurrency and can handle 1000+ WebSocket clients efficiently.
- For further customization, see `Program.cs` and `Controllers/GetInstrumentsController.cs`.

---

## License

This project is for demonstration and educational purposes.