# Architecture de Muscle 2.0

État du dépôt analysé : branche `packaging`, créée depuis `feature/srdr-stiffness-reduction` (commit `62521d1`), novembre 2026.
Ce document décrit l'existant. Il ne propose aucune modification.

Vue d'ensemble : un plugin Grasshopper écrit en C# (`Muscle2.gha`) qui délègue tout le calcul à un package Python (`musclepy`), via Python.NET embarqué dans le processus Rhino.

```
Grasshopper (canvas)
  └─ src/Muscle       C#  composants GH, types GH_* (vue)            → Muscle2.gha
      └─ src/MuscleApp   C#  ViewModel (Point3d, PointLoad…), solveurs « App »
          └─ src/MuscleCore  C#  modèle brut (double[], double[,]), Python.NET, convertisseurs
              └─ src/MusclePy    Python  package `musclepy` : modèle EF + solveurs (numpy/scipy)
```

Les dépendances vont toujours vers l'intérieur. `musclepy` ignore tout de C#, de Grasshopper et de Python.NET : on peut l'utiliser seul.

---

## 1. Le cœur Python (`src/MusclePy/musclepy`)

### Packaging

| Élément | Valeur |
|---|---|
| Fichier | `pyproject.toml` à la **racine du dépôt** (pas dans `src/MusclePy`) |
| Build backend | `hatchling >= 1.27` |
| Nom / version | `musclepy` 2.0.0 (dupliquée dans `musclepy/__init__.py` → `__version__`) |
| Python | `>= 3.9` (le README conseille 3.12.7 dans un env conda `muscle`) |
| Dépendances | `numpy >= 1.26.4`, `scipy >= 1.13.1` — rien d'autre |
| Wheel | `packages = ["src/MusclePy/musclepy"]`, readme `src/MusclePy/README.md`, licence `COPYING.txt` |
| Divers | `musclepy/MusclePy.pyproj` : projet Visual Studio Python référencé par `Muscle.sln`, sans rôle dans le packaging |

En pratique, `scipy` n'est importé que par `solvers/dynamic/main.py`. Tout le reste ne dépend que de `numpy`. Les imports `argparse` (`pynodes.py`) et `ctypes` (`dynamic/main.py`) servent uniquement à récupérer une classe d'exception `ArgumentError`.

### Organisation des modules

```
musclepy/
├── __init__.py            API publique « à plat » (c'est elle qu'appelle le C#)
├── femodel/               modèle éléments finis, immuable, en numpy
│   ├── pynodes.py         PyNodes  : coordonnées, DOF bloqués, charges, déplacements, réactions…
│   ├── pyelements.py      PyElements : type (-1 barre / +1 câble), connectivité, aire, E[comp,trac], tension…
│   ├── pytruss.py         PyTruss  : nodes + elements, is_in_equilibrium()
│   └── prestress_scenario.py  PrestressScenario
├── utils/matrix_calculations.py   matrices d'équilibre, de rigidité, etc. (dépend de femodel)
└── solvers/
    ├── test/        test_script.main — "HELLO","from python" → "hello FROM PYTHON" (diagnostic du pont)
    ├── svd/         SVD de la matrice d'équilibre → PyResultsSVD (mécanismes, modes d'autocontrainte)
    ├── selfstress/  localize_self_stress_modes
    ├── dm/          méthode des déplacements linéaire, puis non linéaire (le non-linéaire appelle le linéaire)
    ├── dr/          relaxation dynamique (PyTrussDR / PyNodesDR / PyElementsDR / PyConfigDR)
    ├── srdr/        DR à réduction de rigidité (ajout du fork) : boucle externe autour de dr/, formules dans formulas.py
    ├── dynamic/     analyse modale (scipy) → PyResultsDynamic
    ├── beam_common.py          noyau corotationnel 6 DDL partagé par les deux solveurs poutre
    ├── beam_fem/    Newton-Raphson poutre-colonne 6 DDL (ajout du fork, portage de Lizée 2026)
    └── beam_dr/     DR explicite 6 DDL (ajout du fork), réutilise beam_fem/py_beam_*.py
```

Dépendances internes notables :

