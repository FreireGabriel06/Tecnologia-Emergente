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
     com XR Origin, EventSystem de VR, jogador de teste do PC, chão de teleporte no piso e **3 livros de exemplo em cima
     da "Mesa Grande"** (popup, vídeo e luz de destaque já ligados). O jogador começa de frente para essa mesa.
5. No fim aparece **Pronto!**. Dê **Play** para testar: sem óculos, use WASD + mouse e aperte **E** num livro.
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
5. O jogador começa de frente para a "Mesa Grande". Para começar na entrada, mova o **XR Origin** (e o *Jogador PC*).
6. Se o teleporte não funcionar no piso, ajuste a altura do objeto **`Chao (teleporte)`** para ficar no nível do piso.
7. Os **Livros de exemplo** podem ser movidos para outras mesas ou apagados depois de configurar os livros de verdade.

## 3. Livros

Os 3 livros do estande de exemplo já funcionam. Para os livros de verdade:

1. Coloque o modelo do livro em cima da mesa/estande e selecione-o na Hierarchy.
2. Use **Sebraelab > Livros > Configurar livro selecionado**. Isso faz automaticamente os **PASSOS 4 e 5** do guia:
   - **Box Collider** ajustado ao tamanho do livro;
   - **Rigidbody** com gravidade (o livro fica apoiado na mesa e cai se for solto);
   - **XR Grab Interactable** (pegar com o Grip);
   - **LivroPopup** e um filho **Popup**: Canvas em World Space com escala 0.001, painel, título, autor, capa,
     resumo, vídeo e um botão *Iniciar Desafio* (desativado, para os minigames no futuro).
3. Conteúdo do popup: na pasta `Assets/Livros`, botão direito > **Create > Sebraelab > Dados do Livro**.
   Preencha título, autor, resumo, capa e vídeo e arraste o asset para o campo **Dados** do `LivroPopup`.
   Os livros de exemplo usam `Assets/Livros/Livro de exemplo 1/2/3`: é só editar esses arquivos.
   Campos vazios (autor, capa, vídeo) somem do popup automaticamente.
4. No `LivroPopup`, **Quando Mostrar** escolhe se o popup abre ao aproximar a mão, ao segurar, ou nos dois casos.
5. Depois de configurar um livro, arraste-o para `Assets/Livros` para virar *prefab*. Os outros livros podem ser cópias
   desse prefab, trocando só o modelo e o campo **Dados**.

> A mesa/estande precisa ter collider (passo 2), senão o livro cai através dela ao dar Play.

## 4. GIFs (vídeos)

O Unity não toca `.gif`. Converta para `.mp4`:

```bash
ffmpeg -i animacao.gif -movflags faststart -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" animacao.mp4
```

(ou use qualquer conversor online de GIF para MP4). Coloque o `.mp4` em `Assets/Videos` e arraste para o campo
**Video** do asset *Dados do Livro*. O vídeo toca em loop quando o popup abre e para quando fecha.
Se o vídeo não tocar no Quest, marque **Transcode** nas configurações de import do vídeo.

## 5. Estandes (área de destaque)

O estande de exemplo já tem uma **Zona de destaque** e uma **Luz de destaque**. Para outro estande:

1. Crie um objeto vazio na frente do estande, por exemplo `Zona Estande 2`.
2. **Add Component > Estande Zona Destaque**. Ele cria um **Box Collider** como *trigger*. Ajuste o tamanho para cobrir
   a área onde o jogador fica, com uns 2 m de altura, porque o script usa a posição da cabeça.
3. Crie uma **Spot Light** apontando para o estande e arraste-a para **Objetos Destaque**: ela começa apagada e acende
   quando o jogador chega.
4. Opcional: um **Audio Source** (com *Play On Awake* desligado) em **Som Ao Entrar**.

## 6. Testar sem óculos

- Dê Play na cena: **WASD** anda, **mouse** olha, **Shift** corre, **E** ou **clique** no livro abre o popup,
  **Esc** solta o mouse.
- Se um headset estiver conectado (Quest Link / Air Link), a mesma cena usa o XR Origin e o jogador de PC se desliga sozinho.
- Para testar o **pegar** sem óculos, use o **XR Device Simulator** (Package Manager > XR Interaction Toolkit > Samples).
  Nesse caso desmarque **Trocar Automaticamente** no `DesktopPlayerController` ou desative o *Jogador PC*.

## 7. Gerar o app para o Meta Quest

1. No Quest: ative o **modo desenvolvedor** (app Meta Horizon no celular).
2. **File > Build Profiles** > **Android** > **Switch Platform**.
3. Conecte o Quest no USB, escolha-o em **Run Device** e clique em **Build And Run**.
4. Se o Unity mostrar avisos de validação, use **Edit > Project Settings > XR Plug-in Management > Project Validation > Fix All**.

## 8. Minigames (fase futura)

Cada popup já tem um botão **BotaoMinigame** desativado, ligado em `LivroPopup.IniciarMinigame()`. Quando for a hora:
ative o botão e, no `LivroPopup`, adicione no evento **Ao Iniciar Minigame** o que deve acontecer (carregar uma cena,
ativar um objeto etc.). A cena já tem o EventSystem de VR que o botão precisa.

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
| Texto do popup não aparece / quadrados | **Window > TextMeshPro > Import TMP Essential Resources**. |
| Materiais rosa | Converta para URP (passo 2.4). |
| Livro cai através da mesa/chão | O cenário está sem colliders (passo 2.2). |
| Não consigo pegar o livro | Confira se o livro tem Collider e **XR Grab Interactable** (use o menu do passo 3). |
| Popup não abre | Confira o campo **Canvas Popup** do `LivroPopup` e o **Quando Mostrar**. |
| Teleporte não funciona | O chão precisa de **Teleportation Area** com a camada **Teleport** (passo 2.6). |
| O modelo aparece "quebrado" depois do clone | Falta o Git LFS: rode `git lfs install` e depois `git lfs pull`. |
