import axios, { AxiosInstance, AxiosError } from 'axios';
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types/auth';
import type {
  ServiceConnection,
  InitiateOAuthRequest,
  OAuthUrlResponse,
  CompleteOAuthRequest,
  CompleteOAuthResponse,
  ServiceType,
} from '../types/services';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:5001/api/v1';

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add request interceptor to inject auth token
    this.client.interceptors.request.use((config) => {
      const token = localStorage.getItem('authToken');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });

    // Add response interceptor for error handling
    this.client.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        if (error.response?.status === 401) {
          // Token expired or invalid - clear auth and redirect to login
          localStorage.removeItem('authToken');
          localStorage.removeItem('user');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  // Authentication endpoints
  async register(request: RegisterRequest): Promise<AuthResponse> {
    const response = await this.client.post<AuthResponse>('/auth/register', request);
    return response.data;
  }

  async login(request: LoginRequest): Promise<AuthResponse> {
    const response = await this.client.post<AuthResponse>('/auth/login', request);
    return response.data;
  }

  // Service connection endpoints
  async getConnections(): Promise<ServiceConnection[]> {
    const response = await this.client.get<ServiceConnection[]>('/services/connections');
    return response.data;
  }

  async getConnection(connectionId: string): Promise<ServiceConnection> {
    const response = await this.client.get<ServiceConnection>(`/services/connections/${connectionId}`);
    return response.data;
  }

  async getConnectionsByService(serviceType: ServiceType): Promise<ServiceConnection[]> {
    const response = await this.client.get<ServiceConnection[]>(`/services/${serviceType}/connections`);
    return response.data;
  }

  async initiateOAuth(request: InitiateOAuthRequest): Promise<OAuthUrlResponse> {
    const response = await this.client.post<OAuthUrlResponse>('/services/connect', request);
    return response.data;
  }

  async completeOAuth(request: CompleteOAuthRequest): Promise<CompleteOAuthResponse> {
    const response = await this.client.post<CompleteOAuthResponse>('/services/callback', request);
    return response.data;
  }

  async disconnectService(connectionId: string): Promise<void> {
    await this.client.delete(`/services/connections/${connectionId}`);
  }

  async refreshToken(connectionId: string): Promise<void> {
    await this.client.post(`/services/connections/${connectionId}/refresh`);
  }

  // Playlist endpoints
  async getPlaylists(): Promise<any[]> {
    const response = await this.client.get('/playlists');
    return response.data;
  }

  async getConnectionPlaylists(connectionId: string): Promise<any[]> {
    const response = await this.client.get(`/playlists/connection/${connectionId}`);
    return response.data;
  }

  async syncPlaylists(connectionId: string): Promise<any> {
    const response = await this.client.post('/playlists/sync', { connectionId });
    return response.data;
  }

  async syncAllPlaylists(): Promise<void> {
    await this.client.post('/playlists/sync/all');
  }
}

export const api = new ApiClient();
