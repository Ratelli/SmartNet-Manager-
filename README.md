
Criação do Projeto Prático Avaliativo: SmartNet Manager

Produção: Rafael Lima

```
SmartNetManager/
├── Modelos/
│   ├── Dispositivo.cs            # classe base do enunciado
│   └── ResultadoOperacao.cs      # retorno padronizado (sucesso + mensagem)
├── Servicos/
│   ├── ServidorDhcp.cs           # Missão 1 – Dictionary<string,string>
│   ├── GerenciadorRede.cs        # Missões 2, 3 e 6 – Queue, List, Firewall
│   └── HistoricoAuditoria.cs     # Missão 4 – Stack<string> + rollback real
├── Relatorios/
│   ├── Dashboard.cs              # Missão 3 – estatísticas com LINQ
│   └── Relatorio.cs              # Missão 5 – ImprimirRelatorio(IEnumerable<Dispositivo>)
├── Interface/
│   ├── Menu.cs                   # menu interativo + lambdas do firewall
│   └── DadosIniciais.cs          # massa de demonstração
├── Utilitarios/
│   └── EnderecoRede.cs           # validação de MAC/IP
└── Program.cs
```

## Checklist das missões

- Missão 1 – `Dictionary<string,string>` (MAC → IP), sem MAC duplicado
- Missão 2 – `Queue<Dispositivo>`, "Adicionar à fila" e "Processar fila" com `Dequeue()`
- Missão 3 – `List<Dispositivo>` + Dashboard LINQ: consumo total, Top 3, por fabricante, alerta > 500 Mbps
- Missão 4 – `Stack<string>` registrando autenticações e firewall; "Desfazer" com `Pop()`
- Missão 5 – `ImprimirRelatorio(IEnumerable<Dispositivo>)` recebendo a Queue e a List
- Missão 6 – `AplicarFirewall(Func<Dispositivo,bool>)` chamado com lambdas no menu

## O que foi entrege além do pedido como sugestão do GPT

- IP entregue na autenticação: reserva DHCP se existir, senão faixa dinâmica `192.168.0.100–254`
- Rollback **real**: desfazer autenticação devolve o dispositivo ao início da fila; desfazer firewall o devolve à mesma posição na lista
- Firewall com regras digitadas pelo usuário (fabricante e limite de banda) via closures
- Validação de MAC/IP e normalização do MAC (maiúsculas, `-` → `:`)

## Não consegui tempo para tentar implementar

- Digitar 0(zero) em qualquer menu para voltar ao menu anterior. (Só pensei nisso durante os testes)