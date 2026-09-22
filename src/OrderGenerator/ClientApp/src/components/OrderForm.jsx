import { useMemo, useState } from 'react';

const symbols = ['PETR4', 'VALE3', 'VIIA4'];

export function OrderForm({ connected, submitting, onSubmit }) {
  const [order, setOrder] = useState({ symbol: 'PETR4', side: 'Buy', quantity: 100, price: 32.5 });
  const notional = useMemo(() => order.quantity * order.price, [order.quantity, order.price]);
  const [error, setError] = useState('');

  function update(field, value) {
    setOrder(current => ({ ...current, [field]: value }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError('');

    if (!Number.isInteger(order.quantity) || order.quantity <= 0 || order.quantity >= 100_000) {
      setError('A quantidade deve ser um número inteiro entre 1 e 99.999.');
      return;
    }
    const cents = order.price * 100;
    if (order.price <= 0 || order.price >= 1_000 || Math.abs(Math.round(cents) - cents) > Number.EPSILON * 100) {
      setError('O preço deve ser um múltiplo de R$ 0,01 entre R$ 0,01 e R$ 999,99.');
      return;
    }

    try {
      await onSubmit(order);
    } catch (exception) {
      setError(exception.message);
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <fieldset className="field">
        <legend>Símbolo</legend>
        <div className="segmented">
          {symbols.map(symbol => (
            <label key={symbol} className={order.symbol === symbol ? 'selected' : ''}>
              <input type="radio" name="symbol" value={symbol} checked={order.symbol === symbol} onChange={() => update('symbol', symbol)} />
              {symbol}
            </label>
          ))}
        </div>
      </fieldset>

      <fieldset className="field">
        <legend>Lado</legend>
        <div className="side-options">
          <label className={order.side === 'Buy' ? 'selected' : ''}>
            <input type="radio" name="side" checked={order.side === 'Buy'} onChange={() => update('side', 'Buy')} />
            <strong>Compra</strong><small>Aumenta a exposição</small>
          </label>
          <label className={`sell ${order.side === 'Sell' ? 'selected' : ''}`}>
            <input type="radio" name="side" checked={order.side === 'Sell'} onChange={() => update('side', 'Sell')} />
            <strong>Venda</strong><small>Reduz a exposição</small>
          </label>
        </div>
      </fieldset>

      <div className="grid">
        <label className="field">
          <span>Quantidade</span>
          <input type="number" min="1" max="99999" step="1" value={order.quantity} onChange={event => update('quantity', Number(event.target.value))} required />
          <small>1 — 99.999</small>
        </label>
        <label className="field">
          <span>Preço unitário</span>
          <div className="money"><span>R$</span><input type="number" min="0.01" max="999.99" step="0.01" value={order.price} onChange={event => update('price', Number(event.target.value))} required /></div>
          <small>Múltiplos de R$ 0,01</small>
        </label>
      </div>

      <div className="notional"><span>Valor financeiro</span><strong>{formatCurrency(notional)}</strong></div>
      <p className="error" role="alert">{error}</p>
      <button type="submit" disabled={submitting || !connected}>
        <span>{submitting ? 'Enviando…' : connected ? 'Enviar ordem' : 'Aguardando conexão FIX'}</span><b>→</b>
      </button>
    </form>
  );
}

export const formatCurrency = value => new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