- `srdr` → `dr` (enveloppe le solveur DR sans le modifier).
- `dm.nonlinear_dm` → `dm.linear_dm`.
- `beam_dr` → `beam_fem` (classes nœuds/éléments) et `beam_common`.
- Le modèle poutre (`beam_*`) est indépendant du modèle treillis (`femodel`).

### Fonctions exportées par `musclepy/__init__.py`

`PyNodes`, `PyElements`, `PyTruss`, `PyConfigDR`, `PyConfigSRDR`, `PyConfigBeam`, `PyConfigBeamDR`, `PyBeamNodes`, `PyBeamElements`, `PyResultsSVD`, `PyResultsDynamic`, `test_script_main`, `main_singular_value_decomposition`, `localize_self_stress_modes`, `main_linear_displacement_method`, `main_nonlinear_displacement_method`, `main_dynamic_relaxation`, `main_stiffness_reduction_dr`, `main_beam_fem` (= `beam_fem.main.solve`), `main_beam_dr` (= `beam_dr.main.solve`), `main_dynamic_modal_analysis`.

---

## 2. Comment le plugin C# appelle le Python

### Projets C#

| Projet | Cible | Dépend de | Rôle |
|---|---|---|---|
| `src/Muscle/Muscle.csproj` | net48, sortie **`Muscle2.gha`** (`AssemblyName=Muscle2`) | Grasshopper 7.37 (NuGet), MuscleApp | Composants GH, types `GH_*`, icônes embarquées |
| `src/MuscleApp/MuscleApp.csproj` | net48 | Grasshopper, MuscleCore | ViewModel + logique des composants + configuration Python.NET par défaut |
| `src/MuscleCore/MuscleCore.csproj` | net48 | **`pythonnet` 3.0.5** (NuGet) | Seul projet qui connaît Python |
| `tests/MuscleCoreTests` | — | MuscleCore | Tests MSTest qui lancent réellement Python |

### Démarrage du moteur Python (composant « StartPython.NET »)

Chaîne d'appel : `Muscle/Components/0.PythonNETInit/StartPythonNETComponent.cs` → `MuscleApp/PythonNETInit/PythonNET.cs` → `MuscleCore/PythonNETInit/PythonNETConfig.cs` + `PythonNETManager.cs`.

Entrées du composant : `Start` (booléen), chemin Anaconda, nom de l'environnement conda, nom de la DLL `python3xx.dll`.

1. **Recherche d'Anaconda** (`PythonNETConfig.TryFindingAnaconda`), dans cet ordre :
   `%USERPROFILE%\Anaconda3`, `C:\ProgramData\Anaconda3`, `%LOCALAPPDATA%\Anaconda3`, `%APPDATA%\Anaconda3`.
   Un dossier est valide s'il contient `python.exe`. Miniconda ou un Python non-conda ne sont pas détectés automatiquement, mais un chemin saisi à la main fonctionne s'il contient `python.exe`.
2. **Environnement** : `muscle` s'il existe, sinon `base`. Chemin = `<anaconda>` pour `base`, `<anaconda>\envs\<nom>` sinon. Il doit contenir `python.exe`.
3. **DLL** : première `python3NN.dll` trouvée dans l'environnement (de 3.19 jusqu'à 3.1).
4. **Mode** :
   - *Utilisateur* (cas normal) : `musclepy` doit être installé par pip dans l'environnement. La vérification accepte `site-packages\musclepy\` **ou** `site-packages\musclepy-*.dist-info` (cas d'une installation éditable `pip install -e .`).
   - *Développeur* : déclenché si l'utilisateur tape le nom d'environnement **`muscle4developers`**. L'environnement réellement utilisé devient `base`, et `PYTHONPATH` reçoit `Directory.GetCurrentDirectory()/../../../../MusclePy`, ce qui suppose que le répertoire courant est `src/Muscle/bin/Debug/net48/`. Attention : c'est le répertoire courant **du processus Rhino**, pas celui du `.gha`. Le mode fonctionne donc seulement si Rhino a été lancé depuis ce dossier. Sur cette machine, le flux réellement utilisé est le mode utilisateur avec l'env `muscle-dev`, qui contient une installation éditable pointant vers `src/MusclePy`.
