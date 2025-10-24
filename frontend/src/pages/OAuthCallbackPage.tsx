import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../services/api';
import { ServiceType } from '../types/services';

export function OAuthCallbackPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [error, setError] = useState('');
  const [isProcessing, setIsProcessing] = useState(true);

  useEffect(() => {
    handleOAuthCallback();
  }, []);

  const handleOAuthCallback = async () => {
    try {
      // Get OAuth parameters from URL
      const code = searchParams.get('code');
      const state = searchParams.get('state');
      const errorParam = searchParams.get('error');

      // Check for OAuth errors
      if (errorParam) {
        setError(`OAuth error: ${errorParam}`);
        setIsProcessing(false);
        return;
      }

      if (!code || !state) {
        setError('Invalid OAuth callback - missing code or state');
        setIsProcessing(false);
        return;
      }

      // Validate state matches stored state
      const storedState = sessionStorage.getItem('oauthState');
      const storedServiceType = sessionStorage.getItem('oauthServiceType');

      if (!storedState || state !== storedState) {
        setError('Invalid state parameter - possible CSRF attack');
        setIsProcessing(false);
        return;
      }

      if (!storedServiceType) {
        setError('Service type not found in session');
        setIsProcessing(false);
        return;
      }

      const serviceType = parseInt(storedServiceType) as ServiceType;

      // Complete OAuth flow
      await api.completeOAuth({
        serviceType,
        code,
        state,
        redirectUri: `${window.location.origin}/auth/callback`,
      });

      // Clear session storage
      sessionStorage.removeItem('oauthState');
      sessionStorage.removeItem('oauthServiceType');

      // Redirect to dashboard
      navigate('/dashboard', { replace: true });
    } catch (err: any) {
      console.error('OAuth callback error:', err);
      setError(err.response?.data?.error || 'Failed to complete OAuth flow');
      setIsProcessing(false);
    }
  };

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100">
        <div className="bg-white p-8 rounded-lg shadow-lg max-w-md w-full">
          <div className="text-red-600 text-center mb-4">
            <svg
              className="mx-auto h-12 w-12"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
          </div>
          <h2 className="text-xl font-bold text-gray-900 text-center mb-2">
            Connection Failed
          </h2>
          <p className="text-gray-600 text-center mb-6">{error}</p>
          <button
            onClick={() => navigate('/dashboard')}
            className="w-full bg-purple-600 text-white py-2 px-4 rounded-lg hover:bg-purple-700 transition-colors"
          >
            Return to Dashboard
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100">
      <div className="bg-white p-8 rounded-lg shadow-lg max-w-md w-full text-center">
        <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600 mb-4"></div>
        <h2 className="text-xl font-bold text-gray-900 mb-2">
          {isProcessing ? 'Connecting Service...' : 'Processing...'}
        </h2>
        <p className="text-gray-600">
          Please wait while we complete the connection
        </p>
      </div>
    </div>
  );
}
