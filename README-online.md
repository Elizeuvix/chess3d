# Chess3D – Jogo Online (TCP)

Este guia rápido mostra como compilar e fazer um smoke test do modo online (host/cliente) usando duas instâncias do Unity/Build.

Requisitos
- Unity 2021.3.17f1 (conforme `ProjectSettings/ProjectVersion.txt`).
- Windows (testado).

Cenas/Componentes envolvidos
- `BoardSynchronizer` (já na cena do tabuleiro).
- `SimpleTcpTransport` + `OnlineMatchManager` em um GameObject (ex.: `OnlineManager`).
- Painel de UI `PanelTCPLobby` com `OnlineLobbyUI`.

Estrutura esperada do painel (nomes dos filhos)
- `StatusText` (TMP_Text)
- `ColorText` (TMP_Text)
- `TCPInputGroup/IPInputField` (TMP_InputField)
- `TCPInputGroup/PortInputField` (TMP_InputField)
- `TCPButtons/HostButton` (Button)
- `TCPButtons/JoinButton` (Button)
- `TCPButtons/ResetButton` (Button)
- `TCPButtons/ResignButton` (Button)

Build e teste rápido (duas instâncias)
1) Compile o jogo normalmente (File → Build). Uma instância será Host, outra Cliente.
2) Abra duas execuções do jogo OU uma execução + o Editor.
3) Na instância Host:
   - Clique em `Host`. O transporte ouvirá na porta configurada (padrão `7777`).
4) Na instância Cliente:
   - Digite o IP e a porta do Host (padrão `127.0.0.1:7777` para mesma máquina).
   - Clique em `Join`.
5) Indicadores:
   - `StatusText`: "Conectado"/"Desconectado".
   - `ColorText`: "Cor: Brancas/Negras" quando online.
6) Jogabilidade:
   - Somente a sua cor pode mover. `Undo/Redo` desabilitado em online.
   - `Reset`: Host reinicia o tabuleiro e envia FEN; Cliente solicita ao Host (RequestReset).
   - `Resign`: Envia desistência e reinicia.

Notas técnicas
- Transporte TCP é 1v1, UTF-8 delimitado por `\n`.
- Protocolos JSON (via `JsonUtility`): `Hello`, `Assign`, `Move`, `Reset`, `RequestReset`, `Resign`.
- O Host atribui cores (Host = Brancas por padrão); o Cliente assume a cor oposta.
- `BoardSynchronizer` publica eventos para atualizações de highlight e fim de jogo.

Dicas de diagnóstico
- Consulte o Console: logs `[TCP]` para rede e `[Online]` para lógica de partida.
- Sem conexão? Verifique firewall e porta (padrão `7777`). Teste `127.0.0.1` antes de LAN/Internet.

Personalização rápida
- Porta/IP padrão: ajuste no componente `SimpleTcpTransport`.
- Textos: altere as strings em `OnlineLobbyUI.cs`.