5. **Initialisation** (`PythonNETManager.Initialize`) : ajoute l'environnement au `PATH`, fixe `PYTHONHOME`, `PYTHONPATH` (`site-packages;Lib;DLLs[;srcDir]`) et `PYTHONNET_PYDLL`, puis appelle `PythonEngine.Initialize()`. Ensuite : `Converters.Main.RegisterConverters()`, puis `site.addsitedir(site-packages)` (indispensable pour que le `.pth` d'une installation éditable soit lu).
6. **Auto-tests** : `import musclepy`, `numpy.cos(2π) == 1`, `musclepy.test_script_main("HELLO","from python") == "hello FROM PYTHON"`.
7. Arrêt (`PythonEngine.Shutdown`) quand on remet `Start` à false ou à la fermeture du document.

Le moteur est **unique par processus Rhino**. Changer d'environnement exige un redémarrage de Rhino si l'initialisation a réussi une première fois.

### Appel d'un solveur

Tout est **en mémoire, dans le même processus**. Il n'y a ni sous-processus, ni fichier, ni JSON, ni socket.

```
Composant GH (Muscle)          ex. SRDRSolverComponent.cs
  → solveur App (MuscleApp/Solvers/SRDR/…)     Truss/PointLoad → CoreTruss + double[]
    → solveur Core (MuscleCore/Solvers/SRDR/SRDR.cs)
        using (Py.GIL()) {
          dynamic musclepy = Py.Import("musclepy");
          pyResult = musclepy.main_stiffness_reduction_dr(structure: coreTruss.ToPython(), loads_increment: double[], …);
          coreResult = pyResult.As<CoreTruss>();
        }
```

Chaque solveur Core suit le même schéma : `Py.Import("musclepy")` (16 occurrences), appel d'une fonction exportée **avec des arguments nommés**, puis décodage du résultat. La liste exacte des fonctions appelées depuis MuscleCore est celle de la section 1.

### Format d'échange : codecs Python.NET

Enregistrés dans `MuscleCore/Converters/Main.cs` :

| Sens | Classe C# | Objet Python | Fichier |
|---|---|---|---|
| C# → Py | `CoreTruss` | `musclepy.PyTruss(...)` | `EncodeToPy/PyTrussEncoder.cs` |
| C# → Py | `CoreNodes` | `musclepy.PyNodes(initial_coordinates=, dof=, loads=, displacements=, reactions=, resisting_forces=)` | `EncodeToPy/PyNodesEncoder.cs` |
| C# → Py | `CoreElements` | `musclepy.PyElements(...)` | `EncodeToPy/PyElementsEncoder.cs` |
| C# → Py | `CoreConfigDR`, `CoreConfigSRDR`, `CoreConfigBeam`, `CoreConfigBeamDR` | `PyConfigDR`, `PyConfigSRDR`, … | `EncodeToPy/*` |
| C# → Py | `CoreBeamNodes`, `CoreBeamElements` | `musclepy.solvers.beam_fem.…` | `EncodeToPy/PyBeam*Encoder.cs` |
| Py → C# | `PyTruss` **ou** `PyTrussDR` (test sur `type.__name__`) | `CoreTruss` | `DecodeFromPy/PyTrussDecoder.cs` |
| Py → C# | `PyNodes`, `PyElements`, `PyResultsSVD`, `PyResultsDynamic`, `PyBeamNodes`, `PyBeamElements` | `Core*` | `DecodeFromPy/*` |

- Les tableaux .NET `double[]` passent directement. Les matrices `double[,]` sont reconstruites à partir de `np.ndarray.shape` et `ravel()` (`DecoderHelper.As2dArray` / `AsInt2dArray` / `AsBool2dArray`).
- Unités : SI côté Python (m, N, Pa). Les conversions depuis mm / MPa saisis dans Grasshopper sont faites côté C# (cf. le correctif d'unités du commit `fb4687a`).
- Les décodeurs lisent les **attributs Python** suivants. Ils forment, avec les noms de fonctions et d'arguments, le contrat entre les deux langages : `nodes`, `elements`, `initial_coordinates`, `coordinates`, `dof`, `loads`, `displacements`, `reactions`, `resisting_forces`, `residuals`, `count`, `type`, `end_nodes`, `area`, `youngs`, `free_length`, `tension`, `inertia`, `masses`, `frequencies`, `mode_shapes`, `r`, `s`, `m`, `moment`, `moment_start`, `moment_end`, `deflection`, `hinge_start`, `hinge_end`, `orientation`, `shear_modulus`, `torsion`, `torsion_constant`, `is_in_equilibrium()`. S'y ajoutent les compteurs de configuration renvoyés par effet de bord : `n_time_step`, `n_ke_reset`, `n_iteration`, `residual`, `converged`, `n_outer_iterations`.

