# Passo a passo no Unity: Sebraelab VR

O repositório já é um projeto Unity (Unity 6). Quase tudo é configurado sozinho pelo **configurador automático**
(menu **Sebraelab > Configurar projeto VR (automático)**). Os outros atalhos do menu **Sebraelab** cuidam do
cenário, dos livros e do jogador de teste. Tudo o que os menus fazem na cena pode ser desfeito com **Ctrl+Z**.

---

## 0. Antes de tudo (cada pessoa, uma vez)

1. No **Unity Hub**, instale o **Unity 6 (6000.0 LTS)** com o módulo **Android Build Support**
   (marque também *OpenJDK* e *Android SDK & NDK*). Ele é necessário para gerar o app do Meta Quest.
   **Todo mundo precisa usar a mesma versão do Unity.**
2. Instale o **Git LFS** e rode uma vez: `git lfs install`. Modelos, texturas e vídeos vão para o LFS
   automaticamente (configurado no `.gitattributes`). O Git for Windows e o GitHub Desktop já trazem o LFS.
3. Clone o repositório.

## 1. Abrir e configurar o projeto (uma pessoa só, na primeira vez)

1. **Unity Hub > Add > Add project from disk** e escolha a pasta do repositório.
   Se o Hub disser que a versão do projeto não está instalada, escolha a Unity 6 que você instalou.
2. Abra o projeto. Na primeira vez o Unity baixa os pacotes (XR Interaction Toolkit, OpenXR, Input System),
   e isso demora alguns minutos.
3. Se aparecer a pergunta do **Input System** sobre ativar os *backends*, clique **Yes**. O Unity reinicia.
4. Vai aparecer a janela **Sebraelab VR: Este projeto ainda não foi configurado**. Clique em **Configurar agora**
   e espere. O Unity recompila algumas vezes e o configurador continua sozinho. Ele:
   - instala o **URP** e cria o asset de render (`Assets/Settings/Sebraelab_URP.asset`);
   - importa os **Starter Assets** do XR Interaction Toolkit (o **XR Origin** com andar, girar, teleporte e pegar objetos);
   - importa o **TMP Essential Resources** do TextMeshPro;
   - ativa o **OpenXR** para PC e Android, com os controles do Quest e o *Meta Quest Support*;
   - deixa o build do Quest pronto (IL2CPP, ARM64, Vulkan, ASTC, Linear);
   - cria a cena **`Assets/Scenes/Sebraelab.unity`** **dentro do modelo do Sebraelab** (`Assets/Sebraelab3D/Sebrae Lab.fbx`),
     com XR Origin, EventSystem de VR, jogador de teste do PC, chão de teleporte no piso, o **hub dos livros** na
     "Mesa Grande" (menu na parede de azulejos) e **3 livros escondidos** em móveis espalhados pela sala (ver passo 3).
     O jogador começa no hub, de frente para a mesa.
5. No fim aparece **Pronto!**. Dê **Play** para testar: sem óculos, ande com WASD + mouse, procure os livros e aperte
   **E** mirando num livro para pegá-lo. Depois volte ao hub e use o menu.
6. Faça o commit de tudo (inclusive `Packages/`, `ProjectSettings/` e as pastas novas em `Assets/`) e o push.
   Os outros só precisam dar `git pull` e abrir o projeto. Para eles o configurador não pergunta nada, porque a cena já existe.

> Só **uma** pessoa deve rodar o configurador e commitar. Se duas pessoas rodarem ao mesmo tempo, o Git recebe duas
> versões diferentes dos mesmos arquivos de configuração.
>
> Se a janela não aparecer ou a configuração parar no meio, rode de novo pelo menu
> **Sebraelab > Configurar projeto VR (automático)**. Ele continua de onde parou.

## 2. Cenário do Sebraelab

O modelo da equipe já está no projeto, em **`Assets/Sebraelab3D`**: `Sebrae Lab.fbx` é a sala montada, os arquivos
`SM_*.fbx` são os móveis separados, e há também as texturas de piso, parede e madeira. O configurador já coloca a sala na
cena. Se precisar colocar de novo (ou usar outro modelo), use **Sebraelab > Cenário > Colocar modelo do Sebraelab na cena**. Isso:
   - coloca o modelo na cena e esconde o que não faz parte da sala: as cópias soltas dos móveis (`SM_...`), a câmera e
     o plano gigante com a imagem de referência que vieram do Blender;
   - adiciona **Mesh Collider** em cada parte e marca tudo como *Static*, para ninguém atravessar parede ou chão;
   - coloca um **chão de teleporte** invisível no nível do piso (as partes com material "Chao ...").

