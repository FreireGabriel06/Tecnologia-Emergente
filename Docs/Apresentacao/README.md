# Roteiro da apresentação (vídeo)

Vídeo: [`Sebraelab_VR_Apresentacao.mp4`](Sebraelab_VR_Apresentacao.mp4) (1080p, ~4min20s, narrado em português, com legendas).

Se precisarem apresentar ao vivo, este é o texto de cada cena, na ordem do vídeo.

## 1. Abertura  ·  0:00

Olá! Este é o Sebraelab VR: uma exposição interativa de livros em realidade virtual. O projeto foi desenvolvido para a disciplina de Tecnologias Emergentes.

## 2. A ideia  ·  0:12

A ideia é recriar o Sebraelab em 3D e transformá-lo numa feira de livros virtual. O visitante coloca os óculos, anda livremente pelo espaço e pega os livros com as próprias mãos. Na hora, ele vê um resumo, a capa e uma animação sobre cada obra.

## 3. Pilares do design  ·  0:30

O projeto segue quatro pilares, definidos no nosso documento de design (GDD). Fidelidade do cenário, para o visitante se sentir dentro do Sebraelab. Interatividade natural: pegar e girar os livros como no mundo real. Multimídia dinâmica, com textos e animações. E uma arquitetura modular, pronta para receber minijogos em cada livro.

## 4. Tecnologias  ·  0:52

Usamos a Unity 6, com o Universal Render Pipeline (URP), otimizado para óculos autônomos. A interação em realidade virtual vem do XR Interaction Toolkit da Unity, e o OpenXR conecta o projeto ao Meta Quest e a outros óculos. A lógica foi programada em C#, e todo o código fica versionado no GitHub.

## 5. Como funciona a experiência  ·  1:15

Veja como funciona a experiência. O visitante começa na entrada do Sebraelab. Com o analógico do controle, ele anda ou se teletransporta pelo espaço. Ao chegar perto de um estande, uma luz de destaque se acende. Quando ele aproxima a mão de um livro, aparece um painel flutuante com as informações. Ao pegar o livro, o painel continua aberto até ele soltar.

## 6. Visualização 3D do estande (ilustrativa)  ·  1:40

Visualização ilustrativa do estande de exemplo que o projeto monta automaticamente: uma mesa, três livros e a luz de destaque. O painel aparece acima do livro, sempre em pé e virado para o visitante, mesmo quando ele levanta e gira o livro na mão.

## 7. O painel do livro  ·  1:58

No painel aparecem o título, o autor, a capa e o resumo do livro. Como a Unity não reproduz GIF, as animações são convertidas para vídeo MP4 e tocam em loop. E cada painel já tem o botão "Iniciar Desafio", reservado para os minijogos.

## 8. Arquitetura dos scripts  ·  2:16

No código, cada responsabilidade fica num script separado. O LivroPopup controla quando o painel abre e fecha. O LivroData guarda os dados de cada livro num arquivo próprio, fácil de editar. O PopupOlharParaJogador mantém o painel virado para o jogador, e a EstandeZonaDestaque acende as luzes dos estandes. Assim, novos livros e novos minijogos entram sem reescrever a base do projeto.

## 9. Automação na Unity  ·  2:42

Para acelerar o trabalho da equipe, criamos um menu Sebraelab dentro da Unity. Com um clique, o configurador automático instala o URP, prepara o jogador de realidade virtual e ativa o Meta Quest, e ainda monta a cena com um estande de exemplo. Outro clique transforma qualquer modelo de livro em um livro interativo.

## 10. Testar sem óculos  ·  3:05

Nem todo mundo da equipe tem óculos de realidade virtual em casa. Por isso, criamos um modo de teste no computador: W, A, S e D andam, o mouse olha e a tecla E abre o painel do livro. Quando um óculos está conectado, a mesma cena troca sozinha para o modo de realidade virtual.

## 11. Como trabalhamos  ·  3:25

Organizamos o desenvolvimento no GitHub, com uma branch para cada tarefa e revisão (Pull Request) antes de juntar tudo na versão principal. Modelos e vídeos pesados vão pelo Git LFS. Também usamos inteligência artificial como assistente de programação, sempre com as mudanças revisadas pela equipe.

## 12. Onde estamos  ·  3:46

Até aqui, concluímos a estrutura do projeto, os scripts de interação e a configuração automática. Os próximos passos são importar o modelo 3D do Sebraelab e cadastrar os livros da exposição, com seus resumos e animações. Depois, vamos testar e otimizar a experiência no Meta Quest. Por fim, vêm os minijogos de cada livro.

## 13. Encerramento  ·  4:08

O Sebraelab VR mostra como a realidade virtual pode aproximar as pessoas dos livros e do ambiente de inovação do Sebrae. Obrigado pela atenção!

---

## Como o vídeo foi feito

- Slides em HTML (`fonte/slides.html`) e cena 3D ilustrativa em three.js (`fonte/cena3d.html`), renderizados quadro a quadro com Playwright (`fonte/render.js`).
- Narração gerada com a voz neural Kokoro (pt-BR) a partir de `fonte/roteiro.json` (`fonte/narrar.py`, `fonte/linha_do_tempo.py`).
- Montagem com ffmpeg (`fonte/montar.py`).
- A cena 3D é uma **ilustração** do estande de exemplo que o configurador cria; não é uma captura do jogo rodando na Unity.
- Para mudar um texto: edite `fonte/roteiro.json` (`fala` = o que a voz diz, com a pronúncia ajustada; `legenda` = o texto na tela) e os slides em `fonte/slides.html`, e gere de novo.
