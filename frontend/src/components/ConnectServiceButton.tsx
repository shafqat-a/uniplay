import { ServiceType } from '../types/services';

interface Props {
  serviceType: ServiceType;
  serviceName: string;
  onConnect: (serviceType: ServiceType) => void;
  connectionCount: number;
  maxConnections: number;
  disabled?: boolean;
}

const serviceColors: Record<ServiceType, string> = {
  [ServiceType.Spotify]: 'from-green-500 to-green-600 hover:from-green-600 hover:to-green-700',
  [ServiceType.AppleMusic]: 'from-red-500 to-red-600 hover:from-red-600 hover:to-red-700',
  [ServiceType.Deezer]: 'from-orange-500 to-orange-600 hover:from-orange-600 hover:to-orange-700',
  [ServiceType.YouTubeMusic]: 'from-red-600 to-red-700 hover:from-red-700 hover:to-red-800',
};

export function ConnectServiceButton({
  serviceType,
  serviceName,
  onConnect,
  connectionCount,
  maxConnections,
  disabled = false,
}: Props) {
  const canConnect = connectionCount < maxConnections && !disabled;
  const colorClasses = serviceColors[serviceType];

  return (
    <button
      onClick={() => canConnect && onConnect(serviceType)}
      disabled={!canConnect}
      className={`
        relative overflow-hidden rounded-lg p-6 text-white transition-all
        ${canConnect
          ? `bg-gradient-to-br ${colorClasses} shadow-lg hover:shadow-xl transform hover:-translate-y-1`
          : 'bg-gray-300 cursor-not-allowed'
        }
      `}
    >
      <div className="relative z-10">
        <h3 className="text-lg font-bold mb-2">{serviceName}</h3>
        <p className="text-sm opacity-90 mb-3">
          {connectionCount}/{maxConnections} connected
        </p>
        {disabled ? (
          <span className="text-xs bg-white bg-opacity-20 px-2 py-1 rounded">
            Coming Soon
          </span>
        ) : canConnect ? (
          <span className="text-sm font-medium">
            Click to Connect
          </span>
        ) : (
          <span className="text-xs bg-white bg-opacity-20 px-2 py-1 rounded">
            Max Connections Reached
          </span>
        )}
      </div>
    </button>
  );
}