Para outro modelo, coloque o arquivo em **`Assets/Models`** antes de usar o menu.
3. Confira a **escala**: em VR, 1 unidade = 1 metro, e uma porta tem uns 2,1 m de altura. Se estiver errado, mude o
   **Scale Factor** na aba **Model** do Inspector do arquivo e clique em **Apply**.
4. **Materiais:** se o modelo veio sem cor, na aba **Materials** do Inspector do arquivo use **Extract Materials...**
   para `Assets/Materials`. Se ficarem rosa: **Window > Rendering > Render Pipeline Converter > Material Upgrade**.
5. O jogador começa no hub, de frente para a "Mesa Grande". Para começar na entrada, mova o **XR Origin** (e o *Jogador PC*).
6. Se o teleporte não funcionar no piso, ajuste a altura do objeto **`Chao (teleporte)`** para ficar no nível do piso.
7. O **Hub dos livros**, os **Esconderijos dos livros** e os **Livros escondidos** podem ser movidos à vontade (passo 3).

## 3. Coleta de livros (o jogo)

Os livros ficam **escondidos pelo Sebraelab** e o jogador precisa encontrá-los, como as páginas do Slender. A princípio
são **3 livros**.

**Como funciona:**

1. O jogador começa no **hub** (a "Mesa Grande", com o menu na parede de azulejos) e vê o aviso *Encontre os 3 livros*.
2. A cada partida os livros aparecem em esconderijos **sorteados** entre os **Esconderijos dos livros**: arquibancada,
   mesa da recepção, bancada da cozinha, mesa perto da TV, estações de trabalho e bancos. Eles flutuam, giram, brilham e
   fazem um som baixinho, para serem achados.
3. **Pegar o livro de qualquer jeito conta**: em VR com a mão (botão **Grip**) ou com o raio do controle; no PC mirando
   e apertando **E** (ou clicando). Aparece o aviso **"1 de 3 livros · Você encontrou: ..."**, toca um som e o livro vai
   para o lugar dele na mesa do hub. Quem cuida disso é um só script, o `GerenciadorColeta` (hub central).
4. No **menu do hub**, os livros ainda não encontrados aparecem como **???**. Escolha um livro encontrado para ler a
   **introdução da história**, com o vídeo do livro. Pegar o livro em cima da mesa do hub também abre a introdução dele.
5. O botão **Desafios** já está no menu, mas **ainda não tem função**: ele mostra *"Os desafios deste livro chegam em
   breve!"* e dispara o evento **Ao Abrir Desafios** do `HubLivrosMenu` (ver passo 8).

**O que está na cena:**

| Objeto | O que é |
| --- | --- |
| `Hub dos livros` | `GerenciadorColeta` (contagem, sorteio, aviso, sons), os 3 **Lugar do livro N** na mesa, **Luz do hub**, **Zona do hub** e o **Menu do hub** (Canvas em World Space) |
| `Painel de azulejos (São Luís)` | Parede de azulejos com a placa *Estante maranhense*. O menu fica em cima dela |
| `Esconderijos dos livros` | Um objeto **Esconderijo de Livro** (`PontoDeLivro`) para cada lugar possível |
| `Livros escondidos` | Os 3 livros: `XR Grab Interactable` + `Rigidbody` parado + `LivroColetavel`, com luz e som |

**Trocar o conteúdo:** os textos ficam nos assets *Dados do Livro* em `Assets/Livros` (`O Mulato`, `Os Tambores de São
Luís`, `Poema Sujo`). O campo **Resumo** é a **introdução da história** que aparece no menu. Capa e vídeo também.
Para um livro novo: **Create > Sebraelab > Dados do Livro** e arraste o asset para o campo **Dados** do `LivroColetavel`.

**Outro modelo de livro:** coloque o modelo na cena, selecione-o e use **Sebraelab > Livros > Transformar em livro
escondido (coleta)**. Ele ganha collider, `Rigidbody` parado, `XR Grab Interactable` e `LivroColetavel`, e entra na
lista do hub. Preencha **Dados** e **Cor** no `LivroColetavel`.

