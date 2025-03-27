/**
 * SignalR Configuration
 * This file provides a centralized configuration for SignalR connections
 * to ensure consistency across the application.
 */

const SignalRConfig = {
    // Hub URL - matches the URL mapped in Program.cs
    hubUrl: '/parkingHub',
    
    // Create a new connection with consistent configuration
    createConnection: function() {
        return new signalR.HubConnectionBuilder()
            .withUrl(this.hubUrl)
            .withAutomaticReconnect()
            .build();
    },
    
    // Start connection with error handling and retry logic
    startConnection: function(connection, onConnected, onError) {
        connection.start()
            .then(() => {
                console.log('SignalR connected successfully');
                if (typeof onConnected === 'function') {
                    onConnected();
                }
            })
            .catch(err => {
                console.error('SignalR connection error:', err);
                if (typeof onError === 'function') {
                    onError(err);
                }
                // Retry connection after 5 seconds
                setTimeout(() => this.startConnection(connection, onConnected, onError), 5000);
            });
    }
};