# Sebraelab VR: Exposição de Livros

Trabalho de Tecnologia Emergente em VR. É uma experiência em realidade virtual (Unity + XR Interaction Toolkit)
que recria o Sebraelab como uma exposição interativa de livros. O jogador anda pelo cenário, pega os livros
com a mão e vê um popup com título, autor, resumo, capa e uma animação (GIF convertido para vídeo).

| Documento | Para que serve |
| --- | --- |
| [`Docs/CONFIGURACAO_UNITY.md`](Docs/CONFIGURACAO_UNITY.md) | Passo a passo no Unity: abrir e configurar o projeto, cenário, livros, vídeos, build para o Quest |
| [`Docs/Apresentacao/`](Docs/Apresentacao/) | Vídeo de apresentação do projeto (narrado, ~4 min) e o roteiro de cada cena |
| [`Docs/GDD_Sebraelab_VR.docx`](Docs/GDD_Sebraelab_VR.docx) | Game Design Document |
| [`Docs/GUIDE_JOGO_SEBRAELAB.txt`](Docs/GUIDE_JOGO_SEBRAELAB.txt) | Guia original de implementação manual |

## Estado do projeto

- [x] Repositório é um projeto Unity 6 com XR Interaction Toolkit, OpenXR e Input System (`Packages/manifest.json`)
- [x] Scripts: popup dos livros, dados dos livros, destaque dos estandes, jogador de teste no PC e menu de automação no editor
- [x] Configurador automático: URP, OpenXR/Quest, XR Origin e a cena `Sebraelab.unity` com um estande de exemplo
- [ ] **Fase 1**: abrir o projeto e rodar o configurador (uma pessoa) e colocar o modelo do Sebraelab em `Assets/Models`
- [ ] **Fase 2**: posicionar e configurar os livros de verdade
- [ ] **Fase 3**: textos, capas e vídeos de cada livro
- [ ] **Fase 4**: polimento, iluminação e performance no Quest
- [ ] **Fase 5**: minigames (pós-MVP; o gancho `IniciarMinigame()` já existe)

## Estrutura

```
Assets/
  Scripts/
    Livros/      LivroPopup.cs, LivroData.cs
    UI/          PopupOlharParaJogador.cs
    Exposicao/   EstandeZonaDestaque.cs
    Player/      DesktopPlayerController.cs, DesktopInteracao.cs, EntradaDesktop.cs
    Editor/      SebraelabMenu.cs, SebraelabConfigurador.cs  (menu "Sebraelab" no Unity)
    Compat/      XRICompat.cs      (deixa os scripts compatíveis com XRI 2.x e 3.x)
  Models/        modelo 3D do Sebraelab (.fbx / .obj / .glb)
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
| `LivroPopup` | Vai no livro. Abre o popup quando a mão se aproxima (Hover) e/ou pega o livro (Select) e fecha ao soltar. Toca o vídeo em loop e tem o gancho `IniciarMinigame()`. |
| `LivroData` | Asset com título, autor, resumo, capa e vídeo de um livro (*Assets > Create > Sebraelab > Dados do Livro*). |
| `PopupOlharParaJogador` | Mantém o popup em pé acima do livro e virado para o jogador, mesmo com o livro girando na mão. |
| `EstandeZonaDestaque` | Área do estande: liga luzes/objetos de destaque e toca um som quando o jogador chega perto. |
| `DesktopPlayerController` | Jogador para testar no PC sem óculos (WASD + mouse). Sem headset, ele substitui o XR Origin; com headset, se desliga sozinho. |
| `DesktopInteracao` | No teste pelo PC, mira num livro e aperta **E** ou clica para abrir o popup. |
| `SebraelabConfigurador` (Editor) | **Sebraelab > Configurar projeto VR (automático)**: instala o URP, importa o XR Origin, ativa o OpenXR para PC e Quest e cria a cena com um estande de exemplo. Pergunta sozinho na primeira vez que o projeto é aberto. |
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
5. Abra `Assets/Scenes/Sebraelab.unity` e dê **Play** (sem óculos: WASD + mouse, **E** abre o popup do livro).

## Regras do Git no grupo

- `git pull` **antes** de começar a mexer.
- Uma branch por tarefa (`git checkout -b feature/livros`) e Pull Request para a `main`.
- **Duas pessoas não editam a mesma cena (`.unity`) ao mesmo tempo**, porque conflito em cena é muito difícil de resolver.
  Combinem no grupo quem está com a cena, e transformem livros/estandes em *prefabs*.
- Sempre commitem os arquivos `.meta` junto com o asset. Nunca commitem `Library/` (o `.gitignore` já impede).
- Mudanças feitas por IA (Claude) ficam numa branch separada com Pull Request: nada entra na `main` sem alguém
  do grupo revisar (como pede a seção 6 do GDD).
