# Apresentação do Sebraelab VR

- **Vídeo:** [`Sebraelab_VR_Apresentacao.mp4`](Sebraelab_VR_Apresentacao.mp4) (1080p, 5min03s, sem som, com legendas na tela).
- **Slides:** [`Sebraelab_VR_Slides.pdf`](Sebraelab_VR_Slides.pdf) (21 slides, na mesma ordem do vídeo, sem legendas), para apresentar ao vivo.

O que é real e o que é ilustrativo:

- **Modelo 3D do Sebraelab:** é o real, do projeto (`Assets/Sebraelab3D`).
- **Cenas de jogo (VR e PC):** simulação feita com esse modelo e com a mesma lógica dos scripts (abre ao aproximar ou pegar e fecha 0,3 s depois de soltar; painel em pé virado para o jogador; luz de destaque). Não é gravação da Unity.
- **Telas da Unity:** reprodução da interface, com nomes de objetos, campos, menus, mensagens e código reais do projeto.
- **Vídeo real do jogo:** dá para gravar com um clique em *Sebraelab > Gravar vídeo de demonstração* (ver `Docs/CONFIGURACAO_UNITY.md`).

Abaixo está o texto de cada parte, para quem for falar durante a apresentação.

## 1. Abertura  ·  0:00

Imagine entrar no Sebraelab sem sair de casa. Andar pelo espaço, pegar um livro na mesa e, na hora, descobrir do que ele trata. Este é o Sebraelab VR, uma exposição interativa de livros em realidade virtual, criada para a disciplina de Tecnologias Emergentes.

## 2. A proposta  ·  0:20

A ideia nasceu de uma pergunta simples: como aproximar as pessoas dos livros e do ambiente de inovação do Sebrae, mesmo à distância? Com a realidade virtual, o visitante explora o espaço no próprio ritmo e interage com o conteúdo de um jeito muito mais marcante do que numa lista ou num site.

## 3. Pilares  ·  0:38

O projeto se apoia em quatro pilares. Primeiro, um cenário fiel ao Sebraelab de verdade. Depois, a interação natural, com as próprias mãos. Além disso, conteúdo multimídia em cada livro e uma arquitetura modular, pronta para receber minijogos.

## 4. Cultura de São Luís  ·  0:54

O projeto também valoriza a cultura de São Luís do Maranhão, a cidade dos azulejos, com um Centro Histórico que a UNESCO reconhece como Patrimônio Mundial. É a terra do Bumba-meu-boi, do Tambor de Crioula e do reggae, e também de grandes escritores, como Aluísio Azevedo, Josué Montello e Ferreira Gullar. Por isso, a exposição ganhou uma estante com autores do Maranhão e um painel inspirado nos azulejos do Centro Histórico.

## 5. Tecnologias  ·  1:22

Para construir tudo isso, usamos a Unity 6, com o Universal Render Pipeline (URP), pensado para óculos autônomos. A interação em VR vem do XR Interaction Toolkit, e o OpenXR conecta o jogo ao Meta Quest e a outros óculos. O cenário foi modelado no Blender, a lógica foi escrita em C#, e todo o projeto é versionado no GitHub.

## 6. O cenário (modelo 3D)  ·  1:46

Este é o cenário: uma réplica em 3D do Sebraelab, modelada pela equipe, com recepção, arquibancada, estações de trabalho e áreas de convivência. Na Unity, cada parte do modelo ganha colisão, para ninguém atravessar as paredes, e o piso é preparado para o teletransporte.

## 7. Jogabilidade em VR (simulação)  ·  2:06

Agora, veja a experiência pelos olhos do visitante. Com o analógico, ele anda pelo espaço. Ou, se preferir, aponta o controle para o chão e se teletransporta. Ao chegar perto de um estande, uma luz de destaque se acende e chama a atenção para os livros. Basta aproximar a mão de um livro para abrir um painel com o título, o autor e o resumo da obra. Ele pode pegar o livro, girar e olhar de perto. O painel continua em pé, virado para ele, e uma animação curta toca em loop. Quando ele solta o livro, o painel se fecha sozinho.

## 8. Modo PC (simulação)  ·  2:43

Para testar sem óculos, existe também o modo computador. As teclas W, A, S e D movem o jogador, e o mouse controla a visão. Mirando num livro, a tecla E abre o painel. E, se um óculos estiver conectado, a mesma cena muda sozinha para a realidade virtual.

## 9. Dentro da Unity: projeto e cena  ·  3:03

Dentro da Unity, o projeto é organizado em pastas: os scripts, o modelo do cenário, os vídeos e os dados de cada livro. A cena principal é montada automaticamente, com o jogador de realidade virtual, o cenário, o piso de teletransporte e os livros.

## 10. Dentro da Unity: livro e Dados do Livro  ·  3:20

Cada livro recebe física, colisão, o componente que permite pegá-lo com a mão (XR Grab Interactable) e o nosso script do painel. Os textos ficam num arquivo separado, o "Dados do Livro". Assim, qualquer pessoa da equipe edita o conteúdo sem tocar no código.

## 11. Código do painel  ·  3:35

Aqui está um trecho real do código. Quando a mão se aproxima ou pega o livro, o painel abre. Quando ela se afasta, o fechamento espera alguns décimos de segundo, para o painel não ficar piscando.

## 12. Automação na Unity  ·  3:48

Para acelerar o trabalho, criamos um menu próprio dentro da Unity. Clicando uma vez, o configurador instala os pacotes, ativa o OpenXR para o Meta Quest e monta a cena inteira. Outro clique transforma qualquer modelo 3D em um livro interativo. E um terceiro grava, sozinho, um vídeo de demonstração do jogo.

## 13. Arquitetura  ·  4:08

A arquitetura é modular: cada script tem uma única responsabilidade. E cada livro já tem um ponto de entrada para o seu minijogo, sem precisar reescrever a base.

## 14. Processo  ·  4:19

No desenvolvimento, usamos o GitHub, com branches e revisão de código (Pull Requests) antes de juntar cada mudança à versão principal. Também usamos inteligência artificial como assistente de programação, sempre com as mudanças revisadas pela equipe.

## 15. Status e próximos passos  ·  4:35

Até aqui, entregamos o cenário completo, as interações com os livros, o modo de teste no computador e a configuração automática. Os próximos passos são cadastrar os livros reais da exposição, testar e otimizar no Meta Quest e, por fim, criar os minijogos.

## 16. Encerramento  ·  4:52

O Sebraelab VR transforma uma visita em uma experiência de descoberta. Obrigado pela atenção!

## Fontes do vídeo

A pasta [`fonte/`](fonte/) tem o roteiro (`roteiro.json`), as páginas que geram cada cena (`jogo.html` com o modelo 3D em three.js, `slides2.html` com os slides e as telas da Unity) e os scripts de renderização (Playwright + ffmpeg). O desenho do azulejo (`azulejo.js`) é a mesma fórmula do `SebraelabCulturaSaoLuis.cs`.
