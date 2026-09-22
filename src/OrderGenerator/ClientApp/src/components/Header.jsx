const labels = {
  checking: 'Verificando FIX',
  connected: 'FIX conectado',
  disconnected: 'FIX desconectado',
  unavailable: 'Serviço indisponível'
};

export function Header({ connectionStatus }) {
  return (
    <header>
      <div className="brand"><span>FL</span> Order desk</div>
      <div className={`connection ${connectionStatus === 'connected' ? 'online' : ''}`}>
        <i aria-hidden="true" />
        <span>{labels[connectionStatus]}</span>
      </div>
    </header>
  );
}
