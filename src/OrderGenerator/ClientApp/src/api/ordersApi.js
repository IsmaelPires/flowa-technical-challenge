async function readError(response) {
  const payload = await response.json().catch(() => ({}));
  return payload.detail
    ?? Object.values(payload.errors ?? {}).flat().join(' ')
    ?? 'Não foi possível processar a solicitação.';
}

export async function getConnectionStatus(signal) {
  const response = await fetch('/api/health', { signal });
  if (!response.ok) throw new Error(await readError(response));
  return response.json();
}

export async function createOrder(order, signal) {
  const response = await fetch('/api/orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(order),
    signal
  });

  if (!response.ok) throw new Error(await readError(response));
  return response.json();
}