**Esconderijos:** mova os objetos de `Esconderijos dos livros` ou duplique um para criar outro. Também dá para olhar um
móvel na Scene View e usar **Sebraelab > Coleta de livros > Criar esconderijo onde a Scene View está olhando**. Para os
livros ficarem sempre no mesmo lugar, desmarque **Sortear Esconderijos** no `GerenciadorColeta` e posicione os livros.

**Cena criada antes desta versão:** abra-a e use **Sebraelab > Coleta de livros > Montar hub e livros escondidos na cena
aberta**. A estante antiga (livros com popup na mesa) é desligada.

> O sistema antigo, de livro com popup que abre ao aproximar a mão, continua disponível em **Sebraelab > Livros >
> Configurar livro com popup (sem coleta)**.

### 3.1 Cultura de São Luís do Maranhão

- **Livros:** os 3 livros escondidos são *O Mulato* (Aluísio Azevedo, 1881), *Os Tambores de São Luís* (Josué Montello,
  1975) e *Poema Sujo* (Ferreira Gullar, 1976), três autores nascidos em São Luís: a *Estante maranhense*. A capa fica
  vazia (coloque a imagem no campo **Capa**, se tiverem uma com direito de uso).
- **Painel de azulejos:** padrão de azulejo português, como os dos casarões do Centro Histórico (Patrimônio Mundial da
  UNESCO). A textura é **gerada pelo próprio script** (`Assets/CulturaSaoLuis/Azulejo_SaoLuis.png`), sem imagem externa.
  O painel é fixado na parede logo atrás da mesa do hub, quando ela existe; senão fica num totem de madeira. O menu do hub
  fica em cima dele, com uma faixa de azulejos no topo.
- **Vídeo:** `AzulejosSaoLuis.mp4` (6 s, em loop) é o vídeo dos três livros no menu.

## 4. GIFs (vídeos)

O Unity não toca `.gif`. Converta para `.mp4`:

```bash
ffmpeg -i animacao.gif -movflags faststart -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" animacao.mp4
```

(ou use qualquer conversor online de GIF para MP4). Coloque o `.mp4` em `Assets/Videos` e arraste para o campo
**Video** do asset *Dados do Livro*. O vídeo toca em loop no menu do hub quando o livro é escolhido.
Se o vídeo não tocar no Quest, marque **Transcode** nas configurações de import do vídeo.

## 5. Estandes (área de destaque)

O hub já tem uma **Zona do hub** e a **Luz do hub**. Para destacar outro lugar:

1. Crie um objeto vazio na frente do estande, por exemplo `Zona Estande 2`.
2. **Add Component > Estande Zona Destaque**. Ele cria um **Box Collider** como *trigger*. Ajuste o tamanho para cobrir
   a área onde o jogador fica, com uns 2 m de altura, porque o script usa a posição da cabeça.
3. Crie uma **Spot Light** apontando para o estande e arraste-a para **Objetos Destaque**: ela começa apagada e acende
   quando o jogador chega.
4. Opcional: um **Audio Source** (com *Play On Awake* desligado) em **Som Ao Entrar**.

## 6. Testar sem óculos

- Dê Play na cena: **WASD** anda, **mouse** olha, **Shift** corre, **Esc** solta o mouse. Mirando com o ponto no centro
  da tela, **E** ou **clique** pega o livro escondido e aperta os botões do menu do hub (a dica aparece embaixo da mira).
- Se um headset estiver conectado (Quest Link / Air Link), a mesma cena usa o XR Origin e o jogador de PC se desliga sozinho.
- Para testar o **pegar** sem óculos, use o **XR Device Simulator** (Package Manager > XR Interaction Toolkit > Samples).
  Nesse caso desmarque **Trocar Automaticamente** no `DesktopPlayerController` ou desative o *Jogador PC*.

## 6.1 Gravar um vídeo real do jogo (1 clique)

Com o projeto configurado, use **Sebraelab > Gravar vídeo de demonstração**. A Unity abre a cena, entra no Play e faz
um passeio automático: olha o hub, vai atrás de cada livro escondido e pega, volta ao hub, abre a introdução de cada
livro no menu e aperta Desafios. Tudo é gravado em MP4 (1920x1080) pelo
**Unity Recorder**. No fim ela sai do Play sozinha e abre a pasta **`Gravacoes/`** do projeto com o arquivo
`Sebraelab_demo.mp4`. Essa pasta fica fora de `Assets` e não vai para o Git.

