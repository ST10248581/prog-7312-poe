const API_BASE_URL = "http://localhost:5127/api";

interface TestResponse {
  status: string;
  message: string;
  timestamp: string;
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(`API error: ${response.status} ${response.statusText}`);
  }
  return response.json() as Promise<T>;
}

export async function testConnection(): Promise<TestResponse> {
  const response = await fetch(`${API_BASE_URL}/test`);
  return handleResponse<TestResponse>(response);
}

export default {
  testConnection,
};
