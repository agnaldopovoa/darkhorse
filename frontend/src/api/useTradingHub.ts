import { useEffect, useRef, useState } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export const useTradingHub = () => {
    const [connection, setConnection] = useState<HubConnection | null>(null);
    const [status, setStatus] = useState<'disconnected' | 'connecting' | 'connected'>('disconnected');
    const isConnecting = useRef(false);

    useEffect(() => {
        const token = localStorage.getItem('accessToken');
        if (!token || isConnecting.current) return;

        isConnecting.current = true;
        setStatus('connecting');

        const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000';
        
        const newConnection = new HubConnectionBuilder()
            .withUrl(`${baseUrl}/hubs/trading`, {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        newConnection.start()
            .then(() => {
                setStatus('connected');
                setConnection(newConnection);
            })
            .catch(e => {
                console.error('SignalR Connection Error: ', e);
                setStatus('disconnected');
            })
            .finally(() => {
                isConnecting.current = false;
            });

        newConnection.onreconnecting(() => setStatus('connecting'));
        newConnection.onreconnected(() => setStatus('connected'));
        newConnection.onclose(() => setStatus('disconnected'));

        return () => {
            newConnection.stop();
        };
    }, []);

    return { connection, status };
};
