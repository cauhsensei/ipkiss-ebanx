# Ipkiss

API de contas em memória para o desafio da EBANX, em .NET 10.

## Como rodar

Requisito: [.NET SDK 10](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/Ipkiss.Api
```

A API sobe em `http://localhost:5000`.

Testes:

```bash
dotnet test
```

Com Docker:

```bash
docker build -t ipkiss .
docker run --rm -p 8080:8080 ipkiss
```

## Endpoints

| Requisição | Sucesso | Erro |
|---|---|---|
| `POST /reset` | `200 OK` | - |
| `GET /balance?account_id={id}` | `200` com o saldo | `404 0` se a conta não existe |
| `POST /event` depósito | `201 {"destination": {...}}` | `400 0` se o valor for inválido |
| `POST /event` saque | `201 {"origin": {...}}` | `404 0` conta inexistente, `400 0` saldo insuficiente |
| `POST /event` transferência | `201 {"origin": {...}, "destination": {...}}` | `404 0` origem inexistente, `400 0` saldo insuficiente |

## Estrutura

```
src/Ipkiss.Api
  Accounts/   regras de negócio (Bank, Account, exceções de domínio)
  Http/       endpoints: traduzem requisição e exceções para status e corpo
  Program.cs  composição
tests/Ipkiss.Tests
  Accounts/   testes unitários do Bank, incluindo concorrência
  Http/       testes ponta a ponta; um deles reproduz o roteiro do Ipkiss Tester
```

## Decisões

- **Estado em memória.** Durabilidade não é requisito. `Bank` é um singleton com um `Dictionary` de saldos.
- **Um único lock.** Toda leitura e escrita passa pelo mesmo lock. A transferência debita e credita dentro dele, então é atômica e não existe janela em que o dinheiro sumiu de uma conta e ainda não chegou na outra. Para o volume do desafio, um lock global é mais simples e suficiente.
- **Validação antes de mutar.** Cada operação confere tudo (conta de origem, saldo e limite do `decimal` no destino) antes de alterar qualquer saldo, então uma operação que falha não deixa estado parcial e não cria conta.
- **`decimal` para dinheiro.** Evita erro de arredondamento de ponto flutuante.
- **Contas nascem no primeiro depósito** (ou ao receber uma transferência), como pede a especificação. Saque e transferência exigem origem existente.
- **Casos fora do roteiro do teste.** Saldo insuficiente, valor menor ou igual a zero, saldo que passaria do limite do `decimal`, tipo de evento desconhecido, campo obrigatório ausente e corpo malformado retornam `400` com corpo `0`, no mesmo formato do `404 0` da especificação. Erros gerados pelo próprio ASP.NET (JSON inválido, `account_id` ausente, rota inexistente) passam pelo `UseStatusCodePages` e seguem o mesmo formato.
- **Exceções de domínio.** O `Bank` sinaliza falhas com exceções específicas, e a camada HTTP é o único lugar que decide o status de cada uma.
- **GET sem efeito colateral.** Consultar uma conta inexistente não a cria. Há teste cobrindo isso.