---

## 3. Ce qui ne peut pas bouger sans casser Grasshopper

### Liens « durs » (casse immédiate)

| Quoi | Pourquoi |
|---|---|
| **GUID des composants** (`ComponentGuid`, 55 `new Guid(...)` dans `src/Muscle`) et **GUID du plugin** (`MuscleInfo.Id = 080da54a-…`) | Un `.gh` référence chaque composant par son GUID. Si on en change un, le composant apparaît « manquant » dans tous les fichiers existants. Le fork a **déjà** changé tous les GUID par rapport à l'amont (voir la section 4 sur les exemples). |
| **API publique de `musclepy`** : noms des fonctions exportées, **noms des arguments nommés**, noms des classes (`PyTruss`, `PyTrussDR`, …) et des attributs listés en §2 | Le C# appelle ces fonctions par chaîne via `dynamic`. Un renommage ne se voit pas à la compilation : il casse uniquement à l'exécution, dans Rhino. |
| **Nom du package importable `musclepy`** | Codé en dur (`PythonNETManager.MusclePy`, `Py.Import("musclepy")` ×16), et utilisé pour la détection `site-packages/musclepy` ou `musclepy-*.dist-info`. |
| **`musclepy.solvers.test.test_script.main`** et sa sortie exacte `"hello FROM PYTHON"` | Sert d'auto-test au démarrage. S'il échoue, StartPython.NET échoue. |
| **Nom de l'assembly `Muscle2`** (`.gha`) | Choisi pour coexister avec `Muscle.gha` officiel dans le dossier Libraries. |

### Liens relatifs (casse si on déplace des dossiers)

| Chemin | Qui en dépend |
|---|---|
| `src/MusclePy/musclepy/` | `pyproject.toml` (`packages`, `include`), installation éditable `pip install -e .` de l'env `muscle-dev`, mode développeur (`../../../../MusclePy` depuis `src/Muscle/bin/Debug/net48`), `tests/MusclePyTests/test_all.py` (`../../src/MusclePy`), `tests/MuscleCoreTests/CoreTestsConfig.cs` (`../../../../../src/MusclePy` depuis `bin/Debug/net48`) |
| `pyproject.toml`, `COPYING.txt`, `src/MusclePy/README.md` | `pyproject.toml` les référence par chemin relatif à la racine |
| `manifest.yml` (racine) | `Muscle.csproj` → `<ApplicationManifest>..\..\manifest.yml</ApplicationManifest>` (paquet Yak) |
| `README.md` (racine) | `Muscle.csproj` → `<None Include="..\..\README.md" Pack="True">` |
| `src/Muscle/Resources/Icons/*.png` | Ressources embarquées sous le nom `Muscle.Resources.Icons.<nom>.png`, chargées par `View/IconLoader.cs`. Si on renomme un PNG ou le dossier, le composant perd son icône (pas de plantage). |
| `src/Muscle/MuscleApp/MuscleCore` (dossiers des `.csproj`) | `Muscle.sln` et les `ProjectReference` relatives |
| Arborescence `bin/Debug/net48` | `Properties/launchSettings.json` (`RHINO_PACKAGE_DIRS=$(ProjectDir)$(OutputPath)`) et les deux calculs de chemin ci-dessus |

### Ce qui peut bouger sans risque pour Grasshopper

- Les noms et dossiers des **fichiers** `.cs` (seuls comptent les GUID et les espaces de noms des types `GH_*` sérialisés).
- Les modules internes de `musclepy`, tant que `musclepy/__init__.py` réexporte les mêmes noms.
- `tests/`, `examples/`, `documentation/`, `tools/`.

