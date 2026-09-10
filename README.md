# prog-7312-poe - Smart-X IoT Mesh Ecosystem Dashboard

> Smart-X IoT Mesh Ecosystem dashboard for real-time telemetry monitoring, device status tracking, alerts, and interactive troubleshooting.

## Overview

The Smart-X IoT Mesh Ecosystem is a proposed interactive dashboard designed to help users monitor and manage a high-throughput IoT environment. The dashboard focuses on **real-time visual feedback** to make continuously changing IoT telemetry easier to understand. It allows users to monitor device status, identify abnormal sensor readings, receive alerts and investigate potential device or connectivity problems. The project is based on research into user engagement and dashboard interactivity for high-throughput IoT systems.

## Project Structure

```
prog-7312-poe/
├── smart-x-backend/          # ASP.NET 10 Web API
│   └── SmartX.Api/
│       ├── Controllers/      # API controllers
│       ├── Program.cs         # Application entry point
│       └── SmartX.Api.csproj  # Project file (.NET 10)
├── smart-x-front-end/        # React + TypeScript (Vite)
│   ├── src/
│   │   ├── pages/            # Page components
│   │   ├── services/         # API service layer
│   │   ├── App.tsx           # Root component
│   │   └── main.tsx          # Entry point
│   ├── package.json
│   └── vite.config.ts
├── .gitignore
└── README.md
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) (v18+)
- npm (comes with Node.js)

## Getting Started

### Backend (ASP.NET 10 Web API)

```bash
cd smart-x-backend/SmartX.Api
dotnet run
```

The API will start on **http://localhost:5127**.

### Frontend (React + TypeScript)

```bash
cd smart-x-front-end
npm install
npm run dev
```

The dev server will start on **http://localhost:5173**.

### Verify the Connection

1. Start the backend API.
2. Start the frontend dev server.
3. Open http://localhost:5173 in your browser.
4. Click the **Test API Connection** button — you should see a success response from the API.

## Key Features

* **Real-time telemetry** — Displays continuously changing sensor data through interactive visualisations.
* **Device status monitoring** — Shows devices as Online, Warning or Offline.
* **Anomaly detection** — Highlights unusual readings and values outside defined thresholds.
* **Proactive alerts** — Notifies users about important events such as device disconnections or abnormal telemetry.
* **Filtering** — Allows users to focus on specific devices, sensors, statuses or time periods.
* **Guided troubleshooting** — Provides structured information to help investigate device and connectivity issues.
* **Progressive disclosure** — Presents important information first while allowing users to access more detailed diagnostics when required.

## Proposed Dashboard

The dashboard follows the principle of:

> **Overview → Filter → Investigate → Troubleshoot**

Users can first view the overall health of the IoT ecosystem before filtering to a specific device or sensor and accessing detailed telemetry and diagnostic information.

## Research Focus

The project investigates how different user engagement strategies can improve the usability of high-throughput IoT dashboards.

The following strategies were considered:

1. Real-time visual feedback
2. Proactive alert systems
3. Gamification
4. Simplified onboarding and guided workflows

**Real-time visual feedback** was selected as the primary strategy because it is most directly suited to continuously changing IoT telemetry and technical monitoring. Proactive alerts and guided workflows are used as supporting strategies.

## Status

**Proposed Solution / Academic Project**

This project represents the proposed dashboard concept developed from the accompanying research into user engagement and dashboard interactivity for high-throughput IoT systems.
