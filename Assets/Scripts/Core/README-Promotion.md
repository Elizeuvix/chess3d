# Promotion UI Integration

## Objetivo
Fornecer interface para escolha da peça de promoção (Queen, Rook, Bishop, Knight) quando um peão alcança a última fileira, evitando aplicar sempre dama.

## Componentes Criados
- `PromotionChoiceUI.cs`: Controla exibição de botões e callback da escolha.
- Atualização em `CoreInputController.cs`: Intercepta clique no destino que gera múltiplas promoções e abre a UI.
- Atualização em `MoveHighlightManager.cs`: Agrupa highlights por destino para não gerar 4 marcadores sobrepostos.
   - Agora aceita um campo público `synchronizer` (não precisa mais estar no mesmo GameObject do `BoardSynchronizer`).

## Como Configurar na Cena
1. Crie um `Canvas` (Screen Space - Overlay). Ajuste a escala conforme necessário.
2. Crie um `Panel` filho (ex: `PromotionPanel`) e adicione o componente `PromotionChoiceUI`.
3. Dentro do painel crie quatro `Button` filhos com nomes exatamente: `Queen`, `Rook`, `Bishop`, `Knight` (se quiser usar autoWire). Cada botão deve ter um `Text`/`TMP_Text` dentro exibindo a letra ou ícone.
4. (Opcional) Desative o painel no Inspector (o script já força `SetActive(false)` ao iniciar).
5. No objeto que contém `CoreInputController`, arraste a referência do `PromotionChoiceUI` para o campo `promotionUI`.
6. Ajuste (opcional) no `PromotionChoiceUI`:
   - `enableTimeoutFallback = true` para ativar fallback automático.
   - `fallbackSeconds` (default 5) para controlar o tempo antes de auto-escolher Dama.

## Fluxo de Execução
1. Usuário seleciona um peão que pode promover.
2. Ao clicar no quadrado de promoção, o `CoreInputController` detecta que existem várias jogadas com mesmo destino e `Promotion != None`.
3. A UI é mostrada via `promotionUI.Show(...)`.
4. Ao clicar num botão, a jogada correspondente é aplicada (`ApplyMove`).
5. Se timeout ocorre (caso ativado), aplica a promoção para Rainha.

## Fallback Sem UI
Se `promotionUI` não estiver atribuída, o sistema automaticamente promove para Dama (Queen).

## Extensões Futuras
- Mostrar miniaturas 3D das peças como botões.
- Suporte a teclado (Q/R/B/N) para seleção rápida.
- Animação de fade in/out.
- Internacionalização de rótulos.

## Teste Manual
1. Posicione um peão branco em `a7` (x=0,y=6) e um rei preto distante para não gerar cheques irracionais.
2. Faça o peão mover para `a8`.
3. Verifique exibição da UI.
4. Clique em cada opção e confirme que a peça correta aparece no tabuleiro resultante (repita reiniciando o estado).
5. Ative timeout e aguarde sem clicar para validar promoção automática para Dama.
6. Repita para um peão preto promovendo em `a1` (x=0,y=0) movendo na direção inversa.

## Notas Técnicas
- A seleção permanece ativa até finalizar a escolha; após aplicar move, highlights são limpos.
- `MoveHighlightManager` ignora duplicados pelo par `(ToX,ToY)`.
- O callback da UI reconcilia a lista de `promoMoves` e aplica o primeiro compatível; se não encontrar, usa o primeiro (seguro pela geração anterior).
 - Scripts legados como `PieceConfig` são desativados automaticamente no `Awake` para evitar conflitos de input.

## Segurança / Robustez
- Try/catch em volta de `ApplyMove` evita quebra do fluxo caso algo inesperado ocorra.
- Timeout impede travamento se UI ficar inacessível.

---
Qualquer dúvida ou melhoria desejada: adicionar item ao TODO e iterar.
