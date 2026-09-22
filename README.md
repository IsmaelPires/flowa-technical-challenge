# Flowa — FIX 4.4 Order Challenge

Solução do desafio composta por duas aplicações C#/.NET 8 que se comunicam exclusivamente pelo protocolo FIX 4.4 com [QuickFIX/n](https://quickfixn.org/):

- **OrderGenerator**: aplicação ASP.NET Core com frontend React e iniciador FIX. Recebe a ordem, envia um `NewOrderSingle (35=D)` e apresenta o `ExecutionReport` recebido.
- **OrderAccumulator**: aplicação de console e aceitador FIX. Mantém a exposição por símbolo e responde com `ExecutionReport (35=8)` de aceite ou rejeição.

## Decisões de implementação

- O estado da exposição é isolado por símbolo e protegido contra concorrência.
- Uma compra soma `preço × quantidade`; uma venda subtrai o mesmo valor.
- O limite de **R$ 100.000.000,00** é inclusivo: uma exposição exatamente no limite é aceita; somente valores absolutos superiores são rejeitados.
- Uma ordem rejeitada não altera o estado.
- Valores monetários usam `decimal`, evitando imprecisão de ponto flutuante.
- O Generator repete todas as validações no servidor, independentemente das validações do navegador.
- Cada requisição aguarda sua resposta pelo `ClOrdID`, com timeout de 10 segundos.
- O `ClOrdID` também é a chave de idempotência: uma retransmissão idêntica recebe a decisão original sem alterar novamente a exposição; a reutilização com outro conteúdo é rejeitada.
- As duas sessões validam as mensagens com o Data Dictionary oficial do FIX 4.4 distribuído pelo pacote QuickFIX/n.
- O campo FIX privado `9000` transporta a exposição corrente na resposta para exibição na interface.

## Pré-requisitos

- .NET SDK 8.0
- Node.js 20.19+ (para compilar o frontend React)

O `global.json` fixa a família do SDK e os pacotes NuGet são restaurados automaticamente.

## Como executar

Abra dois terminais na raiz do repositório.

Primeiro, inicie o acumulador:

```powershell
dotnet run --project src/OrderAccumulator
```

Compile o frontend React na primeira execução:

```powershell
cd src/OrderGenerator/ClientApp
npm install
npm run build
cd ../../..
```

Depois, inicie o gerador:

```powershell
dotnet run --project src/OrderGenerator
```

Acesse [http://localhost:5080](http://localhost:5080). O indicador no topo ficará verde após o logon FIX. O acumulador escuta em `127.0.0.1:5001`.

## Testes e build

```powershell
dotnet build Flowa.Orders.sln --configuration Release
dotnet test Flowa.Orders.sln --configuration Release
```

Os testes cobrem compras, vendas, limite positivo e negativo, igualdade ao limite, idempotência, validação defensiva dos campos FIX e os `ExecutionReport` de aceite e rejeição.

## Estrutura

```text
src/
├── OrderGenerator/      # Web API, frontend React e iniciador FIX
├── OrderAccumulator/    # Aceitador FIX e criação dos ExecutionReports
└── Orders.Domain/       # Regras de ordem e livro de exposição
tests/
└── Orders.Tests/        # Testes unitários do domínio
```

As configurações das sessões ficam em `Config/initiator.cfg` e `Config/acceptor.cfg`. Mensagens e estado de sessão do QuickFIX/n são gravados nas pastas locais `logs/` e `store/`, ignoradas pelo Git.

## Fluxo FIX

1. O navegador envia a ordem à API do OrderGenerator.
2. O Generator cria um `NewOrderSingle` limitado (`OrdType=2`) com `ClOrdID` único.
3. O Accumulator calcula a exposição projetada sob exclusão mútua.
4. Se `abs(exposição projetada) <= 100.000.000`, responde `ExecType=New` e efetiva a exposição.
5. Caso contrário, responde `ExecType=Rejected` e preserva a exposição anterior.

## Premissas e trade-offs

### Exposição no aceite

O texto do desafio determina que uma ordem aceita deve ser considerada na exposição e respondida com `ExecType=New`. Em um fluxo de negociação real, `New` representa o aceite da ordem, não uma execução. A solução segue explicitamente a regra do desafio e contabiliza a quantidade integral no aceite. Se o sistema evoluísse para execuções parciais, a exposição passaria a ser atualizada por `LastQty` nos relatórios de execução.

### Campo de exposição

O `ExecutionReport` padrão não possui um campo específico para a exposição financeira interna. Foi adotado o campo privado `9000` apenas para que o Generator consiga apresentar esse valor sem criar um canal paralelo entre as aplicações. `ValidateUserDefinedFields=N` permite esse campo, enquanto o restante da mensagem continua validado pelo Data Dictionary FIX 4.4.

### Persistência

A exposição e o histórico de idempotência são mantidos em memória e reiniciam junto com o OrderAccumulator. Isso mantém o escopo aderente ao desafio. Em produção, ambos seriam persistidos de forma transacional para suportar reinício e múltiplas instâncias.

### Escalabilidade

A atualização é atômica dentro de uma instância. Para múltiplas instâncias do Accumulator, o livro de exposição precisaria de uma fonte de verdade compartilhada com controle de concorrência, como um banco transacional com versionamento otimista.

## Possíveis evoluções

- Persistir exposição, decisões e auditoria das ordens.
- Publicar métricas de sessão FIX, latência e rejeições.
- Suportar execução parcial e cancelamento de ordens.
- Adicionar autenticação e TLS à comunicação entre os componentes.
