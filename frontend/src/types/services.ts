export enum ServiceType {
  Spotify = 0,
  AppleMusic = 1,
  Deezer = 2,
  YouTubeMusic = 3,
}

export enum ConnectionStatus {
  Active = 0,
  Expired = 1,
  Revoked = 2,
  Error = 3,
}

export interface ServiceConnection {
  id: string;
  serviceType: ServiceType;
  serviceAccountId: string;
  serviceAccountEmail?: string;
  serviceAccountDisplayName?: string;
  serviceAccountProfileImageUrl?: string;
  connectionStatus: ConnectionStatus;
  lastSyncedAt?: string;
  createdAt: string;
  accessTokenExpiresAt: string;
}

export interface InitiateOAuthRequest {
  serviceType: ServiceType;
  redirectUri?: string;
}

export interface OAuthUrlResponse {
  authorizationUrl: string;
  state: string;
}

export interface CompleteOAuthRequest {
  serviceType: ServiceType;
  code: string;
  state: string;
  redirectUri?: string;
}

export interface CompleteOAuthResponse {
  connectionId: string;
  connection: ServiceConnection;
}
