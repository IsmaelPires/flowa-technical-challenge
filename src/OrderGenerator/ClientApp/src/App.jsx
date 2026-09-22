import { useState } from 'react';
import { createOrder } from './api/ordersApi';
import { Header } from './components/Header';
import { OrderForm } from './components/OrderForm';
import { OrderResult } from './components/OrderResult';
import { useFixConnection } from './hooks/useFixConnection';

export function App() {
  const connectionStatus = useFixConnection();
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState(null);

  async function submitOrder(order) {
    setSubmitting(true);
    try {
      const response = await createOrder(order);
      setResult({ ...response, order });
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main>
      <Header connectionStatus={connectionStatus} />
      <section className="hero">
        <p className="eyebrow">FIX 4.4 · NEW ORDER SINGLE</p>
        <h1>Enviar nova ordem</h1>
        <p className="subtitle">Configure a ordem e acompanhe a resposta do acumulador em tempo real.</p>
      </section>
      <section className="workspace">
        <OrderForm connected={connectionStatus === 'connected'} submitting={submitting} onSubmit={submitOrder} />
        <OrderResult result={result} />
      </section>
    </main>
  );
}