Côté sérialisation, les types `GH_Truss`, `GH_Support`, `GH_CrossSection` et `GH_BilinearMaterial` surchargent `Write`/`Read`, mais délèguent à `base`. Aucune donnée Muscle n'est donc persistée dans les `.gh` au-delà de ce que fait Grasshopper.

---

## 4. Exemples et cas de test

### Tests Python (`tests/MusclePyTests`, unittest, compatibles pytest)

128 tests, tous verts avec l'env `muscle-dev` (Python 3.12.7, numpy 2.3.4, scipy 1.16.2). Lancement : `python -m unittest discover -s MusclePyTests -t .` depuis `tests/`, ou `test_all.py`.

| Dossier | Cas |
|---|---|
| `femodel/` | `PyNodes`, `PyElements` (unitaires) |
| `solvers/linear_dm/` | treillis 2 barres (annexe A1.1.1 du mémoire de J. Feron), 3 câbles précontraints |
| `solvers/nonlinear_dm/` | 2 barres, mécanisme lâche, corde tendue précontrainte (+ solution Excel en 3 étapes de charge) |
| `solvers/svd/` | 2 câbles, arc 3 câbles, **simplex expérimental**, modes d'autocontrainte d'un « 2X Snelson » |
| `solvers/dr/` | 2 câbles, 3 câbles (simple / complexe), **simplex** avec raccourcissement d'un câble (référence MATLAB de L. Rhode-Barbarigos) |
| `solvers/dynamic/` | analyse modale du **simplex** |
| `solvers/srdr/` | formules de réduction de rigidité (valeurs de Payen et al. 2026), **simplex** SRDR |

Aucun test Python ne couvre `beam_fem` ni `beam_dr` : ces solveurs ne sont testés que côté C#.

### Tests C# (`tests/MuscleCoreTests`, MSTest)

Ils démarrent un vrai moteur Python (mode développeur, env `base` par défaut dans `CoreTestsConfig.cs`) :
convertisseurs (`PyNodes`, `PyElements`, `PyTruss`, `PyConfigDR`, `PyResultsSVD`), solveurs (TestScript, SVD, SelfStress, LinearDM, DR, SRDR, Dynamic, BeamFEM, BeamDR), ViewModel (propriétés et flambement des poutres, `BeamStructure`).

### Exemples Grasshopper (`examples/`)

| Fichier | Contenu | Composants référencés |
|---|---|---|
| `Tensegrity Simplex/ExperimentalTensegritySimplex - Static Loading and Self-Stress.gh` | Simplex de Feron, Rhode-Barbarigos & Latteur (ASCE JSE 2023), avec l'article et ses annexes en PDF | 27 GUID **amont** |
| `Tensegrity Simplex/ExperimentalTensegritySimplex - Dynamic tests.gh` | Essais dynamiques du simplex (mémoire A. Desmet) | 23 GUID **amont** |
| `linear Truss/Linear Truss Examples and Background MI.gh` | Treillis linéaires (thèse J. Feron p. 23-28). Les dossiers `Warren/`, `arc n3/`, `arc n7/`, `two legs/` contiennent ~180 images `.bmp` générées, plus `BackgroundMI.xlsx` | 12 GUID **amont** |
| `Geometries/tensegrity_geometries_AFlamant_2020.gh` | Géométries de tenségrités (mémoire A. Flamant), sans composant Muscle | — |

**Attention :** les trois exemples Muscle référencent les GUID du plugin d'**origine** (`f8029d34-…`), aucun de ceux du fork. Ouverts avec seulement `Muscle2.gha` installé, leurs composants Muscle apparaîtront manquants.

Hors dépôt, `../TestsGH/` contient des définitions construites avec le fork : `Simplex.gh`, `Simplex2.0.gh`, `Simplex3.0.gh`, `Single strut.gh`, `Cantilever Beam.gh`. Elles utilisent les GUID du fork. `Simplex.gh` mélange encore 7 composants amont.

### Documentation

`documentation/IASS2024_MUSCLE_Final version.pdf` (article de référence), `src/README.md` (architecture pour les développeurs, en anglais), `NOTICE.txt` (contributions du fork), `CHANGELOG.md` (arrêté à 1.1.0, pas à jour pour 2.0.0).