O passeio é feito pelo script `DemonstracaoGuiada` no jogador de PC e funciona mesmo sem óculos.

## 7. Gerar o app para o Meta Quest

1. No Quest: ative o **modo desenvolvedor** (app Meta Horizon no celular).
2. **File > Build Profiles** > **Android** > **Switch Platform**.
3. Conecte o Quest no USB, escolha-o em **Run Device** e clique em **Build And Run**.
4. Se o Unity mostrar avisos de validação, use **Edit > Project Settings > XR Plug-in Management > Project Validation > Fix All**.

## 8. Desafios / minigames (fase futura)

O botão **Desafios** do menu do hub já existe e funciona com o raio do controle e com a mira do PC. Por enquanto ele só
mostra *"em breve"*. Quando for a hora: selecione **Hub dos livros > Menu do hub** e, no `HubLivrosMenu`, adicione no
evento **Ao Abrir Desafios** o que deve acontecer (carregar uma cena, ativar um objeto etc.). O evento recebe os
**Dados do Livro** escolhido, então dá para abrir o desafio certo de cada livro. A mensagem fica no campo
**Mensagem Desafios**.

---

## Configuração manual (só se o configurador falhar)

1. **Package Manager > Unity Registry**: instale **Universal RP**. Em **XR Interaction Toolkit > Samples**, importe **Starter Assets**.
2. **Assets > Create > Rendering > URP Asset (with Universal Renderer)** e arraste o asset para
   **Edit > Project Settings > Graphics > Default Render Pipeline**.
3. **Edit > Project Settings > XR Plug-in Management**: marque **OpenXR** nas abas PC e Android. Em **OpenXR**, adicione o
   **Oculus Touch Controller Profile** e, na aba Android, ative **Meta Quest Support**. Em **Project Validation**, use **Fix All**.
4. **Window > TextMeshPro > Import TMP Essential Resources**.
5. Crie uma cena, apague a **Main Camera** e arraste o prefab **XR Origin (XR Rig)** de
   `Assets/Samples/XR Interaction Toolkit/<versão>/Starter Assets/Prefabs/`. Adicione **GameObject > XR > UI Event System**.
6. Coloque um chão com **Teleportation Area** (Interaction Layer Mask = **Teleport**) e siga os passos 2 a 5 acima.

## Problemas comuns

| Problema | Solução |
| --- | --- |
| Erros de compilação / *Safe Mode* ao abrir | Os pacotes não baixaram (sem internet?) ou a versão do Unity não é a 6. Confira em **Window > Package Manager**. |
| Não aparece o menu **Sebraelab** | Há erro de compilação no Console. Resolva primeiro (geralmente é o item acima). |
| A janela do configurador não apareceu | Use **Sebraelab > Configurar projeto VR (automático)**. |
| Controles VR não respondem | **Edit > Project Settings > Player > Active Input Handling** = *Both* (ou *Input System Package*) e reinicie. |
| Texto do menu ou do aviso não aparece / quadrados | **Window > TextMeshPro > Import TMP Essential Resources**. |
| Materiais rosa | Converta para URP (passo 2.4). |
| Livro cai através da mesa/chão | O cenário está sem colliders (passo 2.2). |
| Não consigo pegar o livro | Confira se o livro tem Collider, **XR Grab Interactable** e **Livro Coletavel** (menu do passo 3). |
| Pego o livro e nada acontece | A cena precisa do `GerenciadorColeta` (objeto **Hub dos livros**). Use o menu da coleta (passo 3). |
| Livros todos no mesmo lugar / não saem do lugar | Há menos esconderijos que livros: crie mais (passo 3). |
| O raio não clica no menu do hub | O Canvas precisa do **Tracked Device Graphic Raycaster** e a cena de um **EventSystem** com **XR UI Input Module**. |
| Teleporte não funciona | O chão precisa de **Teleportation Area** com a camada **Teleport** (passo 2.6). |
| O modelo aparece "quebrado" depois do clone | Falta o Git LFS: rode `git lfs install` e depois `git lfs pull`. |
