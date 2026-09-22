import { useEffect, useState } from 'react';
import { getConnectionStatus } from '../api/ordersApi';

export function useFixConnection() {
  const [status, setStatus] = useState('checking');

  useEffect(() => {
    let active = true;

    async function refresh() {
      try {
        const result = await getConnectionStatus();
        if (active) setStatus(result.status);
      } catch {
        if (active) setStatus('unavailable');
      }
    }

    refresh();
    const interval = window.setInterval(refresh, 4_000);
    return () => {
      active = false;
      window.clearInterval(interval);
    };
  }, []);

  return status;
}
