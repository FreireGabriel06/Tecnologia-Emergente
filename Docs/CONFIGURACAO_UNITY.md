# Passo a passo no Unity: Sebraelab VR

Este guia segue o GDD e o `GUIDE_JOGO_SEBRAELAB.txt`. Onde dá, os passos repetitivos foram trocados pelos
atalhos do menu **Sebraelab** (scripts em `Assets/Scripts/Editor`). Tudo o que o menu faz pode ser desfeito com **Ctrl+Z**.

Os scripts funcionam tanto no **XR Interaction Toolkit 2.x** quanto no **3.x**. Quando o nome de algo muda
entre as versões, os dois nomes aparecem abaixo.

---

## 0. Antes de tudo

- **Versão do Unity:** Unity 6 LTS (ou 2022.3 LTS, como no GDD). O mais importante é que **todo mundo use exatamente a
  mesma versão**. Depois que o projeto for criado, ela fica registrada em `ProjectSettings/ProjectVersion.txt`.
- No Unity Hub, instale a versão com o módulo **Android Build Support** (com *OpenJDK* e *Android SDK & NDK*).
  Ele é necessário para gerar o app do Meta Quest.
- Instale o **Git LFS** e rode `git lfs install` uma vez. Modelos, texturas e vídeos vão para o LFS automaticamente
  (configurado no `.gitattributes`).

## 1. Criar o projeto Unity (uma pessoa só)

O repositório já tem a pasta `Assets/Scripts`, mas ainda não tem o projeto Unity em si (`Packages/` e `ProjectSettings/`).
Os scripts usam o XR Interaction Toolkit, então o projeto precisa ser criado **com os pacotes já instalados** antes de
abrir a pasta do repositório. Se não, o Unity mostra erros de compilação ou abre em *Safe Mode*.

1. **Unity Hub > New project**, numa pasta **fora** do repositório (ex.: `Documentos/SebraelabVR_temp`).
   - Se aparecer o template **VR**, use ele: já vem com URP, XR Interaction Toolkit, OpenXR e um XR Origin pronto.
   - Se não, use **Universal 3D** (ou **3D (URP)**).
2. Se usou Universal 3D, abra **Window > Package Manager > Unity Registry** e instale:
   - **XR Interaction Toolkit**. Quando ele perguntar se quer ativar o novo Input System, clique **Yes** (o Unity reinicia).
   - **OpenXR Plugin** (instala junto o *XR Plugin Management*).
3. Ainda no Package Manager, em **XR Interaction Toolkit > Samples**, importe **Starter Assets**.
   O **XR Device Simulator** também é útil para testar sem óculos.
4. **Edit > Project Settings > XR Plug-in Management**:
   - aba do PC (monitor): marque **OpenXR**;
   - aba Android: marque **OpenXR** e o grupo de recursos **Meta Quest** (*Meta Quest Support*);
   - em **OpenXR > Enabled Interaction Profiles**, adicione **Oculus Touch Controller Profile**
     (e *Meta Quest Touch Plus/Pro* se tiver);
   - em **Project Validation**, clique em **Fix All**.
5. Feche o Unity. Copie as pastas **`Assets`**, **`Packages`** e **`ProjectSettings`** do projeto novo para a pasta do
   repositório (juntando com a `Assets` que já existe). **Não** copie `Library`, `Logs`, `Temp` nem `UserSettings`.
6. No Unity Hub: **Add > Add project from disk** e escolha a pasta do repositório. Abra o projeto.
   Se aparecer o menu **Sebraelab** na barra de cima, deu tudo certo.
7. Commit:
   ```bash
   git checkout -b feature/projeto-unity
   git add .
   git commit -m "Cria projeto Unity com XR Interaction Toolkit e OpenXR"
   git push -u origin feature/projeto-unity
   ```
   Abram um Pull Request para a `main`. Os outros só precisam clonar/dar `git pull` e abrir pelo Unity Hub.

## 2. Cena e jogador VR (XR Origin)

