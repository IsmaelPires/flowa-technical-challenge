import { formatCurrency } from './OrderForm';

export function OrderResult({ result }) {
  if (!result) {
    return (
      <aside aria-live="polite">
        <div className="empty-state">
          <div className="pulse">↗</div>
          <h2>Aguardando ordem</h2>
          <p>A resposta do ExecutionReport será exibida aqui.</p>
        </div>
      </aside>
    );
  }

  const accepted = result.status === 'Accepted';
  return (
    <aside aria-live="polite">
      <div className="result-card">
        <span className={`status ${accepted ? '' : 'rejected'}`}>{accepted ? 'ACEITA · NEW' : 'REJEITADA'}</span>
        <h2>{accepted ? 'Ordem confirmada' : 'Ordem recusada'}</h2>
        <p>{result.message}</p>
        <div className="details">
          <div><span>ClOrdID</span><strong>{result.clientOrderId}</strong></div>
          <div><span>Símbolo</span><strong>{result.order.symbol}</strong></div>
          <div><span>Financeiro</span><strong>{formatCurrency(result.order.quantity * result.order.price)}</strong></div>
          <div><span>Exposição atual</span><strong>{result.exposure == null ? '—' : formatCurrency(result.exposure)}</strong></div>
        </div>
      </div>
    </aside>
  );
}
