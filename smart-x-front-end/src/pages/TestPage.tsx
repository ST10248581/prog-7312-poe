import { useState } from "react";
import { testConnection } from "../services/apiService";

function TestPage() {
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleTest = async () => {
    setLoading(true);
    setError(null);
    setResult(null);

    try {
      const data = await testConnection();
      setResult(
        `Status: ${data.status}\nMessage: ${data.message}\nTimestamp: ${data.timestamp}`
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unknown error");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ padding: "2rem", fontFamily: "monospace" }}>
      <h1>Smart-X API Connection Test</h1>
      <button onClick={handleTest} disabled={loading}>
        {loading ? "Testing..." : "Test API Connection"}
      </button>

      {result && (
        <pre
          style={{
            marginTop: "1rem",
            padding: "1rem",
            background: "#1a1a2e",
            color: "#0f0",
            borderRadius: "8px",
          }}
        >
          {result}
        </pre>
      )}

      {error && (
        <pre
          style={{
            marginTop: "1rem",
            padding: "1rem",
            background: "#2e1a1a",
            color: "#f55",
            borderRadius: "8px",
          }}
        >
          Error: {error}
        </pre>
      )}
    </div>
  );
}

export default TestPage;
