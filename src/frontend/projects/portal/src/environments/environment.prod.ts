export const environment = {
  production: true,
  apiUrl: '/api',  // Same relative path - works in production too
  gatewayUrl: '',  // Empty means use same origin
  features: {
    enableDebug: false,
    enableLogging: false
  }
};
