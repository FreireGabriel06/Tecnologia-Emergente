# Sebraelab VR: Exposição de Livros

Trabalho de Tecnologia Emergente em VR. É uma experiência em realidade virtual (Unity + XR Interaction Toolkit)
que recria o Sebraelab como uma exposição interativa de livros. **Os livros estão escondidos pelo Sebraelab** e o
jogador precisa encontrá-los (coleta no estilo Slender: "1 de 3", "2 de 3"...). Pegar o livro de qualquer jeito conta:
com a mão, com o raio do controle ou com a tecla E no PC. Cada livro encontrado vai para o **hub**, onde um menu mostra
a introdução da história, o vídeo e o botão **Desafios** (preparado para os minijogos).

| Documento | Para que serve |
| --- | --- |
| [`Docs/CONFIGURACAO_UNITY.md`](Docs/CONFIGURACAO_UNITY.md) | Passo a passo no Unity: abrir e configurar o projeto, cenário, livros, vídeos, build para o Quest |
| [`Docs/Apresentacao/`](Docs/Apresentacao/) | Vídeo de apresentação, slides em PDF (`Sebraelab_VR_Slides.pdf`) e o roteiro de cada cena |
| [`Docs/GDD_Sebraelab_VR.docx`](Docs/GDD_Sebraelab_VR.docx) | Game Design Document |
| [`Docs/GUIDE_JOGO_SEBRAELAB.txt`](Docs/GUIDE_JOGO_SEBRAELAB.txt) | Guia original de implementação manual |

## Estado do projeto

- [x] Repositório é um projeto Unity 6 com XR Interaction Toolkit, OpenXR e Input System (`Packages/manifest.json`)
- [x] Scripts: coleta dos livros, hub com menu, dados dos livros, destaque dos estandes, jogador de teste no PC e menu de automação no editor
- [x] Configurador automático: URP, OpenXR/Quest, XR Origin e a cena `Sebraelab.unity` já com o hub e os livros escondidos
- [x] Modelo 3D do Sebraelab no projeto (`Assets/Sebraelab3D`): o configurador monta a cena dentro dele
- [x] Cultura de São Luís: Estante maranhense (Aluísio Azevedo, Josué Montello, Ferreira Gullar) e painel de azulejos portugueses
- [x] Coleta de livros: 3 livros escondidos em esconderijos sorteados, hub central com menu (introdução da história) e botão Desafios preparado
- [ ] **Fase 1**: abrir o projeto no Unity 6 e rodar o configurador (uma pessoa só) e conferir o cenário
- [ ] **Fase 2**: posicionar e configurar os livros de verdade
- [ ] **Fase 3**: textos, capas e vídeos de cada livro
- [ ] **Fase 4**: polimento, iluminação e performance no Quest
- [ ] **Fase 5**: desafios/minigames (pós-MVP; o botão **Desafios** e o evento `Ao Abrir Desafios` já existem)

## Estrutura

```
Assets/
  Scripts/
    Coleta/      LivroColetavel.cs, GerenciadorColeta.cs, PontoDeLivro.cs, AvisoColeta.cs, SonsGerados.cs
    Livros/      LivroData.cs, LivroPopup.cs (popup do modelo antigo)
    UI/          HubLivrosMenu.cs, PopupOlharParaJogador.cs
    Exposicao/   EstandeZonaDestaque.cs
    Player/      DesktopPlayerController.cs, DesktopInteracao.cs, EntradaDesktop.cs
    Editor/      SebraelabMenu.cs, SebraelabConfigurador.cs, SebraelabColeta.cs, SebraelabCulturaSaoLuis.cs,
                 SebraelabGravador.cs  (menu "Sebraelab" no Unity)
    Compat/      XRICompat.cs      (deixa os scripts compatíveis com XRI 2.x e 3.x)
  Sebraelab3D/   modelo 3D do Sebraelab ("Sebrae Lab.fbx"), móveis separados e texturas
  CulturaSaoLuis/ vídeo dos azulejos (AzulejosSaoLuis.mp4); a textura e os materiais do azulejo são gerados aqui
  Models/        outros modelos 3D (.fbx / .obj / .glb)
  Materials/     materiais
  Videos/        GIFs convertidos para .mp4
  Livros/        assets "Dados do Livro" e imagens das capas
  Scenes/        Sebraelab.unity           (criada pelo configurador)
  Settings/, XR/, Samples/, TextMesh Pro/  (criadas pelo configurador; também vão para o Git)
Packages/        manifest.json com os pacotes do projeto
ProjectSettings/ configurações do Unity
Docs/            GDD, guia e passo a passo
```

## Scripts

