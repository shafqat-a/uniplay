import { ServiceConnection, ServiceType, ConnectionStatus } from '../types/services';

interface Props {
  connection: ServiceConnection;
  onDisconnect: (connectionId: string) => void;
}

const serviceNames: Record<ServiceType, string> = {
  [ServiceType.Spotify]: 'Spotify',
  [ServiceType.AppleMusic]: 'Apple Music',
  [ServiceType.Deezer]: 'Deezer',
  [ServiceType.YouTubeMusic]: 'YouTube Music',
};

const serviceColors: Record<ServiceType, string> = {
  [ServiceType.Spotify]: 'bg-green-600',
  [ServiceType.AppleMusic]: 'bg-red-600',
  [ServiceType.Deezer]: 'bg-orange-600',
  [ServiceType.YouTubeMusic]: 'bg-red-700',
};

const statusColors: Record<ConnectionStatus, string> = {
  [ConnectionStatus.Active]: 'bg-green-100 text-green-800',
  [ConnectionStatus.Expired]: 'bg-yellow-100 text-yellow-800',
  [ConnectionStatus.Revoked]: 'bg-red-100 text-red-800',
  [ConnectionStatus.Error]: 'bg-red-100 text-red-800',
};

const statusLabels: Record<ConnectionStatus, string> = {
  [ConnectionStatus.Active]: 'Active',
  [ConnectionStatus.Expired]: 'Expired',
  [ConnectionStatus.Revoked]: 'Revoked',
  [ConnectionStatus.Error]: 'Error',
};

export function ServiceConnectionCard({ connection, onDisconnect }: Props) {
  const serviceName = serviceNames[connection.serviceType];
  const serviceColor = serviceColors[connection.serviceType];
  const statusColor = statusColors[connection.connectionStatus];
  const statusLabel = statusLabels[connection.connectionStatus];

  const formattedDate = new Date(connection.createdAt).toLocaleDateString();
  const expiresAt = new Date(connection.accessTokenExpiresAt);
  const isExpiringSoon = expiresAt.getTime() - Date.now() < 24 * 60 * 60 * 1000; // < 24 hours

  return (
    <div className="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow">
      {/* Service Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          <div className={`w-12 h-12 ${serviceColor} rounded-full flex items-center justify-center text-white font-bold text-lg`}>
            {serviceName.charAt(0)}
          </div>
          <div>
            <h3 className="font-semibold text-gray-900">{serviceName}</h3>
            <span className={`text-xs px-2 py-1 rounded-full ${statusColor}`}>
              {statusLabel}
            </span>
          </div>
        </div>
      </div>

      {/* Account Info */}
      {connection.serviceAccountProfileImageUrl && (
        <img
          src={connection.serviceAccountProfileImageUrl}
          alt="Profile"
          className="w-16 h-16 rounded-full mx-auto mb-3"
        />
      )}

      <div className="text-sm text-gray-600 space-y-1 mb-4">
        {connection.serviceAccountDisplayName && (
          <p className="font-medium text-gray-900">{connection.serviceAccountDisplayName}</p>
        )}
        {connection.serviceAccountEmail && (
          <p className="truncate">{connection.serviceAccountEmail}</p>
        )}
        <p>Connected: {formattedDate}</p>
        {connection.lastSyncedAt && (
          <p>Last synced: {new Date(connection.lastSyncedAt).toLocaleString()}</p>
        )}
      </div>

      {/* Token Expiration Warning */}
      {isExpiringSoon && connection.connectionStatus === ConnectionStatus.Active && (
        <div className="bg-yellow-50 border border-yellow-200 text-yellow-800 text-xs px-3 py-2 rounded mb-4">
          Token expires soon - will auto-refresh
        </div>
      )}

      {/* Actions */}
      <button
        onClick={() => onDisconnect(connection.id)}
        className="w-full bg-red-600 text-white py-2 px-4 rounded-lg hover:bg-red-700 transition-colors text-sm font-medium"
      >
        Disconnect
      </button>
    </div>
  );
}
