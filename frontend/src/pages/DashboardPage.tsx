import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { api } from '../services/api';
import { ServiceConnection, ServiceType } from '../types/services';
import { ServiceConnectionCard } from '../components/ServiceConnectionCard';
import { ConnectServiceButton } from '../components/ConnectServiceButton';

export function DashboardPage() {
  const { user, logout } = useAuth();
  const [connections, setConnections] = useState<ServiceConnection[]>([]);
  const [playlists, setPlaylists] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingPlaylists, setIsLoadingPlaylists] = useState(false);
  const [isSyncing, setIsSyncing] = useState(false);
  const [error, setError] = useState('');
  const [view, setView] = useState<'connections' | 'playlists'>('connections');

  useEffect(() => {
    loadConnections();
  }, []);

  useEffect(() => {
    if (connections.length > 0 && view === 'playlists') {
      loadPlaylists();
    }
  }, [view, connections.length]);

  const loadConnections = async () => {
    try {
      setIsLoading(true);
      const data = await api.getConnections();
      setConnections(data);
    } catch (err: any) {
      setError('Failed to load service connections');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const loadPlaylists = async () => {
    try {
      setIsLoadingPlaylists(true);
      const data = await api.getPlaylists();
      setPlaylists(data);
    } catch (err: any) {
      setError('Failed to load playlists');
      console.error(err);
    } finally {
      setIsLoadingPlaylists(false);
    }
  };

  const handleSyncAll = async () => {
    try {
      setIsSyncing(true);
      setError('');
      await api.syncAllPlaylists();
      await loadPlaylists();
    } catch (err: any) {
      setError('Failed to sync playlists');
      console.error(err);
    } finally {
      setIsSyncing(false);
    }
  };

  const handleDisconnect = async (connectionId: string) => {
    if (!confirm('Are you sure you want to disconnect this service?')) {
      return;
    }

    try {
      await api.disconnectService(connectionId);
      await loadConnections();
    } catch (err: any) {
      setError('Failed to disconnect service');
      console.error(err);
    }
  };

  const handleConnect = async (serviceType: ServiceType) => {
    try {
      const response = await api.initiateOAuth({
        serviceType,
        redirectUri: `${window.location.origin}/auth/callback`,
      });

      sessionStorage.setItem('oauthState', response.state);
      sessionStorage.setItem('oauthServiceType', serviceType.toString());

      window.location.href = response.authorizationUrl;
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to initiate OAuth flow');
      console.error(err);
    }
  };

  const groupedConnections = connections.reduce((acc, conn) => {
    const type = conn.serviceType;
    if (!acc[type]) acc[type] = [];
    acc[type].push(conn);
    return acc;
  }, {} as Record<ServiceType, ServiceConnection[]>);

  const serviceNames: Record<ServiceType, string> = {
    [ServiceType.Spotify]: 'Spotify',
    [ServiceType.AppleMusic]: 'Apple Music',
    [ServiceType.Deezer]: 'Deezer',
    [ServiceType.YouTubeMusic]: 'YouTube Music',
  };

  return (
    <div className="min-h-screen bg-gray-100">
      {/* Header */}
      <header className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <div className="flex justify-between items-center">
            <h1 className="text-2xl font-bold text-gray-900">UniPlay Dashboard</h1>
            <div className="flex items-center gap-4">
              <span className="text-gray-600">{user?.email}</span>
              <button
                onClick={logout}
                className="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700 transition-colors"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-6">
            {error}
          </div>
        )}

        {/* View Tabs */}
        <div className="mb-6 border-b border-gray-200">
          <nav className="-mb-px flex space-x-8">
            <button
              onClick={() => setView('connections')}
              className={`${
                view === 'connections'
                  ? 'border-purple-600 text-purple-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
            >
              Connections
            </button>
            <button
              onClick={() => setView('playlists')}
              className={`${
                view === 'playlists'
                  ? 'border-purple-600 text-purple-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
            >
              Playlists ({playlists.length})
            </button>
          </nav>
        </div>

        {/* Connections View */}
        {view === 'connections' && (
          <>
            {/* Connect Services Section */}
            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-800 mb-4">Connect Music Services</h2>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                <ConnectServiceButton
                  serviceType={ServiceType.Spotify}
                  serviceName="Spotify"
                  onConnect={handleConnect}
                  connectionCount={groupedConnections[ServiceType.Spotify]?.length || 0}
                  maxConnections={3}
                />
                <ConnectServiceButton
                  serviceType={ServiceType.AppleMusic}
                  serviceName="Apple Music"
                  onConnect={handleConnect}
                  connectionCount={groupedConnections[ServiceType.AppleMusic]?.length || 0}
                  maxConnections={3}
                  disabled
                />
                <ConnectServiceButton
                  serviceType={ServiceType.Deezer}
                  serviceName="Deezer"
                  onConnect={handleConnect}
                  connectionCount={groupedConnections[ServiceType.Deezer]?.length || 0}
                  maxConnections={3}
                  disabled
                />
                <ConnectServiceButton
                  serviceType={ServiceType.YouTubeMusic}
                  serviceName="YouTube Music"
                  onConnect={handleConnect}
                  connectionCount={groupedConnections[ServiceType.YouTubeMusic]?.length || 0}
                  maxConnections={3}
                />
              </div>
            </section>

            {/* Connected Services Section */}
            <section>
              <h2 className="text-xl font-semibold text-gray-800 mb-4">Your Connected Services</h2>

              {isLoading ? (
                <div className="text-center py-12">
                  <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600"></div>
                  <p className="mt-4 text-gray-600">Loading connections...</p>
                </div>
              ) : connections.length === 0 ? (
                <div className="bg-white rounded-lg shadow p-8 text-center">
                  <p className="text-gray-600">No services connected yet. Connect a service above to get started!</p>
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {connections.map((connection) => (
                    <ServiceConnectionCard
                      key={connection.id}
                      connection={connection}
                      onDisconnect={handleDisconnect}
                    />
                  ))}
                </div>
              )}
            </section>
          </>
        )}

        {/* Playlists View */}
        {view === 'playlists' && (
          <section>
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-xl font-semibold text-gray-800">All Playlists</h2>
              <button
                onClick={handleSyncAll}
                disabled={isSyncing || connections.length === 0}
                className="bg-purple-600 text-white px-4 py-2 rounded-lg hover:bg-purple-700 transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed flex items-center gap-2"
              >
                {isSyncing ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    Syncing...
                  </>
                ) : (
                  'Sync All Playlists'
                )}
              </button>
            </div>

            {connections.length === 0 ? (
              <div className="bg-white rounded-lg shadow p-8 text-center">
                <p className="text-gray-600">Connect a service first to view playlists</p>
              </div>
            ) : isLoadingPlaylists ? (
              <div className="text-center py-12">
                <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600"></div>
                <p className="mt-4 text-gray-600">Loading playlists...</p>
              </div>
            ) : playlists.length === 0 ? (
              <div className="bg-white rounded-lg shadow p-8 text-center">
                <p className="text-gray-600 mb-4">No playlists found. Click "Sync All Playlists" to fetch your playlists.</p>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                {playlists.map((playlist) => (
                  <div key={playlist.id} className="bg-white rounded-lg shadow-md overflow-hidden hover:shadow-lg transition-shadow">
                    {playlist.imageUrl && (
                      <img
                        src={playlist.imageUrl}
                        alt={playlist.name}
                        className="w-full h-48 object-cover"
                      />
                    )}
                    <div className="p-4">
                      <div className="flex items-center gap-2 mb-2">
                        <span className="text-xs font-semibold text-purple-600">
                          {serviceNames[playlist.serviceType]}
                        </span>
                        {playlist.isPublic && (
                          <span className="text-xs bg-green-100 text-green-800 px-2 py-0.5 rounded">
                            Public
                          </span>
                        )}
                      </div>
                      <h3 className="font-semibold text-gray-900 mb-1 truncate">{playlist.name}</h3>
                      {playlist.description && (
                        <p className="text-sm text-gray-600 mb-2 line-clamp-2">{playlist.description}</p>
                      )}
                      <div className="text-sm text-gray-500">
                        <p>{playlist.trackCount} tracks</p>
                        {playlist.ownerName && <p className="truncate">by {playlist.ownerName}</p>}
                      </div>
                      {playlist.serviceUrl && (
                        <a
                          href={playlist.serviceUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="mt-3 block text-center text-sm text-purple-600 hover:text-purple-700"
                        >
                          View on {serviceNames[playlist.serviceType]} →
                        </a>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </section>
        )}
      </main>
    </div>
  );
}