Este passo substitui o **PASSO 3** do guia. O XR Origin dos *Starter Assets* já vem com toda a locomoção configurada,
então não é preciso adicionar `Locomotion System` / `Continuous Move Provider` à mão (no XRI 3.x o Locomotion System
virou *Locomotion Mediator*).

1. Crie a cena `Assets/Scenes/Sebraelab.unity` (ou use a `SampleScene` do template VR).
2. Apague a **Main Camera**.
3. Arraste para a cena o prefab **XR Origin (XR Rig)** de
   `Assets/Samples/XR Interaction Toolkit/<versão>/Starter Assets/Prefabs/`
   (em algumas versões 2.x ele se chama **Complete XR Origin Set Up**). Ele já tem:
   - **analógico esquerdo:** andar (Continuous Move);
   - **analógico direito:** girar (Snap Turn) e, empurrando para frente, teleporte;
   - **mãos:** pegar de perto (Direct Interactor) e de longe (Ray Interactor) com o botão **Grip**.
4. **GameObject > XR > UI Event System**: necessário para clicar em botões de UI com o controle (ex.: botão do minigame).
5. Vinheta de conforto (GDD 4.1): procure **Tunneling Vignette** nos samples do XRI e coloque como filho da câmera do
   XR Origin (em algumas versões ela já vem no rig).

## 3. Cenário do Sebraelab

1. Arraste o arquivo `.fbx`/`.obj` para `Assets/Models`.
2. **Colliders** (para não atravessar paredes nem o chão). Escolha um:
   - selecione o modelo na aba Project, aba **Model** do Inspector, marque **Generate Colliders** e clique em **Apply**; ou
   - arraste o modelo para a cena, selecione-o e use **Sebraelab > Cenário > Adicionar colliders no cenário selecionado**.

   > O guia manda adicionar um Mesh Collider no objeto do cenário, mas no objeto raiz de um FBX isso não funciona,
   > porque as malhas ficam nos objetos filhos. Os dois caminhos acima colocam o collider em cada parte.
3. **Materiais:** aba **Materials** do Inspector do modelo > **Extract Materials...** > `Assets/Materials`.
   Se ficarem rosa: **Window > Rendering > Render Pipeline Converter > Material Upgrade** (converte para URP).
4. **Escala:** em VR, 1 unidade = 1 metro. Confira se uma porta tem uns 2,1 m de altura. Se não tiver, ajuste o
   **Scale Factor** na aba Model do import.
5. Posicione o **XR Origin** no chão, na entrada do Sebraelab. É ali que o jogador começa.
6. **Teleporte:** selecione o chão > **Add Component > Teleportation Area** > em **Interaction Layer Mask**, marque
   **Teleport** (o raio de teleporte dos Starter Assets só acerta essa camada; o Unity oferece criá-la ao importar os
   Starter Assets).

## 4. Livros

1. Coloque o modelo do livro em cima da mesa/estande e selecione-o na Hierarchy.
2. Use **Sebraelab > Livros > Configurar livro selecionado**. Isso faz automaticamente os **PASSOS 4 e 5** do guia:
   - **Box Collider** ajustado ao tamanho do livro;
   - **Rigidbody** com gravidade (o livro fica apoiado na mesa e cai se for solto);
   - **XR Grab Interactable** (pegar com o Grip);
   - **LivroPopup** e um filho **Popup**: Canvas em World Space com escala 0.001, painel, título, autor, capa,
     resumo, vídeo e um botão *Iniciar Desafio* (desativado, para os minigames no futuro).
3. Conteúdo do popup. Escolha um jeito:
   - **Direto na cena:** expanda `Livro > Popup` na Hierarchy (ative a caixinha para ver e desative de novo depois)
     e edite os textos.
   - **Com asset (recomendado):** na pasta `Assets/Livros`, botão direito > **Create > Sebraelab > Dados do Livro**.
     Preencha título, autor, resumo, capa e vídeo e arraste o asset para o campo **Dados** do `LivroPopup`.
     Ao dar Play o popup é preenchido sozinho.
   - Campos vazios (autor, capa, vídeo) somem do popup automaticamente.
