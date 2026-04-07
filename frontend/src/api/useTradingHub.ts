import { useEffect, useRef, useState } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export const useTradingHub = () => {
    const [connection, setConnection] = useState<HubConnection | null>(null);
    const [status, setStatus] = useState<'disconnected' | 'connecting' | 'connected'>(() => {
        return localStorage.getItem('accessToken') ? 'connecting' : 'disconnected';
    });
    const isConnecting = useRef(false);

    useEffect(() => {
        const token = localStorage.getItem('accessToken');
        if (!token || isConnecting.current) return;

        isConnecting.current = true;

        const baseUrl = import.meta.env.VITE_DARKHORSE_API_URL || 'https://localhost:7000';

        const getCookie = (name: string) => {
            const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
            return match ? match[2] : null;
        };
        const csrfToken = getCookie('csrf_token');

        const newConnection = new HubConnectionBuilder()
            .withUrl(`${baseUrl}/hubs/trading`, {
                accessTokenFactory: () => token,
                headers: csrfToken ? { 'X-CSRF-Token': csrfToken } : undefined
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
