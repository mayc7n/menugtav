# Prompt estruturado: melhorias do Mike Menu

Este documento registra o pedido e os critérios para orientar a evolução do projeto. Ele descreve objetivos, não uma lista de recursos já entregues: confirme o que foi efetivamente implementado no código, no histórico de alterações e nas notas da versão.

## Contexto e objetivo

Evoluir o Mike Menu, um trainer em português para GTA V Legacy no Windows, em modo história, usando a API v3 do ScriptHookVDotNet. Tornar mais fácil escolher e personalizar armas, veículos e aparência do personagem, preservando os controles existentes e a instalação por `scripts/MikeMenu.3.dll`.

## Solicitação

1. **Personagem:** ampliar ações úteis de recuperação e personalização, mantendo verificações do estado do jogador e mensagens claras quando uma ação estiver indisponível.
2. **Armas:** melhorar a seleção e as opções de munição ou personalização conforme os recursos aceitos pela versão do jogo e pela API. Validar disponibilidade antes de aplicar modificações.
3. **Veículos:** facilitar a escolha e ampliar os recursos para o veículo atual, consultando as peças e os modelos realmente disponíveis. Evitar criar entidades repetidas apenas por navegar no menu.
4. **Roupas e trajes:** permitir explorar componentes e variações disponíveis para o personagem atual. Informar como visualizar e aplicar a escolha e tratar componentes sem variações; não assumir que uma roupa existe em todos os personagens.
5. **Prévia visual:** exibir ilustrações ao lado da opção selecionada quando houver recursos visuais adequados e disponíveis no jogo. Onde não houver imagem confiável, usar uma indicação clara ou uma prévia no próprio personagem, sem prometer imagem para todos os itens. Não baixar recursos externos durante o jogo.
6. **Experiência:** manter navegação por teclado, textos legíveis em português e retorno claro para ações bem-sucedidas ou indisponíveis. Não obstruir a navegação quando uma prévia falhar.

## Restrições e implementação

- Manter o escopo no modo história e preservar as verificações existentes de sessão, carregamento e troca de personagem.
- Usar os padrões e dependências existentes; a disponibilidade de modelos, peças, armas e variações depende da instalação e do conteúdo carregado.
- Manter a correspondência entre páginas, opções e ações do menu, incluindo retorno e listas vazias.
- Evitar mudanças irreversíveis sem indicação adequada. Explicar o comportamento de prévias e como cancelar ou restaurar uma alteração quando aplicável.
- Compilar a DLL e atualizar os artefatos distribuídos somente a partir da versão validada. Preservar arquivos e configurações locais do usuário.

## Critérios de validação

- Executar os testes de regras e a compilação disponíveis no projeto; inspecionar os avisos e corrigir falhas relacionadas às mudanças.
- Conferir a navegação de cada página alterada, os limites das listas e as ações para itens ou recursos ausentes.
- Conferir consistência entre código, DLL, ZIP e documentação de instalação antes da distribuição.
- Testar no GTA V Legacy em modo história quando a execução do jogo estiver disponível, incluindo troca de personagem e indisponibilidade de modelos ou peças. Testes fora do jogo não comprovam o comportamento dos nativos ou das prévias em tempo de execução.
- Relatar de forma explícita o que foi implementado, o que ficou pendente e quais verificações foram ou não executadas.