4. No `LivroPopup`, **Quando Mostrar** escolhe se o popup abre ao aproximar a mão, ao segurar, ou nos dois casos.
5. Depois de configurar um livro, arraste-o para `Assets/Livros` para virar *prefab*. Os outros livros podem ser cópias
   desse prefab, trocando só o modelo e o campo **Dados**.

> A mesa/estande precisa ter collider (passo 3), senão o livro cai através dela ao dar Play.

## 5. GIFs (vídeos)

O Unity não toca `.gif`. Converta para `.mp4`:

```bash
ffmpeg -i animacao.gif -movflags faststart -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" animacao.mp4
```

(ou use qualquer conversor online de GIF para MP4). Coloque o `.mp4` em `Assets/Videos` e arraste para o campo
**Video** do asset *Dados do Livro*, ou para o **Video Clip** do `VideoPlayer` no objeto `Popup/Painel/Video`.
O vídeo toca em loop quando o popup abre e para quando fecha. Se o vídeo não tocar no Quest, marque **Transcode** nas
configurações de import do vídeo.

## 6. Estandes (área de destaque)

1. Crie um objeto vazio na frente do estande, por exemplo `Zona Estande 1`.
2. **Add Component > Estande Zona Destaque**. Ele cria um **Box Collider** como *trigger*. Ajuste o tamanho para cobrir
   a área onde o jogador fica, com uns 2 m de altura, porque o script usa a posição da cabeça.
3. Crie uma **Spot Light** apontando para o estande e arraste-a para **Objetos Destaque**: ela começa apagada e acende
   quando o jogador chega.
4. Opcional: um **Audio Source** (com *Play On Awake* desligado) em **Som Ao Entrar**.

## 7. Testar sem óculos

- **Sebraelab > Jogador > Criar jogador de teste (teclado e mouse)** e dê Play:
  **WASD** anda, **mouse** olha, **Shift** corre, **E** ou **clique** no livro abre o popup, **Esc** solta o mouse.
- Se um headset estiver conectado (Quest Link / Air Link), a mesma cena usa o XR Origin e o jogador de PC se desliga sozinho.
- Para testar o **pegar** sem óculos, use o **XR Device Simulator** (sample do XRI). Nesse caso desmarque
  **Trocar Automaticamente** no `DesktopPlayerController` ou desative o *Jogador PC*.

## 8. Gerar o app para o Meta Quest

1. No Quest: ative o **modo desenvolvedor** (app Meta Horizon no celular).
2. **File > Build Settings** (*Build Profiles* no Unity 6) > **Android** > **Switch Platform**.
3. Conecte o Quest no USB, escolha-o em **Run Device** e clique em **Build And Run**.

## 9. Minigames (fase futura)

Cada popup já tem um botão **BotaoMinigame** desativado, ligado em `LivroPopup.IniciarMinigame()`. Quando for a hora:
ative o botão e, no `LivroPopup`, adicione no evento **Ao Iniciar Minigame** o que deve acontecer (carregar uma cena,
ativar um objeto etc.). O botão precisa do **UI Event System** do passo 2.

---

## Problemas comuns

| Problema | Solução |
| --- | --- |
| Erros de compilação / *Safe Mode* ao abrir | Falta o XR Interaction Toolkit. Instale pelo Package Manager (passo 1). |
| Não aparece o menu **Sebraelab** | Há erro de compilação no Console. Resolva primeiro (geralmente é o item acima). |
| Texto do popup não aparece / quadrados | **Window > TextMeshPro > Import TMP Essential Resources**. |
| Materiais rosa | Converta para URP (passo 3.3). |
| Livro cai através da mesa/chão | O cenário está sem colliders (passo 3.2). |
| Não consigo pegar o livro | Confira se o livro tem Collider e **XR Grab Interactable** (use o menu do passo 4). |
| Popup não abre | Confira o campo **Canvas Popup** do `LivroPopup` e o **Quando Mostrar**. |
| Teleporte não funciona | O chão precisa de **Teleportation Area** com a camada **Teleport** (passo 3.6). |
| O modelo aparece "quebrado" depois do clone | Falta o Git LFS: rode `git lfs install` e depois `git lfs pull`. |