| Script | O que faz |
| --- | --- |
| `LivroColetavel` | Livro escondido: flutua, gira, brilha e faz um som baixinho. Conta como encontrado quando é pego de **qualquer jeito** (qualquer interactor do XR Toolkit: mão ou raio; tecla E no PC; ou por código). |
| `GerenciadorColeta` | **Hub central** da coleta: sorteia os esconderijos, conta "1 de 3", mostra o aviso, toca o som, leva o livro para o lugar dele no hub e atualiza o menu. Eventos *Ao Encontrar Livro* e *Ao Encontrar Todos*. |
| `PontoDeLivro` | Esconderijo possível para um livro (*Esconderijo de Livro*). |
| `AvisoColeta` | Aviso na frente do jogador, estilo Slender ("2 de 3 livros"), que funciona em VR e no PC e aparece por cima das paredes. |
| `HubLivrosMenu` | Menu do hub: lista dos livros (??? até ser encontrado), introdução da história, capa, vídeo e o botão **Desafios** (evento *Ao Abrir Desafios*, ainda sem função). Funciona com o raio do controle e com a mira do PC. |
| `SonsGerados` | Sons gerados por código (brilho do livro, livro encontrado, todos encontrados), para não depender de arquivos de áudio. |
| `LivroPopup` | (Modelo antigo, sem coleta) Vai no livro. Abre o popup quando a mão se aproxima (Hover) e/ou pega o livro (Select) e fecha ao soltar. Toca o vídeo em loop e tem o gancho `IniciarMinigame()`. |
| `LivroData` | Asset com título, autor, resumo, capa e vídeo de um livro (*Assets > Create > Sebraelab > Dados do Livro*). |
| `PopupOlharParaJogador` | Mantém o popup em pé acima do livro e virado para o jogador, mesmo com o livro girando na mão. |
| `EstandeZonaDestaque` | Área do estande: liga luzes/objetos de destaque e toca um som quando o jogador chega perto. |
| `DesktopPlayerController` | Jogador para testar no PC sem óculos (WASD + mouse). Sem headset, ele substitui o XR Origin; com headset, se desliga sozinho. |
| `DesktopInteracao` | No teste pelo PC, mira com o centro da tela e aperta **E** ou clica: pega o livro escondido e aperta os botões do menu do hub. |
| `DemonstracaoGuiada` | Passeio automático pela cena (acha e pega os 3 livros, volta ao hub e abre a introdução de cada um), usado para gravar a demonstração. |
| `SebraelabGravador` (Editor) | **Sebraelab > Gravar vídeo de demonstração**: dá Play, faz o passeio e grava um MP4 com o Unity Recorder em `Gravacoes/`. |
| `SebraelabConfigurador` (Editor) | **Sebraelab > Configurar projeto VR (automático)**: instala o URP, importa o XR Origin, ativa o OpenXR para PC e Quest e cria a cena com o hub dos livros e os 3 livros escondidos. Pergunta sozinho na primeira vez que o projeto é aberto. |
| `SebraelabColeta` (Editor) | Monta a coleta: hub na Mesa Grande (lugares dos livros, luz, menu), esconderijos em móveis do Sebraelab (procura superfícies planas com raycast) e os 3 livros escondidos. Menus **Sebraelab > Coleta de livros** e **Sebraelab > Livros > Transformar em livro escondido (coleta)**. |
| `SebraelabCulturaSaoLuis` (Editor) | Cultura de São Luís do Maranhão: dados dos 3 livros da **Estante maranhense** (obras de autores nascidos em São Luís), textura de azulejo português gerada por código e o painel de azulejos com a placa no hub (na parede, se houver uma logo atrás; senão num totem). |
| `SebraelabMenu` (Editor) | Menu **Sebraelab** no Unity: configura um livro inteiro com um clique, coloca o modelo do Sebraelab na cena com colliders e chão de teleporte, e cria o jogador de teste. |

### E a movimentação do personagem em VR?

Em VR o movimento (analógico, teleporte, giro e pegar objetos) **não precisa de script próprio**: ele já vem
pronto no prefab **XR Origin (XR Rig)** dos *Starter Assets* do XR Interaction Toolkit, que o configurador
coloca na cena: analógico esquerdo anda, analógico direito gira e (para frente) teleporta, Grip pega.
Os scripts da pasta `Player/` servem para testar o cenário no PC, sem óculos.

## Começando

1. Instale o **Unity 6 (6000.0 LTS)** pelo Unity Hub, com **Android Build Support**. Todo mundo com a mesma versão.
2. Instale o [Git LFS](https://git-lfs.com) (já vem no Git for Windows e no GitHub Desktop) e rode uma vez:
   `git lfs install`
3. Clone o repositório e abra a pasta pelo Unity Hub (*Add > Add project from disk*).
4. **Primeira vez (uma pessoa só):** quando aparecer a janela *Sebraelab VR*, clique em **Configurar agora** e espere o
   *Pronto!*. Depois faça commit e push de tudo. Detalhes no [passo 1 do guia](Docs/CONFIGURACAO_UNITY.md#1-abrir-e-configurar-o-projeto-uma-pessoa-só-na-primeira-vez).
5. Abra `Assets/Scenes/Sebraelab.unity` e dê **Play** (sem óculos: WASD + mouse; mire num livro escondido e aperte **E** para pegar; no hub, mire nos botões do menu e aperte **E**).

## Regras do Git no grupo

- `git pull` **antes** de começar a mexer.
- Uma branch por tarefa (`git checkout -b feature/livros`) e Pull Request para a `main`.
- **Duas pessoas não editam a mesma cena (`.unity`) ao mesmo tempo**, porque conflito em cena é muito difícil de resolver.
  Combinem no grupo quem está com a cena, e transformem livros/estandes em *prefabs*.
- Sempre commitem os arquivos `.meta` junto com o asset. Nunca commitem `Library/` (o `.gitignore` já impede).
- Mudanças feitas por IA (Claude) ficam numa branch separada com Pull Request: nada entra na `main` sem alguém
  do grupo revisar (como pede a seção 6 do GDD).
