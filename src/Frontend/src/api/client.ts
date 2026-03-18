import axios from 'axios';

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5159',
  headers: { 'Content-Type': 'application/json' },
});

client.interceptors.response.use(
  (res) => res,
  (error) => {
    const problem = error.response?.data;
    const message = problem?.detail ?? problem?.title ?? 'Unexpected error';
    return Promise.reject(new Error(message));
  }
);

// Call once at app startup with the dev user ID from env or config
export function setDevUserId(id: string) {
  client.defaults.headers.common['X-Dev-UserId'] = id;
}

export default client;
