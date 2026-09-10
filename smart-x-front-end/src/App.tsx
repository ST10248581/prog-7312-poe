import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Navbar from "./components/Navbar";
import TelemetryPage from "./pages/TelemetryPage";
import CommandsPage from "./pages/CommandsPage";
import TopologyPage from "./pages/TopologyPage";
import "./App.css";

function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <main className="app-content">
        <Routes>
          <Route path="/" element={<Navigate to="/telemetry" replace />} />
          <Route path="/telemetry" element={<TelemetryPage />} />
          <Route path="/commands" element={<CommandsPage />} />
          <Route path="/topology" element={<TopologyPage />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}

export default App;
